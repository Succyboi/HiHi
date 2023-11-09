using System.Threading;

namespace HiHi.Signaling {
    public abstract class PeerFinder {
        public bool Running { get; private set; }

        protected virtual int FindRoutineSleepMS => 5000;

        protected Thread thread;

        public PeerFinder() {
            thread = new Thread(() => FindRoutine());
        }

        public virtual void Start() {
            Running = true;
            thread.Start();
        }

        public virtual void Stop() {
            Running = false;
        }

        public virtual void Find() { }

        protected virtual void FindRoutine() {
            while (Running) {
                Find();

                Thread.Sleep(FindRoutineSleepMS);
            }
        }
    }
}