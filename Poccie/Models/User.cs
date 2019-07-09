using System;
using System.Net;

namespace PoccieServer.Models
{
    public class User
    {
        public string Username { get; set; }

        public IPEndPoint EndPoint { get; set; }

        public DateTime Connected { get; set; }
    }
}