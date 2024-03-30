using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tmpsclient
{
    public class ServerInfo
    {
        public string ConnectionString;

        public ServerInfo(string path)
        {
            ConnectionString = File.ReadAllText(path);
        }
    }
}
