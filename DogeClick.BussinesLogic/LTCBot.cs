using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Net.Http;
using TeleSharp.TL;
using TLSharp.Core;
using TeleSharp.TL.Messages;
using TeleSharp.TL.Updates;
using TeleSharp.TL.Account;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using DogeClick.BussinesLogic.Models;

namespace DogeClick.BussinesLogic
{
    public delegate void BotFinished(User user);
    public class LTCBot
    {
        public User User { get; private set; }
        public bool IsAuthorized { get { return client.IsUserAuthorized(); } private set { } }

        public event SiteProcessed OnSiteProcessed;
        public event CaptchaMeeted OnCaptchaMeeted;
        public event BotFinished OnBotFinished;

        private TelegramClient client;
        private HttpClient httpClient;
        private TLUser bot = new TLUser();
        private TLInputPeerUser botPeer = new TLInputPeerUser();

        private string rewardUrl = "https://dogeclick.com/reward";
        private BotStateInfo currentState;

        public LTCBot(User user, HttpClient httpClient, string sessionPath = "Sessions", int captchaLimit = 3)
        {
            User = user;
            this.httpClient = httpClient;
            currentState = new BotStateInfo(captchaLimit: captchaLimit);

            if (!Directory.Exists(sessionPath))
            {
                Directory.CreateDirectory(sessionPath);
            }

            client = new TelegramClient(User.ApiId, User.ApiHash, new FileSessionStore(new DirectoryInfo(sessionPath)), User.Phone);
        }

        //First of all need to call this method
        public async Task ConnectAsync()
        {
            await client.ConnectAsync();

            if (!client.IsConnected)
            {
                throw new Exception("Can not connect to Telegram.");
            }
        }

        //Return Hash for auth with MakeAuthAsync
        public async Task<string> RequestCodeAsync()
        {
            return await client.SendCodeRequestAsync(User.Phone);
        }

        //Code from TG or SMS, hash from RequestCodeAsync
        public async Task MakeAuthAsync(string code, string passHash)
        {
            try
            {
                await client.MakeAuthAsync(User.Phone, passHash, code);
            }
            catch (TLSharp.Core.Exceptions.CloudPasswordNeededException)
            {
                TLPassword tLPassword = await client.GetPasswordSetting();
                await client.MakeAuthWithPasswordAsync(tLPassword, User.Password);
            }
        }

        public async Task<BotStateInfo> Start()
        {
            CheckAuth();
            await InitBotContact();

            decimal balanceFromBeginning = await GetBalance();

            await client.SendMessageAsync(botPeer, "/visit");

            while (currentState.CaptchaCounter < currentState.CaptchaLimit)
            {
                await WaitForMessage();
                TLMessage message = (TLMessage)((TLMessagesSlice)await client.GetHistoryAsync(botPeer, limit: 1)).Messages[0];
                if (message.FromId == bot.Id && message.ReplyMarkup is TLReplyInlineMarkup && message.Id != currentState.LastMessage)
                {
                    try
                    {
                        await ReadMessages();
                        string url = GetURLFromMessage(message);
                        bool processedSuccess = await ProcessPage(url);
                        if (processedSuccess)
                        {
                            currentState.SiteCount++;
                            currentState.LastMessage = message.Id;
                            OnSiteProcessed?.Invoke(currentState.SiteCount);
                        }
                        else
                        {
                            currentState.CaptchaCounter++;
                            OnCaptchaMeeted?.Invoke(currentState.CaptchaCounter, currentState.CaptchaLimit);
                            await SkipSite(message);
                        }
                        
                    }
                    catch { }
                }
                if (message.Message.Contains("no new ads"))
                {
                    currentState.NoAds = true;
                    break;
                }
            }

            await ReadMessages();

            if (currentState.NoAds)
            {
                currentState.StopReason = "There are no new ads.";
            }
            else if (currentState.CaptchaCounter >= currentState.CaptchaLimit)
            {
                currentState.StopReason = "Captcha limit.";
            }
            currentState.BalanceOfBot = await GetBalance();
            currentState.EarningFromBotSession = Math.Abs(currentState.BalanceOfBot - balanceFromBeginning);
            OnBotFinished?.Invoke(User);
            return currentState;
        }

        public async Task<decimal> GetBalance()
        {
            await InitBotContact();
            await client.SendMessageAsync(botPeer, "/balance");
            await WaitForMessage();
            TLMessage balanceMessage = (TLMessage)((TLMessagesSlice)await client.GetHistoryAsync(botPeer, limit: 1)).Messages[0];
            await ReadMessages();
            decimal bal;
            try
            {
                bal = decimal.Parse(Regex.Match(balanceMessage.Message, "0.[0-9]{8}").Value.Replace('.', ','));
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + " in message: " + balanceMessage.Message);
            }
            return bal;
        }

        public async Task Withdraw()
        {
            decimal balance = await GetBalance();
            if (balance < 0.0004m)
            {
                throw new Exception("There are too little amount of money to withdraw");
            }

            await client.SendMessageAsync(botPeer, "/withdraw");
            await WaitForMessage();
            await ReadMessages();
            await client.SendMessageAsync(botPeer, User.LTCAdress);
            await WaitForMessage();
            await ReadMessages();
            await client.SendMessageAsync(botPeer, balance.ToString());
            await WaitForMessage();
            await ReadMessages();
            await client.SendMessageAsync(botPeer, "/confirm");
            await WaitForMessage();
            await ReadMessages();
        }

        private async Task WaitForMessage()
        {
            TLState state = await GetState();
            int i = 0;
            while (state.UnreadCount == 0)
            {
                await Task.Delay(2000);
                if (i > 20)
                {
                    throw new Exception("Time out of waiting for a message");
                }
                state = await GetState();
                i++;
            }
        }

        public void CheckAuth()
        {
            if (!client.IsUserAuthorized())
            {
                throw new Exception("User are not authorized. Make auth first.");
            }
        }

        private async Task InitBotContact()
        {
            TLDialogs dialogs = (TLDialogs)await client.GetUserDialogsAsync();

            foreach (TLUser user in dialogs.Users)
            {
                if (user.FirstName.Contains("LTC"))
                {
                    bot = user;
                    botPeer = new TLInputPeerUser() { AccessHash = (long)bot.AccessHash, UserId = bot.Id };
                    break;
                }
            }
        }

        private string GetURLFromMessage(TLMessage message)
        {
            var reply = (TLReplyInlineMarkup)message.ReplyMarkup;
            var btn = (TLKeyboardButtonUrl)reply.Rows[0].Buttons[0];
            return btn.Url;
        }

        private async Task SkipSite(TLMessage message)
        {
            TLReplyInlineMarkup reply = (TLReplyInlineMarkup)message.ReplyMarkup;
            TLKeyboardButtonCallback skipBtn = (TLKeyboardButtonCallback)reply.Rows[1].Buttons[1];
            TLRequestGetBotCallbackAnswer req = new TLRequestGetBotCallbackAnswer() { Peer = botPeer, MsgId = message.Id, Data = skipBtn.Data };
            await client.SendRequestAsync<TLBotCallbackAnswer>(req);
        }

        private async Task ReadMessages()
        {
            TLRequestReadHistory readHistory = new TeleSharp.TL.Messages.TLRequestReadHistory() { Peer = botPeer };
            var response = await client.SendRequestAsync<TLAffectedMessages>(readHistory);
        }

        //Get information about new messages
        private async Task<TLState> GetState()
        {
            TLRequestGetState request = new TLRequestGetState();
            return await client.SendRequestAsync<TLState>(request);
        }

        private async Task<bool> ProcessPage(string url)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument html = await web.LoadFromWebAsync(url);

            HtmlNode cap = html.DocumentNode.SelectSingleNode("/html/body/div[1]/div[2]/div/div/h6");
            if (cap?.InnerText == "Please solve the reCAPTCHA to continue:")
            {
                return false;
            }

            HtmlNode headbar = html.DocumentNode.SelectSingleNode("//*[@id=\"headbar\"]");
            if (headbar != null)
            {
                string code = headbar.GetDataAttribute("code").Value;
                string token = headbar.GetDataAttribute("token").Value;
                int timer = int.Parse(headbar.GetDataAttribute("timer").Value);
                await Task.Delay(timer * 1000);

                Dictionary<string, string> parameters = new Dictionary<string, string>
                {
                    { "code", code },
                    { "token", token }
                };
                HttpContent content = new FormUrlEncodedContent(parameters);
                await httpClient.PostAsync(rewardUrl, content);
            }
            else
            {
                await Task.Delay(10000);
            }

            return true;
        }
    }
}
