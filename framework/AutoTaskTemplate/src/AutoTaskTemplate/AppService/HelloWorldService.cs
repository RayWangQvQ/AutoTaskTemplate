using Microsoft.Extensions.Logging;
using Ray.DDD;
using Ray.Infrastructure.AutoTask;

namespace AutoTaskTemplate.AppService;

[AutoTask("Hello", "测试")]
public class HelloWorldService(ILogger<HelloWorldService> logger) : IAppService, IAutoTaskService
{
    public async Task DoAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        logger.LogInformation("Hello World!");
    }
}
