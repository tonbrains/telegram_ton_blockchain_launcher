using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TONBRAINS.TELEGRAMOPENNETWORK.APP.Models
{
    public class Node
    {

        public string Id { get; set; }
        public string Ip { get; set; }
        public string SshIp { get; set; }
        public int SshPort { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }


        
    }
}
