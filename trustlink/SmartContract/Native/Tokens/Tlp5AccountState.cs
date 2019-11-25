using System.Numerics;
using Trustlink.VM;
using Trustlink.VM.Types;

namespace Trustlink.SmartContract.Native.Tokens
{
    public class Tlp5AccountState
    {
        public BigInteger Balance;

        public Tlp5AccountState()
        {
        }

        public Tlp5AccountState(byte[] data)
        {
            FromByteArray(data);
        }

        public void FromByteArray(byte[] data)
        {
            FromStruct((Struct)data.DeserializeStackItem(16, 34));
        }

        protected virtual void FromStruct(Struct @struct)
        {
            Balance = @struct[0].GetBigInteger();
        }

        public byte[] ToByteArray()
        {
            return ToStruct().Serialize();
        }

        protected virtual Struct ToStruct()
        {
            return new Struct(new StackItem[] { Balance });
        }
    }
}
