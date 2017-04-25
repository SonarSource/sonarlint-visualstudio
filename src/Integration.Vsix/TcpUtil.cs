using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public static class TcpUtil
    {
        public static int FindFreePort(int startPort)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            IPEndPoint[] ipEndPoint = ipGlobalProperties.GetActiveTcpListeners();

            List<int> usedPorts = ipEndPoint.Select(x => x.Port)
                .Concat(tcpConnInfoArray.Select(x => x.LocalEndPoint.Port)).ToList();

            for (int i = startPort; i < UInt16.MaxValue; i++)
            {
                if (!usedPorts.Contains(i))
                {
                    return i;
                }
            }
            throw new InvalidOperationException("Could not find any free TCP port over port " + startPort);
        }
    }
}
