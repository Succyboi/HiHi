namespace HiHi {
    public static class HiHiConfiguration {
        // Peer
        public const int BROADCAST_RECEIVE_PORT = 9050;

        public static int HEARTBEAT_SEND_INTERVAL_MS { get; set; } = 1000;
        public static int HEARTBEAT_TIMEOUT_INTERVAL_MS { get; set; } = 5000;

        public static int PING_INTERVAL_MS { get; set; } = 5000;

        // NetworkObject
        public static ObjectAbandonmentPolicy DEFAULT_ABANDONMENT_POLICY = ObjectAbandonmentPolicy.RemainOwnedRandomly;
    }
}