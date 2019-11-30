using System;
using System.Collections.Generic;
using Trustlink.Ledger;
using Trustlink.Network.P2P.Payloads;
using Trustlink.Persistence;
using Trustlink.VM;

namespace Trustlink.SmartContract
{
    public partial class ApplicationEngine : ExecutionEngine
    {
        public static event EventHandler<NotifyEventArgs> Notify;
        public static event EventHandler<LogEventArgs> Log;

        public const long LinkFree = 0;
        private readonly long link_amount;
        private readonly bool testMode;
        private readonly List<NotifyEventArgs> notifications = new List<NotifyEventArgs>();
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        public TriggerType Trigger { get; }
        public IVerifiable ScriptContainer { get; }
        public Snapshot Snapshot { get; }
        public long LinkConsumed { get; private set; } = 0;
        public UInt160 CurrentScriptHash => CurrentContext?.GetState<ExecutionContextState>().ScriptHash;
        public UInt160 CallingScriptHash => InvocationStack.Count > 1 ? InvocationStack.Peek(1).GetState<ExecutionContextState>().ScriptHash : null;
        public UInt160 EntryScriptHash => EntryContext?.GetState<ExecutionContextState>().ScriptHash;
        public IReadOnlyList<NotifyEventArgs> Notifications => notifications;
        internal Dictionary<UInt160, int> InvocationCounter { get; } = new Dictionary<UInt160, int>();

        public ApplicationEngine(TriggerType trigger, IVerifiable container, Snapshot snapshot, long gas, bool testMode = false)
        {
            this.link_amount = LinkFree + gas;
            this.testMode = testMode;
            this.Trigger = trigger;
            this.ScriptContainer = container;
            this.Snapshot = snapshot;
        }

        internal T AddDisposable<T>(T disposable) where T : IDisposable
        {
            disposables.Add(disposable);
            return disposable;
        }

        private bool AddLink(long lnk)
        {
            LinkConsumed = checked(LinkConsumed + lnk);
            return testMode || LinkConsumed <= link_amount;
        }

        protected override void LoadContext(ExecutionContext context)
        {
            // Set default execution context state

            context.SetState(new ExecutionContextState()
            {
                ScriptHash = ((byte[])context.Script).ToScriptHash()
            });

            base.LoadContext(context);
        }

        public override void Dispose()
        {
            foreach (IDisposable disposable in disposables)
                disposable.Dispose();
            disposables.Clear();
            base.Dispose();
        }

        protected override bool OnSysCall(uint method)
        {
            if (!AddLink(InteropService.GetPrice(method, CurrentContext.EvaluationStack)))
                return false;
            return InteropService.Invoke(this, method);
        }

        protected override bool PreExecuteInstruction()
        {
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length)
                return true;
            return AddLink(OpCodePrices[CurrentContext.CurrentInstruction.OpCode]);
        }

        private static Block CreateDummyBlock(Snapshot snapshot)
        {
            var currentBlock = snapshot.Blocks[snapshot.CurrentBlockHash];
            return new Block
            {
                Version = 0,
                PrevHash = snapshot.CurrentBlockHash,
                MerkleRoot = new UInt256(),
                Timestamp = currentBlock.Timestamp + Blockchain.MillisecondsPerBlock,
                Index = snapshot.Height + 1,
                NextConsensus = currentBlock.NextConsensus,
                Witness = new Witness
                {
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                },
                ConsensusData = new ConsensusData(),
                Transactions = new Transaction[0]
            };
        }

        public static ApplicationEngine Run(byte[] script, Snapshot snapshot,
            IVerifiable container = null, Block persistingBlock = null, bool testMode = false, long extraLINK = default)
        {
            snapshot.PersistingBlock = persistingBlock ?? snapshot.PersistingBlock ?? CreateDummyBlock(snapshot);
            ApplicationEngine engine = new ApplicationEngine(TriggerType.Application, container, snapshot, extraLINK, testMode);
            engine.LoadScript(script);
            engine.Execute();
            return engine;
        }

        public static ApplicationEngine Run(byte[] script, IVerifiable container = null, Block persistingBlock = null, bool testMode = false, long extraLINK = default)
        {
            using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
            {
                return Run(script, snapshot, container, persistingBlock, testMode, extraLINK);
            }
        }

        internal void SendLog(UInt160 script_hash, string message)
        {
            LogEventArgs log = new LogEventArgs(ScriptContainer, script_hash, message);
            Log?.Invoke(this, log);
        }

        internal void SendNotification(UInt160 script_hash, StackItem state)
        {
            NotifyEventArgs notification = new NotifyEventArgs(ScriptContainer, script_hash, state);
            Notify?.Invoke(this, notification);
            notifications.Add(notification);
        }
    }
}
