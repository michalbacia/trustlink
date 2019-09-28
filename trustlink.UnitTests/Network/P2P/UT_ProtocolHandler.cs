using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.Network.P2P;
using Trustlink.Network.P2P.Capabilities;
using Trustlink.Network.P2P.Payloads;

namespace Trustlink.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_ProtocolHandler : TestKit
    {
        private NeoSystem testBlockchain;

        [TestCleanup]
        public void Cleanup()
        {
            Shutdown();
        }

        [TestInitialize]
        public void TestSetup()
        {
            testBlockchain = TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void ProtocolHandler_Test_SendVersion_TellParent()
        {
            var senderProbe = CreateTestProbe();
            var parent = CreateTestProbe();
            var protocolActor = ActorOfAsTestActorRef<ProtocolHandler>(() => new ProtocolHandler(testBlockchain), parent);

            var payload = new VersionPayload()
            {
                UserAgent = "".PadLeft(1024, '0'),
                Nonce = 1,
                Magic = 2,
                Timestamp = 5,
                Version = 6,
                Capabilities = new NodeCapability[]
                {
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                }
            };

            senderProbe.Send(protocolActor, Message.Create(MessageCommand.Version, payload));
            parent.ExpectMsg<VersionPayload>();
        }
    }
}
