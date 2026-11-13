using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ClientSession
    {
        public string UniqueCode { get; set; } = Guid.NewGuid().ToString("N").ToUpper();
        public string Ip { get; set; }
        public int Port { get; set; }
        public DateTime ConnectedAt { get; set; } = DateTime.Now;
        public TcpClient TcpClient { get; set; }
    }
}
