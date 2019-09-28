using System;

namespace Trustlink.Network.P2P
{
    [Flags]
    public enum MessageFlags : byte
    {
        None = 0,
        Compressed = 1 << 0
    }
}
