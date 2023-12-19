using System;
using System.Linq;
using System.Collections.Generic;

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
namespace HiHi.Signaling {
    public class SignalerLobby<T> where T : SignalerConnectionInfo {
        protected static Random random = new Random();

        public int Size { get; private set; }
        public string ConnectionKey { get; private set; }
        public int Count => Connections.Count;
        public bool Full => Count >= Size;
        public bool Empty => Count <= 0;
        public List<T> Connections { get; set; } = new List<T>();

        protected Queue<ushort> availableIDs = new Queue<ushort>();

        public SignalerLobby(int size, string connectionKey) {
            this.Size = size;
            this.ConnectionKey = connectionKey;

            for(ushort i = 0; i < Size; i++) {
                availableIDs.Enqueue((ushort)random.Next(ushort.MaxValue));
            }

            availableIDs = new Queue<ushort>(availableIDs.OrderBy(i => random.Next()));
        }

        public virtual bool TryAdd(T connection) {
            if(Full || Size != connection.DesiredLobbySize ||connection.ConnectionKey != ConnectionKey) {
                return false;
            }

            connection.Verify(availableIDs.Dequeue(), connection.RemoteEndPoint);
            Connections.Add(connection);
            return true;
        }

        public virtual void Remove(T connection) {
            availableIDs.Enqueue(connection.UniqueID);
            Connections.Remove(connection);
        }
    }
}
