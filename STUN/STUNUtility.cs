using HiHi.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

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
namespace HiHi.STUN {
    public static class STUNUtility {
        public static IPEndPoint StunServerIPEndPoint { get; set; } = null;
        public static IPEndPoint ExternalIPEndPoint { get; set; } = null;
        public static STUNNetType NetType { get; set; } = STUNNetType.Unknown;

        public static bool TryRetrieveRemoteIPEndPoint(int port, out IPEndPoint externalEndPoint, out string failReason) => TryRetrieveRemoteIPEndPoint(port, null, null, out externalEndPoint, out failReason);

        public static bool TryRetrieveRemoteIPEndPoint(int port, string[] ipv4STUNServers, string[] ipv6STUNServers, out IPEndPoint externalEndPoint, out string failReason) {
            failReason = string.Empty;

            switch (HiHiConfiguration.STUN_PREFERRED_ADDRESS_FAMILY) {
                default:
                case AddressFamily.InterNetwork:
                    if (TryRetrieveRemoteIPv4(port, ipv4STUNServers, out externalEndPoint, out failReason)) { return true; }
                    if (TryRetrieveRemoteIPv6(port, ipv6STUNServers, out externalEndPoint, out failReason)) { return true; }
                    break;

                case AddressFamily.InterNetworkV6:
                    if (TryRetrieveRemoteIPv6(port, ipv6STUNServers, out externalEndPoint, out failReason)) { return true; }
                    if (TryRetrieveRemoteIPv4(port, ipv4STUNServers, out externalEndPoint, out failReason)) { return true; }
                    break;
            }

            failReason = $"Candidates exhausted. Tried {ipv4STUNServers.Length + ipv6STUNServers.Length}.";
            return false;
        }

        public static bool TryFindStunServerCandidates(AddressFamily addressFamily, int desiredCandidates, out string[] candidates) {
            string stunListURL = null;

            switch (addressFamily) {
                case AddressFamily.InterNetwork:
                    stunListURL = HiHiConfiguration.PUBLIC_STUNL_LIST_IPV4_URL;
                    break;

                case AddressFamily.InterNetworkV6:
                    stunListURL = HiHiConfiguration.PUBLIC_STUNL_LIST_IPV6_URL;
                    break;

                default:
                    throw new ArgumentException($"Unsupported {nameof(AddressFamily)} supplied. Please use {nameof(AddressFamily.InterNetwork)} or {nameof(AddressFamily.InterNetworkV6)}.");
            }

            try {
                using (WebClient client = new WebClient()) {
                    string serverList = client.DownloadString(stunListURL);
                    Random random = new Random();

                    candidates = serverList.Split('\n').OrderBy(s => random.Next()).Take(desiredCandidates).ToArray();
                    return true;
                }
            }
            catch {
                candidates = null;
                return false;
            }
        }

        private static bool TryRetrieveRemoteIPv4(int port, string[] STUNServers, out IPEndPoint externalEndPoint, out string failReason) {
            failReason = string.Empty;
            UdpClient client = new UdpClient(port, AddressFamily.InterNetwork);

            if (STUNServers == null) {
                if (!TryFindStunServerCandidates(AddressFamily.InterNetwork, HiHiConfiguration.STUN_PUBLIC_CANDIDATE_COUNT_PER_FAMILY, out STUNServers)) {
                    STUNServers = new string[0];
                }
            }

            ParseStunServerCandidates(STUNServers, out IPEndPoint[] serverCandidates);

            foreach (IPEndPoint stunIPEndPoint in serverCandidates) {
                if (!STUNClient.TryQuery(stunIPEndPoint, client, out STUNResult result)) { continue; }
                if (result.ExternalEndPoint == null) { continue; }

                NetType = result.NetType;
                ExternalIPEndPoint = externalEndPoint = result.ExternalEndPoint;
                client.Close();
                return true;
            }

            client.Close();
            externalEndPoint = null;
            return false;
        }

        private static bool TryRetrieveRemoteIPv6(int port, string[] STUNServers, out IPEndPoint externalEndPoint, out string failReason) {
            failReason = string.Empty;
            UdpClient client = new UdpClient(port, AddressFamily.InterNetworkV6);

            if (STUNServers == null) {
                if (!TryFindStunServerCandidates(AddressFamily.InterNetworkV6, HiHiConfiguration.STUN_PUBLIC_CANDIDATE_COUNT_PER_FAMILY, out STUNServers)) {
                    STUNServers = new string[0];
                }
            }

            ParseStunServerCandidates(STUNServers, out IPEndPoint[] serverCandidates);

            foreach (IPEndPoint stunIPEndPoint in serverCandidates) {
                if (!STUNClient.TryQuery(stunIPEndPoint, client, out STUNResult result)) { continue; }
                if (result.ExternalEndPoint == null) { continue; }

                NetType = result.NetType;
                ExternalIPEndPoint = externalEndPoint = result.ExternalEndPoint;
                client.Close();
                return true;
            }

            client.Close();
            externalEndPoint = null;
            return false;
        }

        private static void ParseStunServerCandidates(string[] candidateStrings, out IPEndPoint[] candidateEndPoints) {
            List<IPEndPoint> parsedEndPoints = new List<IPEndPoint>();

            for(int c = 0; c < candidateStrings.Length; c++) {
                if (!IPUtility.TryParseStringToIPEndPoint(candidateStrings[c], out IPEndPoint parsedEndPoint)) { continue; }

                 parsedEndPoints.Add(parsedEndPoint);
            }

            candidateEndPoints = parsedEndPoints.ToArray();
        }
    }
}

/* 
 * You've found the secret STUN kitty.
 * Pet to make your IPv6 work and queries return favourably.
 *
 *  ／l 
 * (ﾟ､ ｡７
 *  l  ~ヽ
 *  じしf_,)ノ
 * Kitty pet by:
 * Stupid++, Valdemar, <you>
*/