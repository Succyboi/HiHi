using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using HiHi.STUN;

/*
 * ANTI-CAPITALIST SOFTWARE LICENSE (v 1.4)
 *
 * Copyright © 2023 Pelle Bruinsma
 * 
 * This is anti-capitalist software, released for free use by individuals and organizations that do not operate by capitalist principles.
 *
 * Permission is hereby granted, free of charge, to any person or organization (the "User") obtaining a copy of this software and associated documentation files (the "Software"), to use, copy, modify, merge, distribute, and/or sell copies of the Software, subject to the following conditions:
 * 
 * 1. The above copyright notice and this permission notice shall be included in all copies or modified versions of the Software.
 * 
 * 2. The User is one of the following:
 *    a. An individual person, laboring for themselves
 *    b. A non-profit organization
 *    c. An educational institution
 *    d. An organization that seeks shared profit for all of its members, and allows non-members to set the cost of their labor
 *    
 * 3. If the User is an organization with owners, then all owners are workers and all workers are owners with equal equity and/or equal vote.
 * 
 * 4. If the User is an organization, then the User is not law enforcement or military, or working for or under either.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT EXPRESS OR IMPLIED WARRANTY OF ANY KIND, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
namespace HiHi.Common {
    public static class IPUtility {
        private const string END_POINT_STRING_TEMPLATE = "{0}:{1}";
        private const int MAX_PORT_AQUISITION_TRIES = 1000;

        private static Dictionary<string, string[]> HostNameCache = new Dictionary<string, string[]>();
        private static Dictionary<string, IPEndPoint> EndPointCache = new Dictionary<string, IPEndPoint>();

        public static int GetFreePort(int? preferredPort = null) {
            int port = preferredPort ?? HiHiConfiguration.PREFERRED_PORT;
            int tries = 0;

            while (!CheckUDPPortAvailable(port) && tries < MAX_PORT_AQUISITION_TRIES) {
                port++;
                tries++;
            }
            return port;
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

        public static bool TryGetLocalIPv4Address(out IPAddress iPaddress) {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress address in host.AddressList) {
                if (address.AddressFamily == AddressFamily.InterNetwork) {
                    iPaddress = address;
                    return true;
                }
            }

            iPaddress = null;
            return false;
        }

        public static IPAddress GetLocalIPv4Address() {
            if (!TryGetLocalIPv4Address(out IPAddress ip)) {
                throw new HiHiException("Failed to get local IP address.");
            }

            return ip;
        }

        public static bool TryGetLocalIPv6Address(out IPAddress iPaddress) {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress address in host.AddressList) {
                if (address.AddressFamily == AddressFamily.InterNetworkV6) {
                    iPaddress = address;
                    return true;
                }
            }

            iPaddress = null;
            return false;
        }

        public static IPAddress GetLocalIPv6Address() {
            if (!TryGetLocalIPv6Address(out IPAddress ip)) {
                throw new HiHiException("Failed to get local IP address.");
            }

            return ip;
        }

        public static bool TryGetLocalIPv4AddressString(out string iPaddressString) {
            bool result = TryGetLocalIPv4Address(out IPAddress address);
            iPaddressString = address?.ToString();
            
            return result;
        }

        public static string  GetLocalIPv4AddressString() {
            if (!TryGetLocalIPv4AddressString(out string ip)) {
                throw new HiHiException("Failed to get local IP address.");
            }

            return ip;
        }

        public static bool TryGetLocalIPv6AddressString(out string iPaddressString) {
            bool result = TryGetLocalIPv6Address(out IPAddress address);
            iPaddressString = address?.ToString();

            return result;
        }

        public static string GetLocalIPv6AddressString() {
            if (!TryGetLocalIPv6AddressString(out string ip)) {
                throw new HiHiException("Failed to get local IP address.");
            }

            return ip;
        }

        public static void RetrieveRemoteIPEndPoint(int port, Action<bool, IPEndPoint> onResult) {
            Task.Run(() => {
                try {
                    if(!STUNUtility.TryRetrieveRemoteIPEndPoint(port, out IPEndPoint externalIPEndPoint, out _)) {
                        onResult?.Invoke(false, null);
                        return;
                    }

                    onResult?.Invoke(true, externalIPEndPoint);
                    return;
                }
                catch {
                    onResult?.Invoke(false, null);
                }
            });
        }

        public static string ToEndPointString(this IPEndPoint endpoint) => ToEndPointString(endpoint.Address, endpoint.Port);
        public static string ToEndPointString(IPAddress address, int port) => ToEndPointString(IPAddressToString(address), port);
        public static string ToEndPointString(object address, object port) => string.Format(END_POINT_STRING_TEMPLATE, address, port);

        public static string IPAddressToString(IPAddress iPAddress) {
            if (iPAddress.Equals(IPAddress.Any)) {
                iPAddress = GetLocalIPv4Address();
            }

            return iPAddress.ToString();
        }

        public static bool TryParseHostName(string hostName, out IPAddress[] addresses) {
            if (!HostNameCache.ContainsKey(hostName)) {
                HostNameCache.Add(hostName, Dns.GetHostAddresses(hostName).Select(a => a.ToString()).ToArray());
            }

            addresses = HostNameCache[hostName].Select(s => IPAddress.Parse(s)).ToArray();

            return addresses.Length > 0;
        }

        public static IPEndPoint ParseStringToIPEndPoint(string endPointString) {
            if (TryParseStringToIPEndPoint(endPointString, out IPEndPoint endPoint)) {
                return endPoint;
            }

            throw new HiHiException($"Couldn't parse string: {endPointString}");
        }

        public static bool TryParseStringToIPEndPoint(string endPointString, out IPEndPoint endPoint) {
            if (string.IsNullOrEmpty(endPointString)) {
                endPoint = null;
                return false; 
            }

            if (EndPointCache.ContainsKey(endPointString)) {
                endPoint = EndPointCache[endPointString];
                return true;
            }

            try {
                IPAddress iPAddress;

                string address = endPointString.Substring(0, endPointString.LastIndexOf(':'));
                int port = int.Parse(endPointString.Substring(endPointString.LastIndexOf(':') + 1));

                if (IPAddress.TryParse(address, out iPAddress)) {
                    if (iPAddress.Equals(IPAddress.Any)) {
                        iPAddress = GetLocalIPv4Address();
                    }

                    endPoint = new IPEndPoint(iPAddress, port);
                    EndPointCache.Add(endPointString, endPoint);
                    return true;
                }

                if (TryParseHostName(address, out IPAddress[] addresses)) {
                    iPAddress = addresses.OrderBy(a => a.AddressFamily).FirstOrDefault();
                    endPoint = new IPEndPoint(iPAddress, port);
                    EndPointCache.Add(endPointString, endPoint);
                    return true;
                }
            }
            catch { }

            endPoint = null;
            return false;
        }
    }
}
