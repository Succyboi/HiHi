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
