using Trustlink.SmartContract.Enumerators;

namespace Trustlink.SmartContract.Iterators
{
    internal interface IIterator : IEnumerator
    {
        StackItem Key();
    }
}
