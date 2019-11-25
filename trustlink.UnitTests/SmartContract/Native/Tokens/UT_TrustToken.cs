using System;
using System.Linq;
using System.Numerics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.Cryptography.ECC;
using Trustlink.IO.Caching;
using Trustlink.Ledger;
using Trustlink.Network.P2P.Payloads;
using Trustlink.Persistence;
using Trustlink.SmartContract;
using Trustlink.SmartContract.Native;
using Trustlink.UnitTests.Extensions;
using Trustlink.VM;

namespace Trustlink.UnitTests.SmartContract.Native.Tokens
{
    [TestClass]
    public class UT_TrustToken
    {
        private Store Store;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            Store = TestBlockchain.GetStore();
        }

        [TestMethod]
        public void Check_Name() => NativeContract.TRUST.Name().Should().Be("NEO");

        [TestMethod]
        public void Check_Symbol() => NativeContract.TRUST.Symbol().Should().Be("neo");

        [TestMethod]
        public void Check_Decimals() => NativeContract.TRUST.Decimals().Should().Be(0);

        [TestMethod]
        public void Check_SupportedStandards() => NativeContract.TRUST.SupportedStandards().Should().BeEquivalentTo(new string[] { "NEP-5", "NEP-10" });

        [TestMethod]
        public void Check_Vote()
        {
            var snapshot = Store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            // No signature

            var ret = Check_Vote(snapshot, from, new byte[][] { }, false);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeTrue();

            // Wrong address

            ret = Check_Vote(snapshot, new byte[19], new byte[][] { }, false);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeFalse();

            // Wrong ec

            ret = Check_Vote(snapshot, from, new byte[][] { new byte[19] }, true);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeFalse();

            // no registered

            var fakeAddr = new byte[20];
            fakeAddr[0] = 0x5F;
            fakeAddr[5] = 0xFF;

            ret = Check_Vote(snapshot, fakeAddr, new byte[][] { }, true);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeTrue();

            // TODO: More votes tests
        }

        [TestMethod]
        public void Check_UnclaimedGas()
        {
            var snapshot = Store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            var unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(600000000000));
            unclaim.State.Should().BeTrue();

            unclaim = Check_UnclaimedGas(snapshot, new byte[19]);
            unclaim.Value.Should().Be(BigInteger.Zero);
            unclaim.State.Should().BeFalse();
        }

        [TestMethod]
        public void Check_RegisterValidator()
        {
            var snapshot = Store.GetSnapshot().Clone();

            var ret = Check_RegisterValidator(snapshot, new byte[0]);
            ret.State.Should().BeFalse();
            ret.Result.Should().BeFalse();

            ret = Check_RegisterValidator(snapshot, new byte[33]);
            ret.State.Should().BeFalse();
            ret.Result.Should().BeFalse();

            var keyCount = snapshot.Storages.GetChangeSet().Count();
            var point = Blockchain.StandbyValidators[0].EncodePoint(true);

            ret = Check_RegisterValidator(snapshot, point); // Exists
            ret.State.Should().BeTrue();
            ret.Result.Should().BeFalse();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount); // No changes

            point[20]++; // fake point
            ret = Check_RegisterValidator(snapshot, point); // New

            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 1); // New validator

            // Check GetRegisteredValidators

            var validators = NativeContract.TRUST.GetRegisteredValidators(snapshot).OrderBy(u => u.PublicKey).ToArray();
            var check = Blockchain.StandbyValidators.Select(u => u.EncodePoint(true)).ToList();
            check.Add(point); // Add the new member

            for (int x = 0; x < validators.Length; x++)
            {
                Assert.AreEqual(1, check.RemoveAll(u => u.SequenceEqual(validators[x].PublicKey.EncodePoint(true))));
                Assert.AreEqual(0, validators[x].Votes);
            }

            Assert.AreEqual(0, check.Count);
        }

        [TestMethod]
        public void Check_Transfer()
        {
            var snapshot = Store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            byte[] to = new byte[20];

            var keyCount = snapshot.Storages.GetChangeSet().Count();

            // Check unclaim

            var unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(600000000000));
            unclaim.State.Should().BeTrue();

            // Transfer

            NativeContract.TRUST.Transfer(snapshot, from, to, BigInteger.One, false).Should().BeFalse(); // Not signed
            NativeContract.TRUST.Transfer(snapshot, from, to, BigInteger.One, true).Should().BeTrue();
            NativeContract.TRUST.BalanceOf(snapshot, from).Should().Be(99_999_999);
            NativeContract.TRUST.BalanceOf(snapshot, to).Should().Be(1);

            // Check unclaim

            unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(0));
            unclaim.State.Should().BeTrue();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 4); // Gas + new balance

            // Return balance

            keyCount = snapshot.Storages.GetChangeSet().Count();

            NativeContract.TRUST.Transfer(snapshot, to, from, BigInteger.One, true).Should().BeTrue();
            NativeContract.TRUST.BalanceOf(snapshot, to).Should().Be(0);
            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount - 1);  // Remove neo balance from address two

            // Bad inputs

            NativeContract.TRUST.Transfer(snapshot, from, to, BigInteger.MinusOne, true).Should().BeFalse();
            NativeContract.TRUST.Transfer(snapshot, new byte[19], to, BigInteger.One, false).Should().BeFalse();
            NativeContract.TRUST.Transfer(snapshot, from, new byte[19], BigInteger.One, false).Should().BeFalse();

            // More than balance

            NativeContract.TRUST.Transfer(snapshot, to, from, new BigInteger(2), true).Should().BeFalse();
        }

        [TestMethod]
        public void Check_BalanceOf()
        {
            var snapshot = Store.GetSnapshot().Clone();
            byte[] account = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            NativeContract.TRUST.BalanceOf(snapshot, account).Should().Be(100_000_000);

            account[5]++; // Without existing balance

            NativeContract.TRUST.BalanceOf(snapshot, account).Should().Be(0);
        }

        [TestMethod]
        public void Check_Initialize()
        {
            var snapshot = Store.GetSnapshot().Clone();

            // StandbyValidators

            var validators = Check_GetValidators(snapshot);

            for (var x = 0; x < Blockchain.StandbyValidators.Length; x++)
            {
                validators[x].Equals(Blockchain.StandbyValidators[x]);
            }

            // Check double call

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);

            engine.LoadScript(NativeContract.TRUST.Script);

            var result = NativeContract.TRUST.Initialize(engine);

            result.Should().Be(false);
        }

        [TestMethod]
        public void Check_BadScript()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), 0);

            var script = new ScriptBuilder();
            script.Emit(OpCode.NOP);
            engine.LoadScript(script.ToArray());

            NativeContract.TRUST.Invoke(engine).Should().BeFalse();
        }

        internal static (bool State, bool Result) Check_Vote(Snapshot snapshot, byte[] account, byte[][] pubkeys, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Tlp5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.TRUST.Script);

            var script = new ScriptBuilder();

            foreach (var ec in pubkeys) script.EmitPush(ec);
            script.EmitPush(pubkeys.Length);
            script.Emit(OpCode.PACK);

            script.EmitPush(account.ToArray());
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush("vote");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, (result as VM.Types.Boolean).GetBoolean());
        }

        internal static (bool State, bool Result) Check_RegisterValidator(Snapshot snapshot, byte[] pubkey)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);

            engine.LoadScript(NativeContract.TRUST.Script);

            var script = new ScriptBuilder();
            script.EmitPush(pubkey);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("registerValidator");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, (result as VM.Types.Boolean).GetBoolean());
        }

        internal static ECPoint[] Check_GetValidators(Snapshot snapshot)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);

            engine.LoadScript(NativeContract.TRUST.Script);

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("getValidators");
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));

            return (result as VM.Types.Array).Select(u => u.GetByteArray().AsSerializable<ECPoint>()).ToArray();
        }

        internal static (BigInteger Value, bool State) Check_UnclaimedGas(Snapshot snapshot, byte[] address)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);

            engine.LoadScript(NativeContract.TRUST.Script);

            var script = new ScriptBuilder();
            script.EmitPush(snapshot.PersistingBlock.Index);
            script.EmitPush(address);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush("unclaimedGas");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (BigInteger.Zero, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return ((result as VM.Types.Integer).GetBigInteger(), true);
        }

        internal static void CheckValidator(ECPoint eCPoint, DataCache<StorageKey, StorageItem>.Trackable trackable)
        {
            var st = new BigInteger(trackable.Item.Value);
            st.Should().Be(0);

            trackable.Key.Key.Should().BeEquivalentTo(new byte[] { 33 }.Concat(eCPoint.EncodePoint(true)));
            trackable.Item.IsConstant.Should().Be(false);
        }

        internal static void CheckBalance(byte[] account, DataCache<StorageKey, StorageItem>.Trackable trackable, BigInteger balance, BigInteger height, ECPoint[] votes)
        {
            var st = (VM.Types.Struct)trackable.Item.Value.DeserializeStackItem(3, 32);

            st.Count.Should().Be(3);
            st.Select(u => u.GetType()).ToArray().Should().BeEquivalentTo(new Type[] { typeof(VM.Types.Integer), typeof(VM.Types.Integer), typeof(VM.Types.ByteArray) }); // Balance

            st[0].GetBigInteger().Should().Be(balance); // Balance
            st[1].GetBigInteger().Should().Be(height);  // BalanceHeight
            (st[2].GetByteArray().AsSerializableArray<ECPoint>(Blockchain.MaxValidators)).Should().BeEquivalentTo(votes);  // Votes

            trackable.Key.Key.Should().BeEquivalentTo(new byte[] { 20 }.Concat(account));
            trackable.Item.IsConstant.Should().Be(false);
        }
    }
}
