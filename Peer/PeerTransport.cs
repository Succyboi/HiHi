using HiHi.Common;
using System.Collections.Concurrent;
using System.Threading;

namespace HiHi {
    public abstract class PeerTransport {
        public bool Running { get; private set; }
        public bool ReceiveBroadcast { get; set; }
        public ConcurrentQueue<PeerMessage> IncomingMessages { get; private set; }
        public ConcurrentQueue<PeerMessage> OutgoingMessages { get; private set; }
        public virtual int MaxPacketSize => 0;
        public abstract string LocalIPEndPoint { get; }

        private Thread incomingThread;
        private Thread outgoingThread;

        public PeerTransport() {
            IncomingMessages = new ConcurrentQueue<PeerMessage>();
            OutgoingMessages = new ConcurrentQueue<PeerMessage>();

            incomingThread = new Thread(() => IncomingRoutine());
            outgoingThread = new Thread(() => OutgoingRoutine());
        }

        public virtual void Start() {
            IncomingMessages.Clear();
            OutgoingMessages.Clear();

            Running = true;

            incomingThread.Start();
            outgoingThread.Start();
        }

        public virtual void Stop() {
            Running = false;
        }

        public void Send(PeerMessage message) {
            OutgoingMessages.Enqueue(message);
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
                ReceiveIncomingMessages();
            }
        }

        private void OutgoingRoutine() {
            while(Running) {
                SendOutgoingMessages();
            }
        }
    }
}
