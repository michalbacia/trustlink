using System;
using Trustlink.VM;
using VMArray = Trustlink.VM.Types.Array;

namespace Trustlink.SmartContract.Native
{
    internal class ContractMethodMetadata
    {
        public Func<ApplicationEngine, VMArray, StackItem> Delegate;
        public long Price;
    }
}
