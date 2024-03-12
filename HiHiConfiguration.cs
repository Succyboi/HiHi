using HiHi.Commands;
using System;
using System.Net;
using System.Net.Sockets;

namespace HiHi {
    public static class HiHiConfiguration {
        // HiHi
        public static readonly Version Version = new Version(0, 2, 1);

        // Peer
        public static int BROADCAST_PORT = 9050;
        public static int PREFERRED_PORT = BROADCAST_PORT;
        public static readonly IPEndPoint BROADCAST_ENDPOINT = new IPEndPoint(IPAddress.Broadcast, BROADCAST_PORT);

        public static int HEARTBEAT_SEND_INTERVAL_MS { get; set; } = 1000;
        public static int HEARTBEAT_TIMEOUT_INTERVAL_MS { get; set; } = 5000;

        public static int PING_INTERVAL_MS { get; set; } = 5000;
        public static int PING_BUDGET_PER_TICK { get; set; } = 1;

        // NetworkObject
        public static NetworkObjectAbandonmentPolicy DEFAULT_ABANDONMENT_POLICY = NetworkObjectAbandonmentPolicy.RemainOwnedRandomly;

        // Signaler
        public static int SIGNALING_DEFAULT_PORT { get; set; } = 28910;
        public static int SIGNALING_HEARTBEAT_SEND_INTERVAL_MS { get; set; } = 5000;
        public static int SIGNALING_HEARTBEAT_TIMEOUT_INTERVAL_MS { get; set; } = 25000;
        public static int SIGNALING_DEFAULT_LOBBY_SIZE { get; set; } = 16;

        // STUN
        public static AddressFamily STUN_PREFERRED_ADDRESS_FAMILY = AddressFamily.InterNetwork;
        public static int STUN_PUBLIC_CANDIDATE_COUNT_PER_FAMILY = 4;
        public static string PUBLIC_STUNL_LIST_IPV4_URL = "https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_ipv4s.txt";
        public static string PUBLIC_STUNL_LIST_IPV6_URL = "https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_ipv6s.txt";

        // Commands
        public static bool COMMANDS_ENABLED = true;
        public static Command[] DEFAULT_COMMANDS = new Command[] {
            new Command("/Help", (address) => {
                string helpString = $"Available commands:\n";

                foreach (Command command in CommandUtility.Commands) {
                    helpString += $"{command.CommandString}\n";
                }

                return helpString.Trim();
            }),

            new Command("/Info", (address) => {
                Peer.Connect(address);
                string infoString = $"HiHiV{Version} Local Peer ID: {Peer.Info.UniqueID} Local EndPoint: {Peer.Info.LocalEndPoint} Remote EndPoint: {Peer.Info.RemoteEndPoint}\n\nConnected to {PeerNetwork.RemotePeerCount} peers:\n";

                foreach(ushort peerID in PeerNetwork.RemotePeerIDs) {
                    PeerInfo peerInfo = PeerNetwork.GetPeerInfo(peerID);

                    infoString += $"{peerInfo} ({Math.Round(peerInfo.PingMS)}MS)\n";
                }

                return infoString.Trim();
            }),

            new Command("/Connect", (address) => {
                if (!Peer.TryConnectViaCode(address)) {
                    Peer.Connect(address);
                }

                return $"Sent connect message to {address}";
            }),

            new Command("/ConnectViaCode", (code) => {


                return $"Sent connect message to {code}.";
            }),

            new Command("/ConnectionCode", (address) => {
                return $"Your connection code is \"{Peer.Info.GetConnectionCode()}\".";
            }),
        };
    }
}