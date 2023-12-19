using HiHi.Commands;
using System.Net;

namespace HiHi {
    public static class HiHiConfiguration {
        // Peer
        public const int BROADCAST_RECEIVE_PORT = 9050;
        public const int DEFAULT_PUBLIC_PORT = 8050;
        public static readonly IPEndPoint BROADCAST_RECEIVE_ENDPOINT = new IPEndPoint(IPAddress.Broadcast, BROADCAST_RECEIVE_PORT);

        public static int HEARTBEAT_SEND_INTERVAL_MS { get; set; } = 1000;
        public static int HEARTBEAT_TIMEOUT_INTERVAL_MS { get; set; } = 5000;

        public static int PING_INTERVAL_MS { get; set; } = 5000;

        // NetworkObject
        public static NetworkObjectAbandonmentPolicy DEFAULT_ABANDONMENT_POLICY = NetworkObjectAbandonmentPolicy.RemainOwnedRandomly;

        // Commands
        public static bool COMMANDS_ENABLED = true;
        public static Command[] DEFAULT_COMMANDS = new Command[] {
            new Command("/info", (address) => {
                Peer.Connect(address);
                string infoString = $"Local Peer ID: {Peer.Info.UniqueID} Local EndPoint: {Peer.Info.LocalEndPoint} Remote EndPoint: {Peer.Info.RemoteEndPoint}\n\nConnected to {PeerNetwork.RemotePeerCount} peers:\n";

                foreach(ushort peerID in PeerNetwork.RemotePeerIDs) {
                    PeerInfo peerInfo = PeerNetwork.GetPeerInfo(peerID);

                    infoString += $"{peerInfo}\n";
                }

                return infoString;
            }),

            new Command("/connect", (address) => {
                Peer.Connect(address);
                return $"Sent connect message to {address}";
            })
        };

        // Signaler
        public static int SIGNALER_DEFAULT_PORT { get; set; } = 28910;
        public static int SIGNALER_HEARTBEAT_SEND_INTERVAL_MS { get; set; } = 5000;
        public static int SIGNALER_HEARTBEAT_TIMEOUT_INTERVAL_MS { get; set; } = 25000;
        public static int SIGNALER_DEFAULT_LOBBY_SIZE { get; set; } = 16;
    }
}