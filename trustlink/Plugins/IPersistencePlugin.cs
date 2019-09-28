using System;
using System.Collections.Generic;
using Trustlink.Persistence;
using static Trustlink.Ledger.Blockchain;

namespace Trustlink.Plugins
{
    public interface IPersistencePlugin
    {
        void OnPersist(Snapshot snapshot, IReadOnlyList<ApplicationExecuted> applicationExecutedList);
        void OnCommit(Snapshot snapshot);
        bool ShouldThrowExceptionFromCommit(Exception ex);
    }
}
