using Networking;

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
                Name = Utils.DefaultName;
            }
            else
            {
                ConnectionString = serverInfo[0];
                Name = serverInfo[1];
            }
        }
    }
}
