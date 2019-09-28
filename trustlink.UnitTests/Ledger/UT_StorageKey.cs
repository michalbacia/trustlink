using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.IO;
using Trustlink.Ledger;

namespace Trustlink.UnitTests.Ledger
{
    [TestClass]
    public class UT_StorageKey
    {
        StorageKey uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new StorageKey();
        }

        [TestMethod]
        public void ScriptHash_Get()
        {
            uut.ScriptHash.Should().BeNull();
        }

        [TestMethod]
        public void Size()
        {
            var ut = new StorageKey() { Key = new byte[17], ScriptHash = UInt160.Zero };
            ut.ToArray().Length.Should().Be(((ISerializable)ut).Size);

            ut = new StorageKey() { Key = new byte[0], ScriptHash = UInt160.Zero };
            ut.ToArray().Length.Should().Be(((ISerializable)ut).Size);

            ut = new StorageKey() { Key = new byte[16], ScriptHash = UInt160.Zero };
            ut.ToArray().Length.Should().Be(((ISerializable)ut).Size);
        }

        [TestMethod]
        public void ScriptHash_Set()
        {
            UInt160 val = new UInt160(TestUtils.GetByteArray(20, 0x42));
            uut.ScriptHash = val;
            uut.ScriptHash.Should().Be(val);
        }

        [TestMethod]
        public void Key_Get()
        {
            uut.Key.Should().BeNull();
        }

        [TestMethod]
        public void Key_Set()
        {
            byte[] val = new byte[] { 0x42, 0x32 };
            uut.Key = val;
            uut.Key.Length.Should().Be(2);
            uut.Key[0].Should().Be(val[0]);
            uut.Key[1].Should().Be(val[1]);
        }

        [TestMethod]
        public void Equals_SameObj()
        {
            uut.Equals(uut).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_Null()
        {
            uut.Equals(null).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_SameHash_SameKey()
        {
            UInt160 val = new UInt160(TestUtils.GetByteArray(20, 0x42));
            byte[] keyVal = TestUtils.GetByteArray(10, 0x42);
            StorageKey newSk = new StorageKey
            {
                ScriptHash = val,
                Key = keyVal
            };
            uut.ScriptHash = val;
            uut.Key = keyVal;

            uut.Equals(newSk).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_DiffHash_SameKey()
        {
            UInt160 val = new UInt160(TestUtils.GetByteArray(20, 0x42));
            byte[] keyVal = TestUtils.GetByteArray(10, 0x42);
            StorageKey newSk = new StorageKey
            {
                ScriptHash = val,
                Key = keyVal
            };
            uut.ScriptHash = new UInt160(TestUtils.GetByteArray(20, 0x88));
            uut.Key = keyVal;

            uut.Equals(newSk).Should().BeFalse();
        }


        [TestMethod]
        public void Equals_SameHash_DiffKey()
        {
            UInt160 val = new UInt160(TestUtils.GetByteArray(20, 0x42));
            byte[] keyVal = TestUtils.GetByteArray(10, 0x42);
            StorageKey newSk = new StorageKey
            {
                ScriptHash = val,
                Key = keyVal
            };
            uut.ScriptHash = val;
            uut.Key = TestUtils.GetByteArray(10, 0x88);

            uut.Equals(newSk).Should().BeFalse();
        }

        [TestMethod]
        public void GetHashCode_Get()
        {
            uut.ScriptHash = new UInt160(TestUtils.GetByteArray(20, 0x42));
            uut.Key = TestUtils.GetByteArray(10, 0x42);
            uut.GetHashCode().Should().Be(806209853);
        }
    }
}
