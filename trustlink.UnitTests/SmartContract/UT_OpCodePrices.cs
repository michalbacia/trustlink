using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.SmartContract;

namespace Trustlink.UnitTests.SmartContract
{
    [TestClass]
    public class UT_OpCodePrices
    {
        [TestMethod]
        public void AllOpcodePriceAreSet()
        {
            foreach (OpCode opcode in Enum.GetValues(typeof(OpCode)))
                Assert.IsTrue(ApplicationEngine.OpCodePrices.ContainsKey(opcode));
        }
    }
}
