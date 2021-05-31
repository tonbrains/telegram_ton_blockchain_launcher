using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TONBRAINS.TELEGRAMOPENNETWORK.APP.Models;
using TONBRAINS.TONOPS.Core.Handlers;

namespace TONBRAINS.TONOPS.Core.SSH
{
    public class SSHAuthMdl
    {

        public SSHAuthMdl() { }

        public SSHAuthMdl(Node node)
        {
                Id = node.Id;
                Host = node.SshIp;
                Port = node.SshPort;
                User = node.UserName;
                Password = node.Password;
           
        }

        public string Id
        {
            get;
            set;
        }


        public string Host
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }
        public string User
        {
            get;
            set;
        }
        public string Password
        {
            get;
            set;
        }
    }
}