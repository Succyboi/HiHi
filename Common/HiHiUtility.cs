using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;

namespace HiHi.Common {
    public static class HiHiUtility {
        public static int GetFreePort(int preferredPort = 0) {
            return CheckUDPPortAvailable(preferredPort)
                ? preferredPort
                : 0;
        }

        public static bool CheckUDPPortAvailable(int port) {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] udpEndpoints = ipGlobalProperties.GetActiveUdpListeners();

            foreach (IPEndPoint udpListener in udpEndpoints) {
                if (udpListener.Port == port) {
                    return false;
                }
            }

            return true;
        }

        public static bool CheckTCPPortAvailable(int port) {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnections = ipGlobalProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation tcpConnection in tcpConnections) {
                if (tcpConnection.LocalEndPoint.Port == port) {
                    return false;
                }
            }

            return true;
        }

        public static bool TryGetLocalIPAddress(out string ip) {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress address in host.AddressList) {
                if (address.AddressFamily == AddressFamily.InterNetwork) {
                    ip = address.ToString();
                    return true;
                }
            }

            ip = "";
            return false;
        }
    }
}
