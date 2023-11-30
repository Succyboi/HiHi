using HiHi.Common;
using System.Net;

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
namespace HiHi.Serialization {
    public static class SerializationHelper {
        // I love Pattern matching >:3 - Pelle
        public static void Serialize<T>(this T value, BitBuffer buffer) {
            switch (value) {
                case bool explicitValue:
                    explicitValue.Serialize(buffer);
                    return;

                case byte explicitValue:
                    explicitValue.Serialize(buffer);
                    return;

                case short explicitValue:
                    explicitValue.Serialize(buffer);
                    return;

                case ushort explicitValue:
                    explicitValue.Serialize(buffer);
                    return;

                case int explicitValue:
                    explicitValue.Serialize(buffer);
                    return;

                case uint explicitValue:
                    explicitValue.Serialize(buffer);
                    return;

                case long explicitValue:
                    explicitValue.Serialize(buffer);
                    return;

                case ulong explicitValue:
                    explicitValue.Serialize(buffer);
                    return;

                case string explicitValue:
                    explicitValue.Serialize(buffer);
                    return;

                case ISerializable explicitValue:
                    explicitValue.Serialize(buffer);
                    return;
            }

            throw new HiHiException($"Tried to deserialize and failed. Use Implement {nameof(ISerializable)}.");
        }
        public static T Deserialize<T>(this T value, BitBuffer buffer) {
            switch (value) {
                case bool explicitValue:
                    explicitValue = explicitValue.Deserialize(buffer);
                    if (explicitValue is T TFromBoolValue) { return TFromBoolValue; }
                    break;

                case byte explicitValue:
                    explicitValue = explicitValue.Deserialize(buffer);
                    if (explicitValue is T TFromByteValue) { return TFromByteValue; }
                    break;

                case short explicitValue:
                    explicitValue = explicitValue.Deserialize(buffer);
                    if (explicitValue is T TFromShortValue) { return TFromShortValue; }
                    break;

                case ushort explicitValue:
                    explicitValue = explicitValue.Deserialize(buffer);
                    if (explicitValue is T TFromUShortValue) { return TFromUShortValue; }
                    break;

                case int explicitValue:
                    explicitValue = explicitValue.Deserialize(buffer);
                    if (explicitValue is T TFromIntValue) { return TFromIntValue; }
                    break;

                case uint explicitValue:
                    explicitValue = explicitValue.Deserialize(buffer);
                    if (explicitValue is T TFromUIntValue) { return TFromUIntValue; }
                    break;

                case long explicitValue:
                    explicitValue = explicitValue.Deserialize(buffer);
                    if (explicitValue is T TFromLongValue) { return TFromLongValue; }
                    break;

                case ulong explicitValue:
                    explicitValue = explicitValue.Deserialize(buffer);
                    if (explicitValue is T TFromULongValue) { return TFromULongValue; }
                    break;

                case string explicitValue:
                    explicitValue = explicitValue.Deserialize(buffer);
                    if (explicitValue is T TFromStringValue) { return TFromStringValue; }
                    break;

                case ISerializable explicitValue:
                    explicitValue = Deserialize(explicitValue, buffer);
                    if (explicitValue is T TFromISerializableValue) { return TFromISerializableValue; }
                    break;
            }

            throw new HiHiException($"Tried to deserialize and failed. Use Implement {nameof(ISerializable)}.");
        }

        #region Serialize

        private static void Serialize(this bool value, BitBuffer buffer) => buffer.AddBool(value);
        private static void Serialize(this byte value, BitBuffer buffer) => buffer.AddByte(value);
        private static void Serialize(this short value, BitBuffer buffer) => buffer.AddShort(value);
        private static void Serialize(this ushort value, BitBuffer buffer) => buffer.AddUShort(value);
        private static void Serialize(this int value, BitBuffer buffer) => buffer.AddInt(value);
        private static void Serialize(this uint value, BitBuffer buffer) => buffer.AddUInt(value);
        private static void Serialize(this long value, BitBuffer buffer) => buffer.AddLong(value);
        private static void Serialize(this ulong value, BitBuffer buffer) => buffer.AddULong(value);
        private static void Serialize(this string value, BitBuffer buffer) => buffer.AddString(value);
        private static void Serialize(this ISerializable value, BitBuffer buffer) => value.Serialize(buffer);

        #endregion

        #region Deserialize

        private static bool Deserialize(this bool value, BitBuffer buffer) => buffer.ReadBool();
        private static byte Deserialize(this byte value, BitBuffer buffer) => buffer.ReadByte();
        private static short Deserialize(this short value, BitBuffer buffer) => buffer.ReadShort();
        private static ushort Deserialize(this ushort value, BitBuffer buffer) => buffer.ReadUShort();
        private static int Deserialize(this int value, BitBuffer buffer) => buffer.ReadInt();
        private static uint Deserialize(this uint value, BitBuffer buffer) => buffer.ReadUInt();
        private static long Deserialize(this long value, BitBuffer buffer) => buffer.ReadLong();
        private static ulong Deserialize(this ulong value, BitBuffer buffer) => buffer.ReadULong();
        private static string Deserialize(this string value, BitBuffer buffer) => buffer.ReadString();
        private static ISerializable Deserialize(this ISerializable value, BitBuffer buffer) {
            value.Deserialize(buffer);
            return value;
        }

        #endregion
    }
}
