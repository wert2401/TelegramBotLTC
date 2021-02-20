using DogeClick.BussinesLogic.Models;
using DogeClick.BussinesLogic.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DogeClick.BussinesLogic
{
    public delegate void SiteProcessed(int sitesCount);
    public delegate void CaptchaMeeted(int captchasCount, int captchaLimit);
    public delegate void AuthorizationNeeded(User user);
    public delegate void SkippedUser(User user);
    public delegate void BotException(Exception e);
    public delegate void Withdrawed(decimal withdrawedCount);
    public delegate void UserStarted(User user);
    public delegate void UserCompleted(BotStateInfo botResult);
    public delegate void SessionCompleted(SessionInfo sessionInfo);

    public class DogeBotsHolder //make method for adding new user
    {
        //Need to give special method that wiil return string from your input (for example Console.ReadLine())
        public delegate string Input();
        private readonly Input InputMethod;
        public event SiteProcessed OnSiteProcessed;
        public event CaptchaMeeted OnCaptchaMeeted;
        public event BotException OnBotException;
        public event AuthorizationNeeded OnAuthorizationNeeded;
        public event SkippedUser OnSkippedUser;
        public event Withdrawed OnWithdrawed;
        public event UserCompleted OnUserCompleted;
        public event UserStarted OnUserStarted;
        public event SessionCompleted OnSessionCompleted;

        private List<User> users;
        private string pathToUsers;
        private string pathToSessionsInfo;
        private decimal withdrowedFromSession;

        public DogeBotsHolder(List<User> users, Input inputMethod, string pathToUsers = "Users.json", string pathToSessionsInfo = "SessionsInfo.csv", decimal withdrowedFromSession = 0)
        {
            this.users = users;
            this.pathToUsers = pathToUsers;
            this.pathToSessionsInfo = pathToSessionsInfo;
            this.withdrowedFromSession = withdrowedFromSession;
            InputMethod = inputMethod;
        }

        public async Task RunBots()
        {
            users = GetUsers(pathToUsers);

            SessionInfo sessionInfo = new SessionInfo(DateTime.Now);

            //Running bots
            using (HttpClient http = new HttpClient())
            {
                foreach (User user in users)
                {
                    LTCBot ltcBot = new LTCBot(user, http);
                    await ltcBot.ConnectAsync();

                    await CheckAuthorization(ltcBot);

                    //Check timeouts of bot
                    if (!IsTimeoutsGood(user))
                    {
                        continue;
                    }

                    BotStateInfo result = new BotStateInfo();

                    try
                    {
                        OnUserStarted?.Invoke(user);
                        SubscribeOnBotEvents(ltcBot);
                        result = await ltcBot.Start();
                        await TryToWithdraw(ltcBot, result);
                    }
                    catch (Exception e)
                    {
                        OnBotException?.Invoke(e);
                    }

                    SaveUsers();

                    OnUserCompleted?.Invoke(result);
                    UpdateSessionInfo(sessionInfo, result);

                    //Timeout before starting next bot
                    if (users.Last() != user)
                    {
                        await Task.Delay(60000);
                    }
                }
            }

            if (sessionInfo.CountOfUsers > 0)
            {
                sessionInfo.DurationInMinutes = (DateTime.Now - sessionInfo.Date).TotalMinutes;
                SaveSession(sessionInfo);
            }

            OnSessionCompleted?.Invoke(sessionInfo);
        }

        private void SaveUsers()
        {
            FileService.WriteJson(pathToUsers, users);
        }

        private void SaveSession(SessionInfo session)
        {
            FileService.AppendToFile(pathToSessionsInfo, session.ToString());
        }

        private void UpdateSessionInfo(SessionInfo sessionInfo, BotStateInfo result)
        {
            sessionInfo.CountOfUsers++;
            sessionInfo.Earning += result.EarningFromBotSession;
            sessionInfo.WithdrawedCount = withdrowedFromSession;
        }

        private async Task TryToWithdraw(LTCBot ltcBot, BotStateInfo result)
        {
            if (result.BalanceOfBot > 0.0004m)
            {
                try
                {
                    await ltcBot.Withdraw();
                    OnWithdrawed?.Invoke(result.BalanceOfBot);
                    withdrowedFromSession += result.BalanceOfBot;
                    result.BalanceOfBot = 0;
                }
                catch (Exception e)
                {
                    OnBotException?.Invoke(e);
                }
            }
        }

        private void SubscribeOnBotEvents(LTCBot ltcBot)
        {
            ltcBot.OnCaptchaMeeted += OnCaptchaMeeted;
            ltcBot.OnSiteProcessed += OnSiteProcessed;
            ltcBot.OnBotFinished += LtcBot_OnBotFinished;
        }

        private bool IsTimeoutsGood(User user)
        {
            if ((DateTime.Now - user.TimeOfCreation).TotalDays < 3 || (DateTime.Now - user.TimeOfLastLaunch).TotalHours < 5)
            {
                OnSkippedUser?.Invoke(user);
                return false;
            }
            return true;
        }

        private async Task CheckAuthorization(LTCBot ltcBot)
        {
            if (!ltcBot.IsAuthorized)
            {
                string passHash = await ltcBot.RequestCodeAsync();
                OnAuthorizationNeeded?.Invoke(ltcBot.User);
                string code = InputMethod();
                await ltcBot.MakeAuthAsync(code, passHash);
            }
        }

        private List<User> GetUsers(string path)
        {
            List<User> u = FileService.ReadJson<List<User>>(path);
            if (u.Count == 0 || u == null)
            {
                throw new Exception("There are no users in file");
            }
            return u;
        }

        private void LtcBot_OnBotFinished(User user)
        {
            user.TimeOfLastLaunch = DateTime.Now;
        }
    }
}
