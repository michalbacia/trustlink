using System;

namespace Trustlink.Ledger
{
    [Flags]
    public enum StorageFlags : byte
    {
        None = 0,
        Constant = 0x01
    }
}
