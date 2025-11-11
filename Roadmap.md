# Roadmap

This document roughly outlines added features per version and potential future additions.



## Version 0.1

### Description

> The initial proof of concept implementation of HiHi.



### Features

- [x] Connections
- [x] Local discovery
- [x] Serialization
- [x] Messaging
- [x] Networked objects
- [x] Ownership
- [x] Synchronized variables
- [x] RPC's
- [x] Time synchronization
- [x] Synchronized transforms
- [x] Synchronized physics bodies
- [x] Synchronized spawning & destruction
- [x] Spawn history synchronization for new connections
- [x] Abandonment
- [x] Questions
- [x] Message allocation optimization
- [x] Prettify NetworkObject implementation
- [x] PeerMessage sender header optimization
- [x] Signaling
- [x] NAT punching *To be more extensively tested.*
- [x] Democracy
- [x] Unity Bindings
- [x] Rename engine implementations (I.E. UnityNetworkObject -> NetworkObject)
- [x] Add HiHiVector2 conversion
- [x] Rework PeerNetwork to distinguish between all peers and remote peers
- [x] Rework PeerNetwork to be static
- [x] Rework SyncSpawn to return type that was spawned
- [x] PeerNetwork player election
- [x] Retire UDPTransport
- [x] NetworkObject Interface properties



## Version 0.2 (WIP)

### Description

> Focused on improving ease of development and remote connection.



### Features

- [x] BitBuffer to/from Base64/Hex
- [x] Remote address estimation *Port estimation still has to be done.*
- [x] Local address usage attempts
- [x] Include HiHi version in connection key
- [x] Update Unity Bindings
- [x] NetworkObject reference SyncObject *To be tested*
- [x] Dissolve interface layer
- [x] STUN external IP fetching
- [x] Connection codes
- [x] NetworkObject Syncable reference
- [x] NetworkObject NONE unique ID
- [x] PeerInfo Syncable reference
- [x] STUN IPv6
- [x] Dissolve core folder
- [x] Ping bugetting
- [ ] Guid based networkobjects
- [ ] SyncSet, SyncList, SyncDictionary
- [ ] Consider separating signaling and p2p code
- [ ] Signaling rework
- [ ] Rework ushort IDs to be variable bit uint
- [ ] Update docs (NAT traversal & Longevity)



### Known bugs

- [ ] STUNClient receives a non-matching TransactionID

  - >  Currently this means that STUNClient doesn't validate messages by TransactionID. 
    >
    > May cause STUN to yield inconsistent results.



## Freezer

### Description

> Contains potential long term features to be implemented.



### Features

- Asset store / Godot asset library uploads

- Example project

- Getting started tutorial
- Steamworks transport
- BLE Transport
- Interest management
- Include LocalAutoConnect with engine implementations
