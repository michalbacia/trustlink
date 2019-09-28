using System;

namespace Trustlink.SmartContract.Enumerators
{
    internal interface IEnumerator : IDisposable
    {
        bool Next();
        StackItem Value();
    }
}
