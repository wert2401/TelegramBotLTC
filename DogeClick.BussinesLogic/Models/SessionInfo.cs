using System;
using System.Collections.Generic;
using System.Text;

namespace DogeClick.BussinesLogic.Models
{
    public class SessionInfo
    {
        public DateTime Date { get; set; }
        public double DurationInMinutes { get; set; }
        public int CountOfUsers { get; set; }
        public decimal Earning { get; set; }
        public decimal WithdrawedCount { get; set; }

        public SessionInfo(DateTime startTime)
        {
            Date = startTime;
            Earning = 0;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Date.ToString("dd:MM:yy HH:mm") + ";");
            sb.Append(DurationInMinutes + ";");
            sb.Append(CountOfUsers + ";");
            sb.Append(Earning + ";");
            return sb.ToString();
        }
    }
}
