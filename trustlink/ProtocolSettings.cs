using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace Trustlink
{
    public class ProtocolSettings
    {
        public uint Magic { get; }
        public byte AddressVersion { get; }
        public string[] StandbyValidators { get; }
        public string[] SeedList { get; }
        public uint MillisecondsPerBlock { get; }
        public int MemoryPoolMaxTransactions { get; }

        static ProtocolSettings _default;

        static bool UpdateDefault(IConfiguration configuration)
        {
            var settings = new ProtocolSettings(configuration.GetSection("ProtocolConfiguration"));
            return null == Interlocked.CompareExchange(ref _default, settings, null);
        }

        public static bool Initialize(IConfiguration configuration)
        {
            return UpdateDefault(configuration);
        }

        public static ProtocolSettings Default
        {
            get
            {
                if (_default == null)
                {
                    var configuration = Helper.LoadConfig("protocol");
                    UpdateDefault(configuration);
                }

                return _default;
            }
        }

        private ProtocolSettings(IConfigurationSection section)
        {
            this.Magic = section.GetValue("Magic", 0x4F454Eu);
            this.AddressVersion = section.GetValue("AddressVersion", (byte)0x41);
            IConfigurationSection section_sv = section.GetSection("StandbyValidators");
            if (section_sv.Exists())
                this.StandbyValidators = section_sv.GetChildren().Select(p => p.Get<string>()).ToArray();
            else
                this.StandbyValidators = new[]
                {
                    "022238a1c7a47d7b2f8224bbb45ce6f384ba457b7c53ef034f2d6caa4ed98c72c0",
                    "02becba1549d34180818d402036ef7c91270fc95564f63e8d00e0db962f1f239be",
                    "02b9a6abac3e886686b27d986f7c3911ebacb902dac500b18d977baa3a928b2aa7",
                    "03b2ece5dabb96009740d3d766dbb67e08f72bb0ee01ea0a3c09a038d7159595a7",
                    "02ea3415771494c8b99646a58debe3ab2a07c7a9ce85be5d5dfda440e6ce11fb84"


                };
            IConfigurationSection section_sl = section.GetSection("SeedList");
            if (section_sl.Exists())
                this.SeedList = section_sl.GetChildren().Select(p => p.Get<string>()).ToArray();
            else
                this.SeedList = new[]
                {
                    "seed1.trustlink.tech:2011",
                    "seed2.trustlink.tech:2011",
                    "seed3.trustlink.tech:2011",
                    "seed4.trustlink.tech:2011"
                };
            this.MillisecondsPerBlock = section.GetValue("MillisecondsPerBlock", 15000u);
            this.MemoryPoolMaxTransactions = Math.Max(1, section.GetValue("MemoryPoolMaxTransactions", 50_000));
        }
    }
}
