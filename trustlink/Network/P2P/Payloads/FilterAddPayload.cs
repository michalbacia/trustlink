using System.IO;
using Trustlink.IO;

namespace Trustlink.Network.P2P.Payloads
{
    public class FilterAddPayload : ISerializable
    {
        public byte[] Data;

        public int Size => Data.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Data = reader.ReadVarBytes(520);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Data);
        }
    }
}
