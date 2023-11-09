using System;

namespace HiHi.Common {
    public class HiHiException : Exception {
        public HiHiException() { }
        public HiHiException(string message) : base(message) { }
        public HiHiException(string message, Exception inner) : base(message, inner) { }
    }
}
