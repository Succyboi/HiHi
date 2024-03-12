using System.Net;
using System.Net.Sockets;
using System.Threading;
using static HiHi.STUN.STUNMessage;

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
    public static class STUNClient {
        private const int TRANSACTION_TIMEOUT_MILLISECONDS = 10000;
        private const int TRANSACTION_POLL_INTERVAL_MILLISECONDS = 1000;
        private const int EXPECTED_PACKET_SIZE = 512;

        public static bool TryQuery(IPEndPoint stunIPEndPoint, UdpClient client, out STUNResult result) {
            if (stunIPEndPoint == null) { 
                result = null; 
                return false; 
            }

            STUNMessage message = new STUNMessage() { Type = STUNMessageType.BindingRequest };
            STUNMessage response = DoTransaction(message, client, stunIPEndPoint);

            if (CheckUDPBlocked(response, out result)) { return true; }

            IPEndPoint initialMappedAddress = response.MappedAddress;
            IPEndPoint initialChangedAddress = response.MappedAddress;
            message = new STUNMessage() { Type = STUNMessageType.BindingRequest, ChangeRequest = new STUNChangeRequest(true, true) };
            response = DoTransaction(message, client, stunIPEndPoint);

            if (CheckNAT(client, response, out result)) { return true; }

            message = new STUNMessage() { Type = STUNMessageType.BindingRequest };
            response = DoTransaction(message, client, initialChangedAddress);

            if (response == null) {
                result = null;
                return false;
            }

            if (CheckSymmetricNAT(response, initialMappedAddress, out result)) { return true; }

            message = new STUNMessage() { Type = STUNMessageType.BindingRequest, ChangeRequest = new STUNChangeRequest(false, true) };
            response = DoTransaction(message, client, response.ChangedAddress);

            CheckRestriction(response, out result);
            return true;
        }

        private static bool CheckUDPBlocked(STUNMessage response, out STUNResult result) {
            if (response != null) {
                result = null; 
                return false; 
            }

            result = new STUNResult(STUNNetType.UdpBlocked, null);
            return true;
        }

        private static bool CheckNAT(UdpClient client, STUNMessage response, out STUNResult result) {
            if (!client.Client.LocalEndPoint.Equals(response?.MappedAddress)) {
                if (response != null) {
                    result = new STUNResult(STUNNetType.FullCone, response.MappedAddress);
                    return true;
                }

                result = null;
                return false;
            }

            if (response != null) {
                result = new STUNResult(STUNNetType.OpenInternet, response.MappedAddress);
                return true;
            }

            result = new STUNResult(STUNNetType.SymmetricUdpFirewall, response.MappedAddress);
            return true;
        }

        private static bool CheckSymmetricNAT(STUNMessage response, IPEndPoint initialMappedAddress, out STUNResult result) {
            if (!(response.MappedAddress?.Equals(initialMappedAddress) ?? false)) {
                result = new STUNResult(STUNNetType.Symmetric, initialMappedAddress);
                return true;
            }

            result = null;
            return false;
        }

        private static void CheckRestriction(STUNMessage response, out STUNResult result) {
            if (response != null) {
                result = new STUNResult(STUNNetType.RestrictedCone, response.MappedAddress);
                return;
            }

            result = new STUNResult(STUNNetType.PortRestrictedCone, response.MappedAddress);
        }

        private static STUNMessage DoTransaction(STUNMessage request, UdpClient client, IPEndPoint stunIPEndpoint) {
            byte[] requestData = request.ToByteArray();
            byte[] receivedData = new byte[EXPECTED_PACKET_SIZE];
            IPEndPoint remoteEndPoint = stunIPEndpoint;

            for (int t = 0; t < TRANSACTION_TIMEOUT_MILLISECONDS / TRANSACTION_POLL_INTERVAL_MILLISECONDS; t++) {
                try {
                    client.Send(requestData, requestData.Length, stunIPEndpoint);

                    if (client.Available > 0) {
                        receivedData = client.Receive(ref remoteEndPoint);

                        STUNMessage response = new STUNMessage();
                        response.FromByteArray(receivedData);

                        return response;
                    }

                    Thread.Sleep(TRANSACTION_POLL_INTERVAL_MILLISECONDS / 1000);
                }
                catch { }
            }

            return null;
        }
    }
}