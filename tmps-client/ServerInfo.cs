﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tmpsclient
{
    public class ServerInfo
    {
        public string ConnectionString;
        public string Name;

        public ServerInfo(string path)
        {
            string[] serverInfo = File.ReadLines(path).ToArray();
            if (serverInfo.Length == 0 )
            {
                throw new Exception("Tried to read empty ServerInfo config file");
            }
            else if (serverInfo.Length == 1)
            {
                ConnectionString = serverInfo[0];
                Name = "";
            }
            ConnectionString = serverInfo[0];
            Name = serverInfo[1];
        }
    }
}
