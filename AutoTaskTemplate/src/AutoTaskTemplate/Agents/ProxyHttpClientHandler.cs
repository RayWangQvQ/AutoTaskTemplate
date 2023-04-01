using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoTaskTemplate.Agents
{
    public class ProxyHttpClientHandler : HttpClientHandler
    {
        public ProxyHttpClientHandler(IOptions<HttpClientCustomOptions> options, ILogger<ProxyHttpClientHandler> logger)
        {
            var configs = options.Value;
            var proxyAddress = configs.WebProxy;

            if (!proxyAddress.IsNullOrWhiteSpace())
            {
                logger.LogInformation("使用代理");

                WebProxy webProxy;

                //user:password@host:port http proxy only
                if (proxyAddress.Contains("@"))
                {
                    var credentialAndAddressList = proxyAddress.Split("@");

                    string userPass = credentialAndAddressList[0];
                    string proxyUser = userPass.Split(":")[0];
                    string proxyPass = userPass.Split(":")[1];
                    var credentials = new NetworkCredential(proxyUser, proxyPass);

                    string address = credentialAndAddressList[1];

                    webProxy = new WebProxy(address, true, null, credentials);
                }
                else
                {
                    webProxy = new WebProxy(proxyAddress, true);
                }
                Proxy = webProxy;
            }
        }
    }
}
