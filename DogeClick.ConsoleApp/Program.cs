using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DogeClick.BussinesLogic;
using DogeClick.BussinesLogic.Models;

namespace DogeClick.ConsoleApp
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            string pathToUsers = "Users.json";
            List<User> users = new List<User>();

            if (File.Exists(pathToUsers))
            {
                users = JsonConvert.DeserializeObject<List<User>>(File.ReadAllText(pathToUsers));
            }
            if (users.Count == 0)
            {
                Console.WriteLine("You have no number to work with, add new users first");
                Console.ReadKey();
                return;
            }

            //Adding new users, need to move it in bot holder
            if (args.Length > 0 && args[0].ToLower().Contains("add"))
            {
                Console.WriteLine("Adding new user");
                string phone, password, ltc, apiHash;
                int apiId;
                Console.WriteLine("Enter the phone number: ");
                phone = Console.ReadLine();
                Console.WriteLine("Enter the password for authentification: ");
                password = Console.ReadLine();
                Console.WriteLine("Enter the API ID: ");
                apiId = int.Parse(Console.ReadLine());
                Console.WriteLine("Enter the APIHash: ");
                apiHash = Console.ReadLine();
                Console.WriteLine("Enter the LTC adress");
                ltc = Console.ReadLine();

                User userToAdd = new User(phone, password, apiId, apiHash, ltc);
                users.Add(userToAdd);
                File.WriteAllText(pathToUsers, JsonConvert.SerializeObject(users));
                return;
            }
           
            //Running bots
            DogeBotsHolder botsHolder = new DogeBotsHolder(users, Input);
            SubscribeOnEvents(botsHolder);
            Console.WriteLine("Bots started");
            await botsHolder.RunBots();

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        private static void SubscribeOnEvents(DogeBotsHolder botsHolder)
        {
            botsHolder.OnUserCompleted += BotHolder_OnShowBotResult;
            botsHolder.OnSiteProcessed += BotHolder_OnSiteProcessed;
            botsHolder.OnSkippedUser += BotHolder_OnSkippedUser;
            botsHolder.OnSessionCompleted += BotHolder_OnSessionCompleted;
            botsHolder.OnCaptchaMeeted += BotHolder_OnCaptchaMeeted;
            botsHolder.OnUserStarted += BotHolder_OnUserStarted;
            botsHolder.OnAuthorizationNeeded += BotsHolder_OnAuthorizationNeeded;
        }

        private static void BotsHolder_OnAuthorizationNeeded(User user)
        {
            WriteWithColor("Need to auth " + user.Phone, ConsoleColor.Blue);
        }

        private static string Input()
        {
            string str = Console.ReadLine();
            return str;
        }

        private static void BotHolder_OnUserStarted(User user)
        {
            WriteWithColor("User: " + user.Phone + " was started", ConsoleColor.Green);
        }

        private static void WriteWithColor(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        private static void BotHolder_OnSkippedUser(User user)
        {
            WriteWithColor("User: " + user.Phone + " was skipped, try again in " + (5 - (DateTime.Now - user.TimeOfLastLaunch).TotalHours).ToString("N2"), ConsoleColor.Yellow);
        }

        private static void BotHolder_OnShowBotResult(BotStateInfo result)
        {
            WriteWithColor("Stop reason: " + result.StopReason, ConsoleColor.Green);
            WriteWithColor("Count of processed sites: " + result.SiteCount.ToString(), ConsoleColor.Green);
            WriteWithColor("Balance: " + result.BalanceOfBot.ToString(), ConsoleColor.Green);
        }

        private static void BotHolder_OnSessionCompleted(SessionInfo result)
        {
            WriteWithColor("Withdrawed " + result.WithdrawedCount, ConsoleColor.Red);
            WriteWithColor("Duration " + result.DurationInMinutes, ConsoleColor.Red);
            WriteWithColor("Earned: " + result.Earning, ConsoleColor.Red);
        }

        private static void BotHolder_OnSiteProcessed(int sitesCount)
        {
            WriteWithColor("\tCount of processed sites: " + sitesCount.ToString(), ConsoleColor.DarkGray);
        }

        private static void BotHolder_OnCaptchaMeeted(int captchaCount, int captchaLimit)
        {
            WriteWithColor("\t\tCaptcha: " + captchaCount.ToString() + " / " + captchaLimit.ToString(), ConsoleColor.DarkGray);
        }
    }
}
