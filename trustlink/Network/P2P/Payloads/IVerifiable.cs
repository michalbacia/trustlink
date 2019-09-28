using System.IO;
using Trustlink.IO;
using Trustlink.Persistence;

namespace Trustlink.Network.P2P.Payloads
{
    public interface IVerifiable : ISerializable
    {
        Witness[] Witnesses { get; set; }

        void DeserializeUnsigned(BinaryReader reader);

        UInt160[] GetScriptHashesForVerifying(Snapshot snapshot);

        void SerializeUnsigned(BinaryWriter writer);
    }
}
