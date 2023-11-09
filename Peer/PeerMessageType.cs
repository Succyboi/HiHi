namespace HiHi {
    public enum PeerMessageType {
        // Never transmitted.
        Unknown = -1,                           // Unknown.

        // Connection.
        Connect = 0,                            // Introductory hello message. Contains PeerInfo, not neccesarily from the peer that sent it.
        Disconnect = 1,                         // Terminating goodbye message.
        HeartBeat = 2,                          // Required to maintian the connection.
        PeerNetwork = 3,                        // Contains the serialized contents of PeerNetwork.
        Time = 4,                               // Contains information to sync time.

        // High level
        PingRequest = 5,                        // Requests a ping response, contains a timestamp.
        PingResponse = 6,                       // Contains the ping from the local peer to the peer that sent the message.
        Log = 7,                                // Contains a string. For debug and general communication purposes.

        // NetworkObject
        ObjectSpawn = 8,                        // Spawns a NetworkObject.
        ObjectDestroy = 9,                      // Destroys a NetworkObject.
        ObjectOwnershipChange = 10,             // Changes a NetworkObject's owner.
        ObjectAbandoned = 11,                   // Abandons a NetworkObject.
        ObjectAbandonmentPolicyChange = 12,     // Changes a NetworkObject's AbandonmentPolicy.

        // SyncObject
        SOData = 13,                            // Contains data to be interpreted by a SyncObject implementation.
        // 14,
        // 15,
        // FOR MORE PEER MESSAGE TYPES, UP PEER_MESSAGE_TYPE_BITS IN THE PeerMessage HEADER.
    }
}
