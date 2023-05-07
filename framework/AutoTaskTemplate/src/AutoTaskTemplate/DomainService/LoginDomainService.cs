using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Ray.DDD;
using Ray.Infrastructure.QingLong;
using AutoTaskTemplate.Configs;

namespace AutoTaskTemplate.DomainService;

public class LoginDomainService : IDomainService
{
    private readonly SystemConfig _systemOptions;
    private readonly ILogger<LoginDomainService> _logger;
    private readonly IQingLongApi _qingLongApi;
    private readonly IHostEnvironment _hostEnvironment;

    public LoginDomainService(
        ILogger<LoginDomainService> logger,
        IOptions<SystemConfig> systemOptions,
        IQingLongApi qingLongApi,
        IHostEnvironment hostEnvironment
        )
    {
        _logger = logger;
        _qingLongApi = qingLongApi;
        _hostEnvironment = hostEnvironment;
        _systemOptions = systemOptions.Value;
    }

    public async Task LoginAsync(MyAccountInfo myAccount, IPage page, CancellationToken cancellationToken)
    {
        //await PwdLoginAsync(myAccount, page, cancellationToken);
        await QrCodeLoginAsync(myAccount,page, cancellationToken);
    }

    /// <summary>
    /// 扫描二维码登录
    /// </summary>
    /// <param name="myAccount"></param>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task QrCodeLoginAsync(MyAccountInfo myAccount, IPage page, CancellationToken cancellationToken)
    {
        var loginLocator = page.GetByText("登录", new() { Exact = true });

        if (await loginLocator.CountAsync() < 0)
        {
            _logger.LogInformation("已登录，不需要重复登录");
            return;
        }

        await loginLocator.ClickAsync();

        var qrCodeLocator = page.GetByRole(AriaRole.Img, new() { Name = "Scan me!" });
        var picBase64 = await qrCodeLocator.First.GetAttributeAsync("src");
        ShowQrCode(picBase64);

        //扫描
        var maxTry = 5;
        var currentTry = 0;
        var loginSuccess = false;
        while (currentTry < maxTry && !loginSuccess)
        {
            currentTry++;
            _logger.LogInformation("[{time}]等待扫码...", currentTry);

            await Task.Delay(20 * 1000, cancellationToken);

            if (await page.GetByText("登录", new() { Exact = true }).CountAsync() == 0)
            {
                loginSuccess = true;
                _logger.LogInformation("扫码登录成功！");
                await page.ScreenshotAsync(new()
                {
                    Path = "screenshots/already_login.png",
                });
                break;
            };
        }

        _logger.LogInformation("持久化账号状态");
        await SaveStatesAsync(myAccount, page, cancellationToken);
        _logger.LogInformation("持久化成功");
    }

    /// <summary>
    /// 账号密码登录
    /// </summary>
    /// <param name="myAccount"></param>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task PwdLoginAsync(MyAccountInfo myAccount, IPage page, CancellationToken cancellationToken)
    {
        _logger.LogInformation("填入账号：{userName}", myAccount.UserName);
        var emailLocator = page.GetByLabel("账号");
        await emailLocator.ClickAsync();
        await emailLocator.FillAsync(myAccount.UserName);

        _logger.LogInformation("填入密码：{pwd}", new string('*', myAccount.Pwd.Length));
        var pwdLocator = page.GetByLabel("密码");
        await pwdLocator.ClickAsync();
        await pwdLocator.FillAsync(myAccount.Pwd);

        await page.GetByText("记住我").ClickAsync();

        _logger.LogInformation("点击登录");
        var loginLocator = page.GetByRole(AriaRole.Button, new() { Name = "登录", Exact = true });
        await loginLocator.ClickAsync();

        //todo:判断是否登录成功

        _logger.LogInformation("持久化账号状态");
        await SaveStatesAsync(myAccount, page, cancellationToken);
        _logger.LogInformation("持久化成功");
    }

    /// <summary>
    /// 持久化状态
    /// </summary>
    /// <param name="myAccount"></param>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task SaveStatesAsync(MyAccountInfo myAccount, IPage page, CancellationToken cancellationToken)
    {
        myAccount.States = await page.Context.StorageStateAsync();

        if (_systemOptions.Platform.ToLower() == "qinglong")
        {
            _logger.LogInformation("尝试存储到青龙环境变量");
            await QingLongHelper.SaveCookieListItemToQinLongAsync(_qingLongApi,
                $"{MyConst.EnvPrefix}Accounts", myAccount.States, myAccount.GetDedeUserID(), "States",
                _logger, cancellationToken);
        }
        else
        {
            _logger.LogInformation("尝试存储到本地配置文件");
            SaveStatesToJsonFile(myAccount);
        }
    }

    public void SaveStatesToJsonFile(MyAccountInfo myAccount)
    {
        var pl = _hostEnvironment.ContentRootPath.Split("bin").ToList();
        pl.RemoveAt(pl.Count - 1);
        var path = Path.Combine(string.Join("bin", pl), "accounts.json");

        if (!File.Exists(path))
        {
            File.WriteAllText(path, "{\"Accounts\":[]}");
        }

        var jsonStr = File.ReadAllText(path);

        dynamic jsonObj = string.IsNullOrWhiteSpace(jsonStr) ? new() : JsonConvert.DeserializeObject(jsonStr);
        var accounts = ((JArray)jsonObj["Accounts"]).ToObject<List<MyAccountInfo>>();

        var find = accounts?.FirstOrDefault(x => x.Equals(myAccount));

        if (find!=null)
        {
            var index = accounts.IndexOf(find);
            accounts[index].States = myAccount.States;
        }
        else
        {
            var n = new MyAccountInfo
            {
                UserName = myAccount.UserName ?? "",
                Pwd = myAccount.Pwd ?? "",
                States = myAccount.States ?? ""
            };
            accounts.Add(n);
        }

        jsonObj["Accounts"] = JArray.FromObject(accounts);

        string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
        File.WriteAllText(path, output);
    }

    private void ShowQrCode(string base64Str)
    {
        var text = Ray.Infrastructure.BarCode.BarCodeHelper
            .DecodeByBase64Str(base64Str)
            .ToString();
        var img = Ray.Infrastructure.BarCode.BarCodeHelper.EncodeByImageSharp(text, optionsAction: op =>
        {
            op.Width = 20;
            op.Height = 20;
        });//重新生成，压缩下

        //打印二维码
        if (_systemOptions.Platform.ToLower() == "qinglong")
        {
            Ray.Infrastructure.BarCode.BarCodeHelper.PrintSmallQrCode(img,
                onRowPrintProcess: s => _logger.LogInformation(s));
        }
        else
        {
            Ray.Infrastructure.BarCode.BarCodeHelper.PrintQrCode(img,
                onRowPrintProcess: s => _logger.LogInformation(s));
        }
        img.Dispose();
        _logger.LogInformation("若显示异常，请访问在线版扫描：{qrcode}", GetOnlinePic(text));
    }

    private string GetOnlinePic(string str)
    {
        var encode = System.Web.HttpUtility.UrlEncode(str); ;
        return $"https://tool.lu/qrcode/basic.html?text={encode}";
    }
}

