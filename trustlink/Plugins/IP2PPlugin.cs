using Trustlink.Network.P2P;
using Trustlink.Network.P2P.Payloads;

namespace Trustlink.Plugins
{
    public interface IP2PPlugin
    {
        bool OnP2PMessage(Message message);
        bool OnConsensusMessage(ConsensusPayload payload);
    }
}
