using System;
using VMArray = Neo.VM.Types.Array;

namespace Trustlink.SmartContract.Native
{
    internal class ContractMethodMetadata
    {
        public Func<ApplicationEngine, VMArray, StackItem> Delegate;
        public long Price;
    }
}
