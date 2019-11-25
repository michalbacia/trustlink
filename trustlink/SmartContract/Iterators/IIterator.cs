using Trustlink.SmartContract.Enumerators;
using Trustlink.VM;

namespace Trustlink.SmartContract.Iterators
{
    internal interface IIterator : IEnumerator
    {
        StackItem Key();
    }
}
