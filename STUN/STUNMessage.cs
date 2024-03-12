using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

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
namespace HiHi.STUN {
    // Implements STUN message. Defined in RFC 3489.
    public class STUNMessage {
        private const int IPv4_ADDRESS_LENGTH = 4;
        private const int IPv6_ADDRESS_LENGTH = 16;

        public enum STUNMessageType {
            BindingRequest = 0x0001,
            BindingResponse = 0x0101,
            BindingErrorResponse = 0x0111,
            SharedSecretRequest = 0x0002,
            SharedSecretResponse = 0x0102,
            SharedSecretErrorResponse = 0x0112,
        }

        private enum AttributeType {
            MappedAddress = 0x0001,
            ResponseAddress = 0x0002,
            ChangeRequest = 0x0003,
            SourceAddress = 0x0004,
            ChangedAddress = 0x0005,
            Username = 0x0006,
            Password = 0x0007,
            MessageIntegrity = 0x0008,
            ErrorCode = 0x0009,
            UnknownAttribute = 0x000A,
            ReflectedFrom = 0x000B,
            XorMappedAddress = 0x8020,
            XorOnly = 0x0021,
            ServerName = 0x8022,
        }

        private enum IPFamily {
            IPv4 = 0x01,
            IPv6 = 0x02,
        }

        public STUNMessageType Type {
            get { return type; }

            set { type = value; }
        }

        public Guid TransactionID {
            get { return transactionID; }
        }

        public IPEndPoint MappedAddress {
            get { return mappedAddress; }

            set { mappedAddress = value; }
        }

        public IPEndPoint ResponseAddress {
            get { return responseAddress; }

            set { responseAddress = value; }
        }

        public STUNChangeRequest ChangeRequest {
            get { return changeRequest; }

            set { changeRequest = value; }
        }

        public IPEndPoint SourceAddress {
            get { return sourceAddress; }

            set { sourceAddress = value; }
        }

        public IPEndPoint ChangedAddress {
            get { return changedAddress; }

            set { changedAddress = value; }
        }

        public string UserName {
            get { return userName; }

            set { userName = value; }
        }

        public string Password {
            get { return password; }

            set { password = value; }
        }

        public STUNErrorCode ErrorCode {
            get { return errorCode; }

            set { errorCode = value; }
        }

        public IPEndPoint ReflectedFrom {
            get { return reflectedFrom; }

            set { reflectedFrom = value; }
        }

        public string ServerName {
            get { return serverName; }

            set { serverName = value; }
        }

        private STUNMessageType type = STUNMessageType.BindingRequest;
        private Guid transactionID = Guid.Empty;
        private IPEndPoint mappedAddress = null;
        private IPEndPoint responseAddress = null;
        private STUNChangeRequest changeRequest = null;
        private IPEndPoint sourceAddress = null;
        private IPEndPoint changedAddress = null;
        private string userName = null;
        private string password = null;
        private STUNErrorCode errorCode = null;
        private IPEndPoint reflectedFrom = null;
        private string serverName = null;

        public STUNMessage() {
            transactionID = Guid.NewGuid();
        }


        public void FromByteArray(byte[] data) {
            /* RFC 3489 11.1.             
                All STUN messages consist of a 20 byte header:

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |      STUN Message Type        |         Message Length        |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                        Transaction ID
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                                                               |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
              
               The message length is the count, in bytes, of the size of the
               message, not including the 20 byte header.
            */

            if (data.Length < 20) {
                throw new ArgumentException("Invalid STUN message value.");
            }

            int offset = 0;

            //--- message header --------------------------------------------------

            // STUN Message Type
            int messageType = (data[offset++] << 8 | data[offset++]);
            if (messageType == (int)STUNMessageType.BindingErrorResponse) {
                type = STUNMessageType.BindingErrorResponse;
            }
            else if (messageType == (int)STUNMessageType.BindingRequest) {
                type = STUNMessageType.BindingRequest;
            }
            else if (messageType == (int)STUNMessageType.BindingResponse) {
                type = STUNMessageType.BindingResponse;
            }
            else if (messageType == (int)STUNMessageType.SharedSecretErrorResponse) {
                type = STUNMessageType.SharedSecretErrorResponse;
            }
            else if (messageType == (int)STUNMessageType.SharedSecretRequest) {
                type = STUNMessageType.SharedSecretRequest;
            }
            else if (messageType == (int)STUNMessageType.SharedSecretResponse) {
                type = STUNMessageType.SharedSecretResponse;
            }
            else {
                throw new ArgumentException("Invalid STUN message type value.");
            }

            // Message Length
            int messageLength = (data[offset++] << 8 | data[offset++]);

            // Transaction ID
            byte[] guid = new byte[16];
            Array.Copy(data, offset, guid, 0, 16);
            transactionID = new Guid(guid);
            offset += 16;

            //--- Message attributes ---------------------------------------------
            while ((offset - 20) < messageLength) {
                ParseAttribute(data, ref offset);
            }
        }

        public byte[] ToByteArray() {
            /* RFC 3489 11.1.             
                All STUN messages consist of a 20 byte header:

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |      STUN Message Type        |         Message Length        |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                        Transaction ID
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                                                                               |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             
               The message length is the count, in bytes, of the size of the
               message, not including the 20 byte header.

            */

            // We allocate 512 for header, that should be more than enough.
            byte[] msg = new byte[512];

            int offset = 0;

            //--- message header -------------------------------------

            // STUN Message Type (2 bytes)
            msg[offset++] = (byte)((int)Type >> 8);
            msg[offset++] = (byte)((int)Type & 0xFF);

            // Message Length (2 bytes) will be assigned at last.
            msg[offset++] = 0;
            msg[offset++] = 0;

            // Transaction ID (16 bytes)
            Array.Copy(transactionID.ToByteArray(), 0, msg, offset, 16);
            offset += 16;

            //--- Message attributes ------------------------------------

            /* RFC 3489 11.2.
                After the header are 0 or more attributes.  Each attribute is TLV
                encoded, with a 16 bit type, 16 bit length, and variable value:

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |         Type                  |            Length             |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |                             Value                             ....
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            */

            if (MappedAddress != null) {
                StoreEndPoint(AttributeType.MappedAddress, MappedAddress, msg, ref offset);
            }
            else if (ResponseAddress != null) {
                StoreEndPoint(AttributeType.ResponseAddress, ResponseAddress, msg, ref offset);
            }
            else if (ChangeRequest != null) {
                /*
                    The CHANGE-REQUEST attribute is used by the client to request that
                    the server use a different address and/or port when sending the
                    response.  The attribute is 32 bits long, although only two bits (A
                    and B) are used:

                     0                   1                   2                   3
                     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 A B 0|
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                    The meaning of the flags is:

                    A: This is the "change IP" flag.  If true, it requests the server
                       to send the Binding Response with a different IP address than the
                       one the Binding Request was received on.

                    B: This is the "change port" flag.  If true, it requests the
                       server to send the Binding Response with a different port than the
                       one the Binding Request was received on.
                */

                // Attribute header
                msg[offset++] = (int)AttributeType.ChangeRequest >> 8;
                msg[offset++] = (int)AttributeType.ChangeRequest & 0xFF;
                msg[offset++] = 0;
                msg[offset++] = 4;

                msg[offset++] = 0;
                msg[offset++] = 0;
                msg[offset++] = 0;
                msg[offset++] = (byte)(Convert.ToInt32(ChangeRequest.ChangeIP) << 2 | Convert.ToInt32(ChangeRequest.ChangePort) << 1);
            }
            else if (SourceAddress != null) {
                StoreEndPoint(AttributeType.SourceAddress, SourceAddress, msg, ref offset);
            }
            else if (ChangedAddress != null) {
                StoreEndPoint(AttributeType.ChangedAddress, ChangedAddress, msg, ref offset);
            }
            else if (UserName != null) {
                byte[] userBytes = Encoding.ASCII.GetBytes(UserName);

                // Attribute header
                msg[offset++] = (int)AttributeType.Username >> 8;
                msg[offset++] = (int)AttributeType.Username & 0xFF;
                msg[offset++] = (byte)(userBytes.Length >> 8);
                msg[offset++] = (byte)(userBytes.Length & 0xFF);

                Array.Copy(userBytes, 0, msg, offset, userBytes.Length);
                offset += userBytes.Length;
            }
            else if (Password != null) {
                byte[] passBytes = Encoding.ASCII.GetBytes(Password);

                // Attribute header
                msg[offset++] = (int)AttributeType.Password >> 8;
                msg[offset++] = (int)AttributeType.Password & 0xFF;
                msg[offset++] = (byte)(passBytes.Length >> 8);
                msg[offset++] = (byte)(passBytes.Length & 0xFF);

                Array.Copy(passBytes, 0, msg, offset, passBytes.Length);
                offset += passBytes.Length;
            }
            else if (ErrorCode != null) {
                /* 3489 11.2.9.
                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |                   0                     |Class|     Number    |
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |      Reason Phrase (variable)                                ..
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                */

                byte[] reasonBytes = Encoding.ASCII.GetBytes(ErrorCode.Reason);

                // Header
                msg[offset++] = 0;
                msg[offset++] = (int)AttributeType.ErrorCode;
                msg[offset++] = 0;
                msg[offset++] = (byte)(4 + reasonBytes.Length);

                // Empty
                msg[offset++] = 0;
                msg[offset++] = 0;
                // Class
                msg[offset++] = (byte)Math.Floor((double)(ErrorCode.Code / 100));
                // Number
                msg[offset++] = (byte)(ErrorCode.Code & 0xFF);
                // ReasonPhrase
                Array.Copy(reasonBytes, msg, reasonBytes.Length);
                offset += reasonBytes.Length;
            }
            else if (ReflectedFrom != null) {
                StoreEndPoint(AttributeType.ReflectedFrom, ReflectedFrom, msg, ref offset);
            }

            // Update Message Length. NOTE: 20 bytes header not included.
            msg[2] = (byte)((offset - 20) >> 8);
            msg[3] = (byte)((offset - 20) & 0xFF);

            // Make reatval with actual size.
            byte[] retVal = new byte[offset];
            Array.Copy(msg, retVal, retVal.Length);

            return retVal;
        }

        private void ParseAttribute(byte[] data, ref int offset) {
            /* RFC 3489 11.2.
                Each attribute is TLV encoded, with a 16 bit type, 16 bit length, and variable value:

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |         Type                  |            Length             |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |                             Value                             ....
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+                            
            */

            // Type
            AttributeType type = (AttributeType)(data[offset++] << 8 | data[offset++]);

            // Length
            int length = (data[offset++] << 8 | data[offset++]);

            // MAPPED-ADDRESS
            if (type == AttributeType.MappedAddress) {
                mappedAddress = ParseEndPoint(data, ref offset);
            }
            // RESPONSE-ADDRESS
            else if (type == AttributeType.ResponseAddress) {
                responseAddress = ParseEndPoint(data, ref offset);
            }
            // CHANGE-REQUEST
            else if (type == AttributeType.ChangeRequest) {
                /*
                    The CHANGE-REQUEST attribute is used by the client to request that
                    the server use a different address and/or port when sending the
                    response.  The attribute is 32 bits long, although only two bits (A
                    and B) are used:

                     0                   1                   2                   3
                     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 A B 0|
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

                    The meaning of the flags is:

                    A: This is the "change IP" flag.  If true, it requests the server
                       to send the Binding Response with a different IP address than the
                       one the Binding Request was received on.

                    B: This is the "change port" flag.  If true, it requests the
                       server to send the Binding Response with a different port than the
                       one the Binding Request was received on.
                */

                // Skip 3 bytes
                offset += 3;

                changeRequest = new STUNChangeRequest((data[offset] & 4) != 0, (data[offset] & 2) != 0);
                offset++;
            }
            // SOURCE-ADDRESS
            else if (type == AttributeType.SourceAddress) {
                sourceAddress = ParseEndPoint(data, ref offset);
            }
            // CHANGED-ADDRESS
            else if (type == AttributeType.ChangedAddress) {
                changedAddress = ParseEndPoint(data, ref offset);
            }
            // USERNAME
            else if (type == AttributeType.Username) {
                userName = Encoding.Default.GetString(data, offset, length);
                offset += length;
            }
            // PASSWORD
            else if (type == AttributeType.Password) {
                password = Encoding.Default.GetString(data, offset, length);
                offset += length;
            }
            // MESSAGE-INTEGRITY
            else if (type == AttributeType.MessageIntegrity) {
                offset += length;
            }
            // ERROR-CODE
            else if (type == AttributeType.ErrorCode) {
                /* 3489 11.2.9.
                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |                   0                     |Class|     Number    |
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    |      Reason Phrase (variable)                                ..
                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                */

                int errorCode = (data[offset + 2] & 0x7) * 100 + (data[offset + 3] & 0xFF);

                this.errorCode = new STUNErrorCode(errorCode, Encoding.Default.GetString(data, offset + 4, length - 4));
                offset += length;
            }
            // UNKNOWN-ATTRIBUTES
            else if (type == AttributeType.UnknownAttribute) {
                offset += length;
            }
            // REFLECTED-FROM
            else if (type == AttributeType.ReflectedFrom) {
                reflectedFrom = ParseEndPoint(data, ref offset);
            }
            // XorMappedAddress
            // XorOnly
            // ServerName
            else if (type == AttributeType.ServerName) {
                serverName = Encoding.Default.GetString(data, offset, length);
                offset += length;
            }
            // Unknown
            else {
                offset += length;
            }
        }

        private IPEndPoint ParseEndPoint(byte[] data, ref int offset) {
            /*
                It consists of an eight bit address family, and a sixteen bit
                port, followed by a fixed length value representing the IP address.

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |x x x x x x x x|    Family     |           Port                |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                             Address                           |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            */

            // Family
            IPFamily family = (IPFamily)(data[offset++] << 8 | data[offset++]);

            // Port
            int port = (data[offset++] << 8 | data[offset++]);

            // Address
            byte[] addressBytes = null;
            switch (family) {
                default:
                case IPFamily.IPv4:
                    addressBytes = new byte[IPv4_ADDRESS_LENGTH];
                    break;

                case IPFamily.IPv6:
                    addressBytes = new byte[IPv6_ADDRESS_LENGTH];
                    break;
            }

            for (int b = 0; b < addressBytes.Length; b++) {
                addressBytes[b] = data[offset++];
            }

            return new IPEndPoint(new IPAddress(addressBytes), port);
        }

        private void StoreEndPoint(AttributeType type, IPEndPoint endPoint, byte[] message, ref int offset) {
            /*
                It consists of an eight bit address family, and a sixteen bit
                port, followed by a fixed length value representing the IP address.

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |x x x x x x x x|    Family     |           Port                |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                             Address                           |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+             
            */

            // Header
            message[offset++] = (byte)((int)type >> 8);
            message[offset++] = (byte)((int)type & 0xFF);
            message[offset++] = 0;
            message[offset++] = 8;

            // Unused
            message[offset++] = 0;

            // Family
            switch (endPoint.AddressFamily) {
                default:
                case AddressFamily.InterNetwork:
                    message[offset++] = (byte)IPFamily.IPv4;
                    break;

                case AddressFamily.InterNetworkV6:
                    message[offset++] = (byte)IPFamily.IPv6;
                    break;
            }

            // Port
            message[offset++] = (byte)(endPoint.Port >> 8);
            message[offset++] = (byte)(endPoint.Port & 0xFF);

            // Address
            byte[] addressBytes = endPoint.Address.GetAddressBytes();
            for (int b = 0; b < addressBytes.Length; b++) {
                message[offset++] = addressBytes[b];
            }
        }
    }
}