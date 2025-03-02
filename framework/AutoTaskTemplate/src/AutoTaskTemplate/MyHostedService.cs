using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ray.Infrastructure.AutoTask;

namespace AutoTaskTemplate;

public class MyHostedService(
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<MyHostedService> logger,
    IServiceProvider serviceProvider,
    AutoTaskTypeFactory autoTaskTypeFactory)
    : IHostedService
{
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await DoTaskAsync(cancellationToken);

        logger.LogInformation("·开始推送·{task}", $"{configuration["Run"]}任务");
        hostApplicationLifetime.StopApplication();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {

    }

    private async Task DoTaskAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var run = configuration["Run"];

        var autoTaskInfo = autoTaskTypeFactory.GetByCode(run);

        while (autoTaskInfo == null)
        {
            logger.LogInformation("未指定目标任务，请选择要运行的任务：");
            autoTaskTypeFactory.Show(logger);
            logger.LogInformation("请输入：");

            var index = Console.ReadLine();
            var suc = int.TryParse(index, out int num);
            if (!suc)
            {
                logger.LogWarning("输入异常，请输入序号");
                continue;
            }

            autoTaskInfo = autoTaskTypeFactory.GetByIndex(num);
        }

        logger.LogInformation("目标任务：{run}", autoTaskInfo.ToString());

        var service = (IAutoTaskService)scope.ServiceProvider.GetRequiredService(autoTaskInfo.ImplementType);
        await service.DoAsync(cancellationToken);
    }
}

