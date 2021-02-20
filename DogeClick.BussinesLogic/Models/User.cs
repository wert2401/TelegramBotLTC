using System;
using System.Collections.Generic;
using System.Text;

namespace DogeClick.BussinesLogic.Models
{
    public class User
    {
        public string Phone { get; private set; }
        public string Password { get; private set; }
        public int ApiId { get; private set; }
        public string ApiHash { get; private set; }
        public DateTime TimeOfCreation { get; set; }
        public DateTime TimeOfLastLaunch { get; set; }
        public string LTCAdress { get; private set; }
        public User(string phone, string password, int apiId, string apiHash, string lTCAdress)
        {
            Phone = phone;
            Password = password;
            ApiId = apiId;
            ApiHash = apiHash;
            TimeOfCreation = DateTime.Now;
            LTCAdress = lTCAdress;
        }
    }
}
