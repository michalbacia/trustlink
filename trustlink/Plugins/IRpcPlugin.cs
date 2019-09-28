using Microsoft.AspNetCore.Http;
using Trustlink.IO.Json;

namespace Trustlink.Plugins
{
    public interface IRpcPlugin
    {
        void PreProcess(HttpContext context, string method, JArray _params);
        JObject OnProcess(HttpContext context, string method, JArray _params);
        void PostProcess(HttpContext context, string method, JArray _params, JObject result);
    }
}
