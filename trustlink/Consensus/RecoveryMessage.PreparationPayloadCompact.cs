using System.IO;
using Trustlink.IO;
using Trustlink.Network.P2P.Payloads;

namespace Trustlink.Consensus
{
    partial class RecoveryMessage
    {
        public class PreparationPayloadCompact : ISerializable
        {
            public ushort ValidatorIndex;
            public byte[] InvocationScript;

            int ISerializable.Size =>
                sizeof(ushort) +                //ValidatorIndex
                InvocationScript.GetVarSize();  //InvocationScript

            void ISerializable.Deserialize(BinaryReader reader)
            {
                ValidatorIndex = reader.ReadUInt16();
                InvocationScript = reader.ReadVarBytes(1024);
            }

            public static PreparationPayloadCompact FromPayload(ConsensusPayload payload)
            {
                return new PreparationPayloadCompact
                {
                    ValidatorIndex = payload.ValidatorIndex,
                    InvocationScript = payload.Witness.InvocationScript
                };
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ValidatorIndex);
                writer.WriteVarBytes(InvocationScript);
            }
        }
    }
}
