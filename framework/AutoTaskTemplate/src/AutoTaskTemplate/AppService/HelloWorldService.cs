using System.Text.Json;
using AutoTaskTemplate.Agents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ray.Infrastructure.AutoTask;
using Volo.Abp.DependencyInjection;

namespace AutoTaskTemplate.AppService;

[AutoTask("Hello", "测试")]
public class HelloWorldService : ITransientDependency, IAutoTaskService
{
    private readonly IConfiguration _configuration;
    private readonly IIkuuuApi _hostlocApi;
    private readonly ILogger<HelloWorldService> _logger;
    private readonly TargetAccountInfo _targetAccount;

    public HelloWorldService(
        IConfiguration configuration,
        IIkuuuApi hostlocApi,
        TargetAccountManager<TargetAccountInfo> targetAccountManager,
        ILogger<HelloWorldService> logger
        )
    {
        _configuration = configuration;
        _hostlocApi = hostlocApi;
        _logger = logger;
        _targetAccount = targetAccountManager.CurrentTargetAccount;
    }


    public async Task DoAsync(CancellationToken cancellationToken)
    {
        var re = await LoginAsync(cancellationToken);
        if (re)
        {
            await CheckinAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 登录获取Cookie
    /// </summary>
    /// <returns></returns>
    public async Task<bool> LoginAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始任务：登录");
        _logger.LogInformation(_targetAccount.UserName);

        var req = new LoginRequest(_targetAccount.UserName, _targetAccount.Pwd);
        var re = await _hostlocApi.LoginAsync(req);

        if (!re.IsSuccessStatusCode)
        {
            _logger.LogError(JsonSerializer.Serialize(re));
            return false;
        }

        _logger.LogDebug(JsonSerializer.Serialize(re.Content));

        if (re.Content?.ret != 1)
        {
            _logger.LogError("登录失败：{msg}", re.Content?.msg ?? "");
            return false;
        }
        _logger.LogInformation("{msg}", re.Content?.msg ?? "登录成功！");
        _logger.LogInformation("Success{newLine}", Environment.NewLine);

        return true;
    }

    /// <summary>
    /// 签到
    /// </summary>
    /// <returns></returns>
    public async Task CheckinAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始签到");
        var re2 = await _hostlocApi.CheckinAsync();

        if (!re2.IsSuccessStatusCode)
        {
            _logger.LogError(JsonSerializer.Serialize(re2));
            return;
        }

        _logger.LogDebug(JsonSerializer.Serialize(re2.Content));

        if (re2.Content?.ret != 1)
        {
            _logger.LogError("签到失败：{msg}", re2.Content?.msg ?? "");
            return;
        }

        _logger.LogInformation("{msg}", re2.Content?.msg ?? "签到成功！");
        _logger.LogInformation("Success{newLine}", Environment.NewLine);
    }
}
