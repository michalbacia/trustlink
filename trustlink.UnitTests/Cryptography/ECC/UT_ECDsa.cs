using System;
using System.Numerics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.Wallets;
using ECDsa = Trustlink.Cryptography.ECC.ECDsa;

namespace Trustlink.UnitTests.Cryptography.ECC
{
    [TestClass]
    public class UT_ECDsa
    {
        private KeyPair key = null;

        [TestInitialize]
        public void TestSetup()
        {
            key = UT_Crypto.generateCertainKey(32);
        }

        [TestMethod]
        public void TestECDsaConstructor()
        {
            Action action = () => new ECDsa(key.PublicKey);
            action.ShouldNotThrow();
            action = () => new ECDsa(key.PrivateKey, key.PublicKey.Curve);
            action.ShouldNotThrow();
        }

        [TestMethod]
        public void TestGenerateSignature()
        {
            ECDsa sa = new ECDsa(key.PrivateKey, key.PublicKey.Curve);
            byte[] message = System.Text.Encoding.Default.GetBytes("HelloWorld");
            for (int i = 0; i < 30; i++)
            {
                BigInteger[] result = sa.GenerateSignature(message);
                result.Length.Should().Be(2);
            }
            sa = new ECDsa(key.PublicKey);
            Action action = () => sa.GenerateSignature(message);
            action.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public void TestVerifySignature()
        {
            ECDsa sa = new ECDsa(key.PrivateKey, key.PublicKey.Curve);
            byte[] message = System.Text.Encoding.Default.GetBytes("HelloWorld");
            BigInteger[] result = sa.GenerateSignature(message);
            sa.VerifySignature(message, result[0], result[1]).Should().BeTrue();
            sa.VerifySignature(message, new BigInteger(-100), result[1]).Should().BeFalse();
        }
    }
}
