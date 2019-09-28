using System.Linq;
using Trustlink.IO.Json;
using Trustlink.SmartContract;

namespace Trustlink.Wallets.NEP6
{
    internal class TLP6Contract : Contract
    {
        public string[] ParameterNames;
        public bool Deployed;

        public static TLP6Contract FromJson(JObject json)
        {
            if (json == null) return null;
            return new TLP6Contract
            {
                Script = json["script"].AsString().HexToBytes(),
                ParameterList = ((JArray)json["parameters"]).Select(p => p["type"].TryGetEnum<ContractParameterType>()).ToArray(),
                ParameterNames = ((JArray)json["parameters"]).Select(p => p["name"].AsString()).ToArray(),
                Deployed = json["deployed"].AsBoolean()
            };
        }

        public JObject ToJson()
        {
            JObject contract = new JObject();
            contract["script"] = Script.ToHexString();
            contract["parameters"] = new JArray(ParameterList.Zip(ParameterNames, (type, name) =>
            {
                JObject parameter = new JObject();
                parameter["name"] = name;
                parameter["type"] = type;
                return parameter;
            }));
            contract["deployed"] = Deployed;
            return contract;
        }
    }
}
