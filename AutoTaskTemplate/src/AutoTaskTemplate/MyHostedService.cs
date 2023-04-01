using AutoTaskTemplate.AppService;
using AutoTaskTemplate.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.Infrastructure.AutoTask;

namespace AutoTaskTemplate;

public class MyHostedService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<MyHostedService> _logger;
    private readonly TargetAccountManager<TargetAccountInfo> _accountManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<AccountOptions> _accountOptions;

    public MyHostedService(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<MyHostedService> logger,
        IOptions<List<AccountOptions>> accountOptions,
        TargetAccountManager<TargetAccountInfo> targetAccountManager,
        IServiceProvider serviceProvider
    )
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _accountManager = targetAccountManager;
        _serviceProvider = serviceProvider;
        _accountOptions = accountOptions.Value;
        _accountManager.Init(_accountOptions.Select(x => new TargetAccountInfo(x.Email, x.Pwd)).ToList());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_accountManager.Count <= 0)
        {
            _logger.LogWarning("一个账号没配你运行个卵");
            return;
        }

        for (int i = 0; i < _accountManager.Count; i++)
        {
            _accountManager.Index = i;
            var currentAccount = _accountManager.CurrentTargetAccount;
            _logger.LogInformation("========账号{count}========", i + 1);
            _logger.LogInformation("用户名：{userName}", currentAccount.UserName);

            using var scope = _serviceProvider.CreateScope();
            var helloWorldService = scope.ServiceProvider.GetRequiredService<HelloWorldService>();
            await helloWorldService.SayHelloAsync(cancellationToken);

            _logger.LogInformation("========账号{count}结束========{newLine}", i + 1, Environment.NewLine);

            _logger.LogInformation("·开始推送·{task}·{user}", $"{_configuration["Run"]}任务", currentAccount.UserName);
        }
        _hostApplicationLifetime.StopApplication();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}
