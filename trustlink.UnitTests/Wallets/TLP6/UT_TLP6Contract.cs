using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.IO.Json;
using Trustlink.SmartContract;
using Trustlink.Wallets.TLP6;

namespace Trustlink.UnitTests.Wallets.TLP6
{
    [TestClass]
    public class UT_TLP6Contract
    {
        [TestMethod]
        public void TestFromNullJson()
        {
            var tlp6Contract = TLP6Contract.FromJson(null);
            tlp6Contract.Should().BeNull();
        }

        [TestMethod]
        public void TestFromJson()
        {
            string json = "{\"script\":\"2103ef891df4c0b7eefb937d21ea0fb88cde8e0d82a7ff11872b5e7047969dafb4eb68747476aa\"," +
                "\"parameters\":[{\"name\":\"signature\",\"type\":\"Signature\"}],\"deployed\":false}";
            JObject @object = JObject.Parse(json);

            var tlp6Contract = TLP6Contract.FromJson(@object);
            tlp6Contract.Script.Should().BeEquivalentTo("2103ef891df4c0b7eefb937d21ea0fb88cde8e0d82a7ff11872b5e7047969dafb4eb68747476aa".HexToBytes());
            tlp6Contract.ParameterList.Length.Should().Be(1);
            tlp6Contract.ParameterList[0].Should().Be(ContractParameterType.Signature);
            tlp6Contract.ParameterNames.Length.Should().Be(1);
            tlp6Contract.ParameterNames[0].Should().Be("signature");
            tlp6Contract.Deployed.Should().BeFalse();
        }

        [TestMethod]
        public void TestToJson()
        {
            var nep6Contract = new TLP6Contract()
            {
                Script = new byte[] { 0x00, 0x01 },
                ParameterList = new ContractParameterType[] { ContractParameterType.Boolean, ContractParameterType.Integer },
                ParameterNames = new string[] { "param1", "param2" },
                Deployed = false
            };

            JObject @object = nep6Contract.ToJson();
            JString jString = (JString)@object["script"];
            jString.Value.Should().Be(nep6Contract.Script.ToHexString());

            JBoolean jBoolean = (JBoolean)@object["deployed"];
            jBoolean.Value.Should().BeFalse();

            JArray parameters = (JArray)@object["parameters"];
            parameters.Count.Should().Be(2);

            jString = (JString)(parameters[0]["name"]);
            jString.Value.Should().Be("param1");
            jString = (JString)(parameters[0]["type"]);
            jString.Value.Should().Be(ContractParameterType.Boolean.ToString());

            jString = (JString)(parameters[1]["name"]);
            jString.Value.Should().Be("param2");
            jString = (JString)(parameters[1]["type"]);
            jString.Value.Should().Be(ContractParameterType.Integer.ToString());
        }
    }
}
