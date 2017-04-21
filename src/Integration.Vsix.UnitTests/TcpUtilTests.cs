using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TcpUtilTests
    {
        [TestMethod]
        public void Test_Find_Tcp_Port()
        {
            int port = TcpUtil.FindFreePort(9000);
            Console.Out.WriteLine("Port found: " + port);
            Assert.IsTrue(port >= 9000);

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, port);
            Socket newsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            newsock.Bind(localEndPoint);
            newsock.Listen(10);
            newsock.Close();

        }
    }
}
