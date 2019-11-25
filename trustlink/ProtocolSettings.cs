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
                    "02466d6d8851ce461fce2f04a790e616c71a1959af3beda65178329297555115a8",
                    "0398ed0f590705cb99bd85a3348cb1e204a07e33ca2d1f34d9684314bea2fbfbfe",
                    "029d012be2c28defd95ab3b3585ca24f38606140362bb4818e0d143829ce8afda1",
                    "03b470cbaa92efc669a73e5eaa05c60d69fbc9a28511594ec8eb5bf850f2a4419c",
                    "0339bf71916d954d8361063160c210d9b8ce130b884c63cb2e2bcbbca16fced245"


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
