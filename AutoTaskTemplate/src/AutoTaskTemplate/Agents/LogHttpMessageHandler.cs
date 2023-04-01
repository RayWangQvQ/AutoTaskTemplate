using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace AutoTaskTemplate.Agents
{
    public class LogHttpMessageHandler : DelegatingHandler
    {
        private readonly ILogger<LogHttpMessageHandler> _logger;

        public LogHttpMessageHandler(ILogger<LogHttpMessageHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var req = request;
            var msg = $"[Request]";

            _logger.LogDebug($"{msg} {req.Method} {req.RequestUri.PathAndQuery} {req.RequestUri.Scheme}/{req.Version}");
            _logger.LogDebug($"{msg} Host: {req.RequestUri.Scheme}://{req.RequestUri.Host}");

            foreach (var header in req.Headers)
                _logger.LogDebug($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

            if (req.Content != null)
            {
                foreach (var header in req.Content.Headers)
                    _logger.LogDebug($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

                if (req.Content is StringContent || this.IsTextBasedContentType(req.Headers) || this.IsTextBasedContentType(req.Content.Headers))
                {
                    var result = await req.Content.ReadAsStringAsync();

                    _logger.LogDebug($"{msg} Content:");
                    _logger.LogDebug($"{msg} {string.Join("", result.Cast<char>().Take(255))}...");

                }
            }

            var start = DateTime.Now;

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            var end = DateTime.Now;

            _logger.LogDebug($"{msg} Duration: {end - start}");

            msg = $"[Response]";

            var resp = response;

            _logger.LogDebug($"{msg} {req.RequestUri.Scheme.ToUpper()}/{resp.Version} {(int)resp.StatusCode} {resp.ReasonPhrase}");

            foreach (var header in resp.Headers)
                _logger.LogDebug($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

            if (resp.Content != null)
            {
                foreach (var header in resp.Content.Headers)
                    _logger.LogDebug($"{msg} {header.Key}: {string.Join(", ", header.Value)}");

                if (resp.Content is StringContent || this.IsTextBasedContentType(resp.Headers) || this.IsTextBasedContentType(resp.Content.Headers))
                {
                    start = DateTime.Now;
                    var result = await resp.Content.ReadAsStringAsync();
                    end = DateTime.Now;

                    _logger.LogDebug($"{msg} Content:");
                    _logger.LogDebug($"{msg} {result}");
                    _logger.LogDebug($"{msg} Duration: {end - start}");
                }
            }

            return response;
        }

        readonly string[] types = new[] { "html", "text", "xml", "json", "txt", "x-www-form-urlencoded" };

        bool IsTextBasedContentType(HttpHeaders headers)
        {
            IEnumerable<string> values;
            if (!headers.TryGetValues("Content-Type", out values))
                return false;
            var header = string.Join(" ", values).ToLowerInvariant();

            return types.Any(t => header.Contains(t));
        }
    }
}
