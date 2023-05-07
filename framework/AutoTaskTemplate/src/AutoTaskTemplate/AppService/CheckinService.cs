using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ray.DDD;
using Ray.Infrastructure.Aop;
using Ray.Infrastructure.AutoTask;

namespace AutoTaskTemplate.AppService;

[AutoTask("Checkin", "签到")]
public class CheckinService : IAppService, IAutoTaskService
{
    private readonly TargetAccountManager<MyAccountInfo> _targetAccountManager;
    private readonly ILogger<LoginService> _logger;

    public CheckinService(
        TargetAccountManager<MyAccountInfo> targetAccountManager,
        ILogger<LoginService> logger
    )
    {
        _targetAccountManager = targetAccountManager;
        _logger = logger;
    }

    public async Task DoAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("共{count}个账号", _targetAccountManager.Count);

        for (int i = 0; i < _targetAccountManager.Count; i++)
        {
            MyAccountInfo myAccount = _targetAccountManager.CurrentTargetAccount;

            try
            {
                await DoForAccountAsync(myAccount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "签到异常");
            }

            if (_targetAccountManager.HasNext())
            {
                var sec = 30;
                _logger.LogInformation("睡个{sec}秒", sec);
                await Task.Delay(30 * 1000, cancellationToken);
                _targetAccountManager.MoveToNext();
            }
        }
    }

    [DelimiterInterceptor("账号签到", DelimiterScale.L)]
    private async Task DoForAccountAsync(MyAccountInfo myAccount, CancellationToken cancellationToken)
    {
        _logger.LogInformation("账号：{account}", myAccount.NickName);

        _logger.LogInformation("打开浏览器");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
#if DEBUG
            Headless = false,
#else
            Headless = true,
#endif
        });
        var context = await browser.NewContextAsync();

        //加载状态
        _logger.LogInformation("加载历史状态");
        if (!string.IsNullOrWhiteSpace(myAccount.States))
        {
            var cookies = (JArray)JsonConvert.DeserializeObject<JObject>(myAccount.States)["cookies"];
            await context.AddCookiesAsync(cookies.ToObject<List<Cookie>>());
        }

        //新增tab页
        IPage page = await context.NewPageAsync();

        //访问并签到
        await CheckInAsync(myAccount, page, cancellationToken);
    }

    private async Task CheckInAsync(MyAccountInfo account, IPage page, CancellationToken cancellationToken)
    {
        var url = "https://account.bilibili.com/account/home";
        _logger.LogInformation("访问{url}", url);
        await page.GotoAsync(url);

        var exps = await page.Locator(".home-dialy-exp-item").AllAsync();

        foreach (var locator in exps.ToList())
        {
            var taskName = await locator.Locator(".re-exp-info").InnerTextAsync();
            var progress = await locator.Locator(".re-exp-getexp").InnerTextAsync();

            _logger.LogInformation("{taskName}：{progress}", taskName, progress);
        }
    }

}
