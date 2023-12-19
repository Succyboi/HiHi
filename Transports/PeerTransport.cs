using HiHi.Common;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

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
namespace HiHi {
    public abstract class PeerTransport {
        public const int THREAD_TIMER_INTERVAL_MS = 5;

        public bool Running { get; private set; } = false;
        public bool ReceiveBroadcast { get; set; } = false;
        public bool IncomingMessagesAvailable => !IncomingMessages.IsEmpty;
        public ConcurrentQueue<PeerMessage> IncomingMessages { get; private set; }
        public ConcurrentQueue<PeerMessage> OutgoingMessages { get; private set; }
        public virtual int MaxPacketSize => 0;
        public abstract string LocalEndPoint { get; }
        public abstract string LocalAddress { get; }
        public abstract int LocalPort { get; }

        private Thread incomingThread;
        private ThreadTimer incomingThreadTimer = new ThreadTimer(THREAD_TIMER_INTERVAL_MS);
        private Thread outgoingThread;
        private ThreadTimer outgoingThreadTimer = new ThreadTimer(THREAD_TIMER_INTERVAL_MS);

        public PeerTransport() {
            IncomingMessages = new ConcurrentQueue<PeerMessage>();
            OutgoingMessages = new ConcurrentQueue<PeerMessage>();

            incomingThread = new Thread(() => IncomingRoutine());
            outgoingThread = new Thread(() => OutgoingRoutine());
        }

        public virtual void Start() {
            if (Running) { return; }

            IncomingMessages.Clear();
            OutgoingMessages.Clear();

            Running = true;

            incomingThread.Start();
            outgoingThread.Start();
        }

        public virtual void Stop() {
            if (!Running) { return; }

            Running = false;
        }

        public void Send(PeerMessage message) {
            OutgoingMessages.Enqueue(message);
        }

        public virtual void SendBroadcast(PeerMessage message) {
            throw new NotImplementedException($"{nameof(SendBroadcast)} is not implemented on this {nameof(PeerTransport)}.");
        }

        public virtual void SendNATIntroduction(string internalEndPointA, string externalEndPointA, string internalEndPointB, string externalEndPointB) {
            throw new NotImplementedException($"{nameof(SendNATIntroduction)} is not implemented on this {nameof(PeerTransport)}.");
        }

        public PeerMessage Receive() {
            if(!IncomingMessages.TryDequeue(out PeerMessage message)) {
                throw new HiHiException("Couldn't dequeue from incoming messages.");
            }

            return message;
        }

        protected abstract void ReceiveIncomingMessages();

        protected abstract void SendOutgoingMessages();

        private void IncomingRoutine() {
            while (Running) {
                incomingThreadTimer.Reset();

                ReceiveIncomingMessages();

                incomingThreadTimer.Sleep();
            }
        }

        private void OutgoingRoutine() {
            while(Running) {
                outgoingThreadTimer.Reset();

                SendOutgoingMessages();

                outgoingThreadTimer.Sleep();
            }
        }
    }
}
