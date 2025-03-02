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

public class LoginDomainService(
    ILogger<LoginDomainService> logger,
    IOptions<SystemConfig> systemOptions,
    IQingLongApi qingLongApi,
    IHostEnvironment hostEnvironment)
    : IDomainService
{
    private readonly SystemConfig _systemOptions = systemOptions.Value;

    public async Task LoginAsync(MyAccountInfo account, IPage page, CancellationToken cancellationToken)
    {
        //await PwdLoginAsync(myAccount, page, cancellationToken);
        await QrCodeLoginAsync(account,page, cancellationToken);
    }

    /// <summary>
    /// 扫描二维码登录
    /// </summary>
    /// <param name="account"></param>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task QrCodeLoginAsync(MyAccountInfo account, IPage page, CancellationToken cancellationToken)
    {
        var loginLocator = page.GetByText("登录", new() { Exact = true });

        if (await loginLocator.CountAsync() < 0)
        {
            logger.LogInformation("已登录，不需要重复登录");
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
            logger.LogInformation("[{time}]等待扫码...", currentTry);

            await Task.Delay(20 * 1000, cancellationToken);

            if (await page.GetByText("登录", new() { Exact = true }).CountAsync() == 0)
            {
                loginSuccess = true;
                logger.LogInformation("扫码登录成功！");
                await page.ScreenshotAsync(new()
                {
                    Path = "screenshots/already_login.png",
                });
                break;
            };
        }

        logger.LogInformation("持久化账号状态");
        await SaveStatesAsync(account, page, cancellationToken);
        logger.LogInformation("持久化成功");
    }

    /// <summary>
    /// 账号密码登录
    /// </summary>
    /// <param name="account"></param>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task PwdLoginAsync(MyAccountInfo account, IPage page, CancellationToken cancellationToken)
    {
        logger.LogInformation("填入账号：{userName}", account.UserName);
        var emailLocator = page.GetByLabel("账号");
        await emailLocator.ClickAsync();
        await emailLocator.FillAsync(account.UserName);

        logger.LogInformation("填入密码：{pwd}", new string('*', account.Pwd.Length));
        var pwdLocator = page.GetByLabel("密码");
        await pwdLocator.ClickAsync();
        await pwdLocator.FillAsync(account.Pwd);

        await page.GetByText("记住我").ClickAsync();

        logger.LogInformation("点击登录");
        var loginLocator = page.GetByRole(AriaRole.Button, new() { Name = "登录", Exact = true });
        await loginLocator.ClickAsync();

        //todo:判断是否登录成功

        logger.LogInformation("持久化账号状态");
        await SaveStatesAsync(account, page, cancellationToken);
        logger.LogInformation("持久化成功");
    }

    /// <summary>
    /// 持久化状态
    /// </summary>
    /// <param name="account"></param>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task SaveStatesAsync(MyAccountInfo account, IPage page, CancellationToken cancellationToken)
    {
        account.States = await page.Context.StorageStateAsync();

        if (_systemOptions.Platform.ToLower() == "qinglong")
        {
            logger.LogInformation("尝试存储到青龙环境变量");
            await SaveStatesToQingLongAsync(account, cancellationToken);
        }
        else
        {
            logger.LogInformation("尝试存储到本地配置文件");
            SaveStatesToJsonFile(account);
        }
    }

    public async Task SaveStatesToQingLongAsync(MyAccountInfo myAccount, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(myAccount.States)) return;

        // 使用用户名+密码登录，所以根据用户名来保存states
        if (!string.IsNullOrWhiteSpace(myAccount.UserName))
        {
            await QingLongHelper.SaveStatesByUserNameAsync(qingLongApi,
                myAccount.UserName,
                $"{MyConst.EnvPrefix}Accounts",
                myAccount.States,
                logger: logger, cancellationToken: cancellationToken);
            return;
        }

        //使用扫码等方式登录，没有用户名，根据states本身来保存
        await QingLongHelper.SaveStatesByStatesAsync(qingLongApi,
            $"{MyConst.EnvPrefix}Accounts", myAccount.States, myAccount.GetKeyValueFromStates(), "States",
            logger, cancellationToken);
    }

    public void SaveStatesToJsonFile(MyAccountInfo myAccount)
    {
        var pl = hostEnvironment.ContentRootPath.Split("bin").ToList();
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
                onRowPrintProcess: s => logger.LogInformation(s));
        }
        else
        {
            Ray.Infrastructure.BarCode.BarCodeHelper.PrintQrCode(img,
                onRowPrintProcess: s => logger.LogInformation(s));
        }
        img.Dispose();
        logger.LogInformation("若显示异常，请访问在线版扫描：{qrcode}", GetOnlinePic(text));
    }

    private string GetOnlinePic(string str)
    {
        var encode = System.Web.HttpUtility.UrlEncode(str); ;
        return $"https://tool.lu/qrcode/basic.html?text={encode}";
    }
}

