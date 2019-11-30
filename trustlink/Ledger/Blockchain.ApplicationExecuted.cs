using System.Linq;
using Trustlink.Network.P2P.Payloads;
using Trustlink.SmartContract;
using Trustlink.VM;

namespace Trustlink.Ledger
{
    partial class Blockchain
    {
        partial class ApplicationExecuted
        {
            public Transaction Transaction;
            public TriggerType Trigger { get; internal set; }
            public VMState VMState { get; internal set; }
            public long LinkConsumed { get; internal set; }
            public StackItem[] Stack { get; internal set; }
            public NotifyEventArgs[] Notifications { get; internal set; }

            internal ApplicationExecuted(ApplicationEngine engine)
            {
                Transaction = engine.ScriptContainer as Transaction;
                Trigger = engine.Trigger;
                VMState = engine.State;
                LinkConsumed = engine.LinkConsumed;
                Stack = engine.ResultStack.ToArray();
                Notifications = engine.Notifications.ToArray();
            }
        }
    }
}
