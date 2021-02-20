using System;
using System.Collections.Generic;
using System.Text;

namespace DogeClick.BussinesLogic.Models
{
    public class BotStateInfo
    {
        public BotStateInfo(int siteCount = 0, string stopReason = "", decimal balance = 0, int captchaLimit = 3, int captchaCounter = 0, bool noAds = false, int lastMessage = 0)
        {
            SiteCount = siteCount;
            StopReason = stopReason;
            BalanceOfBot = balance;
            CaptchaLimit = captchaLimit;
            CaptchaCounter = captchaCounter;
            NoAds = noAds;
            LastMessage = lastMessage;
        }

        public int SiteCount { get; set; }
        public string StopReason { get; set; }
        public decimal EarningFromBotSession { get; set; }
        public decimal BalanceOfBot { get; set; }
        public int CaptchaLimit { get; private set; }
        public int CaptchaCounter { get; set; }
        public bool NoAds { get; set; }
        public int LastMessage { get; set; }
    }
}
