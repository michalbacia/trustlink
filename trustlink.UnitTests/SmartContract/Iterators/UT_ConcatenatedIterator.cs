using System.Numerics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Trustlink.UnitTests.SmartContract.Iterators
{

    [TestClass]
    public class UT_ConcatenatedIterator
    {
        [TestMethod]
        public void ConcatenatedIteratedOverflowTest()
        {
            Integer[] array1 = { MakeIntegerStackItem(1) };
            ArrayWrapper it1 = new ArrayWrapper(array1);
            ArrayWrapper it2 = new ArrayWrapper(array1);
            ConcatenatedIterator uut = new ConcatenatedIterator(it1, it2);

            AssertionExtensions.Should((bool) uut.Next()).Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(0));
            uut.Value().Should().Be(array1[0]);

            AssertionExtensions.Should((bool) uut.Next()).Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(0));
            uut.Value().Should().Be(array1[0]);

            AssertionExtensions.Should((bool) uut.Next()).Be(false);
        }

        [TestMethod]
        public void ConcatenatedIteratedTest()
        {
            Integer[] array1 = { MakeIntegerStackItem(1), MakeIntegerStackItem(7), MakeIntegerStackItem(23) };
            Integer[] array2 = { MakeIntegerStackItem(8), MakeIntegerStackItem(47) };
            ArrayWrapper it1 = new ArrayWrapper(array1);
            ArrayWrapper it2 = new ArrayWrapper(array2);
            ConcatenatedIterator uut = new ConcatenatedIterator(it1, it2);

            AssertionExtensions.Should((bool) uut.Next()).Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(0));
            uut.Value().Should().Be(array1[0]);

            AssertionExtensions.Should((bool) uut.Next()).Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(1));
            uut.Value().Should().Be(array1[1]);

            AssertionExtensions.Should((bool) uut.Next()).Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(2));
            uut.Value().Should().Be(array1[2]);

            AssertionExtensions.Should((bool) uut.Next()).Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(0));
            uut.Value().Should().Be(array2[0]);

            AssertionExtensions.Should((bool) uut.Next()).Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(1));
            uut.Value().Should().Be(array2[1]);

            AssertionExtensions.Should((bool) uut.Next()).Be(false);
        }

        private Integer MakeIntegerStackItem(int val)
        {
            return new Integer(new BigInteger(val));
        }
    }
}
