using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Maui.App.Services;

public class ApiKeyHandler : DelegatingHandler
{
    private readonly IConfiguration _config;
    public ApiKeyHandler(IConfiguration config) => _config = config;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = _config["Api:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            if (!request.Headers.Contains("X-Api-Key"))
                request.Headers.Add("X-Api-Key", apiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
