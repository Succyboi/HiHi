namespace HiHi {
    public static class HiHiConfiguration {
        // Peer
        public const int BROADCAST_RECEIVE_PORT = 9050;

        public static int HEARTBEAT_SEND_INTERVAL_MS { get; set; } = 1000;
        public static int HEARTBEAT_TIMEOUT_INTERVAL_MS { get; set; } = 5000;

        public static int PING_INTERVAL_MS { get; set; } = 5000;

        // NetworkObject
        public static NetworkObjectAbandonmentPolicy DEFAULT_ABANDONMENT_POLICY = NetworkObjectAbandonmentPolicy.RemainOwnedRandomly;

        // Signaler
        public static int SIGNALER_DEFAULT_PORT { get; set; } = 28910;
        public static int SIGNALER_HEARTBEAT_SEND_INTERVAL_MS { get; set; } = 5000;
        public static int SIGNALER_HEARTBEAT_TIMEOUT_INTERVAL_MS { get; set; } = 25000;
    }
}