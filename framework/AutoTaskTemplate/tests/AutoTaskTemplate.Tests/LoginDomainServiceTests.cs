using AutoTaskTemplate.Configs;
using AutoTaskTemplate.DomainService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Ray.Infrastructure.QingLong;

namespace AutoTaskTemplate.Tests;

public class LoginDomainServiceTests
{
    private const string StatesSample = @"
{
    ""cookies"":[
        {""name"":""innersign"",""value"":""0"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":-1,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""buvid3"",""value"":""E6F37199-8FA3-123-C3F9-CAD06B5456D130747infoc"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":1.7149818E+09,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""i-wanna-go-back"",""value"":""-1"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":1.7149818E+09,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""b_ut"",""value"":""7"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":1.7149818E+09,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""b_lsid"",""value"":""26397CE8_187123F5073"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":-1,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""_uuid"",""value"":""AE8D59A5-7456-E926-12AA-123B5D36D107531458infoc"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":1.7149818E+09,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""FEED_LIVE_VERSION"",""value"":""V8"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":1.7149818E+09,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""header_theme_version"",""value"":""undefined"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":1.7149818E+09,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""buvid_fp"",""value"":""ddfdb72ad101231a892a42e45663c961"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":1.7180058E+09,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""home_feed_column"",""value"":""4"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":1.7149818E+09,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""browser_resolution"",""value"":""1230-720"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":1.7149818E+09,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""b_nut"",""value"":""1683412333"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":1.7180058E+09,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""buvid4"",""value"":""6CC2D123-14B0-0C1F-9456-C34DF123C09133519-023045615-fgDvZyqdDlejd0uF1etKyeWLk90hLcaL3Ip96yyJCCnR/6mz9Uj8Ug%3D%3D"",""domain"":"".bilibili.com"",""path"":""/"",""expires"":1.7180058E+09,""httpOnly"":false,""secure"":false,""sameSite"":""Lax""},
        {""name"":""SESSDATA"",""value"":""3f8f0e85%2C1698997123%2C3d456%2A51"",""domain"":"".bigfunapp.cn"",""path"":""/"",""expires"":1.6989978E+09,""httpOnly"":true,""secure"":true,""sameSite"":""None""},
        {""name"":""bili_jct"",""value"":""106eb01235c6774c41e9fef45dd4565d"",""domain"":"".bigfunapp.cn"",""path"":""/"",""expires"":1.6989978E+09,""httpOnly"":false,""secure"":true,""sameSite"":""None""},
        {""name"":""DedeUserID"",""value"":""123456"",""domain"":"".bigfunapp.cn"",""path"":""/"",""expires"":1.6989978E+09,""httpOnly"":false,""secure"":true,""sameSite"":""None""},
        {""name"":""DedeUserID__ckMd5"",""value"":""90123d1d4ec4565a"",""domain"":"".bigfunapp.cn"",""path"":""/"",""expires"":1.6989978E+09,""httpOnly"":false,""secure"":true,""sameSite"":""None""},
        {""name"":""sid"",""value"":""e123rrop"",""domain"":"".bigfunapp.cn"",""path"":""/"",""expires"":1.6989978E+09,""httpOnly"":false,""secure"":true,""sameSite"":""None""}],
    ""origins"":[
        {
            ""origin"":""https://www.bilibili.com"",
            ""localStorage"":[
                {""name"":""abc-v1"",""value"":""""},
                {""name"":""wbi_sub_url"",""value"":""https://i0.hdslb.com/bfs/wbi/132ed1b7a5cc4388a8b52960d8e2922d.png""},
                {""name"":""time_tracker"",""value"":""{\u00221585227649\u0022:\u002220230507\u0022}""},
                {""name"":""ac_time_value"",""value"":""a7d79562b5a7217fd5650abc52fbd051""},
                {""name"":""show-feed-version-guide"",""value"":""\u0022VIEWED\u0022""},
                {""name"":""im_floatmsg_1585227649"",""value"":""{\u0022res\u0022:{\u0022data\u0022:[],\u0022code\u0022:0},\u0022ts\u0022:1683445812496,\u0022uid\u0022:1585227649}""},
                {""name"":""bili-login-state"",""value"":""2""},
                {""name"":""wbi_img_url"",""value"":""https://i0.hdslb.com/bfs/wbi/6d7d165587614802885330696e7e6502.png""},
                {""name"":""SHOWED_USER_FEEDBACK"",""value"":""1""}
            ]
        },
        {
            ""origin"":""https://s1.hdslb.com"",
            ""localStorage"":[
                {""name"":""search_history:search_history"",""value"":""[]""},
                {""name"":""meta-reporter-space:cross_buvid4"",""value"":""null""},
                {""name"":""log-reporter-space:cross_buvid4"",""value"":""6CC2DCF1-14B0-0C1F-919A-C34DF047C09133519-023050715-fgDvZyqdDlejd0uF1etKyeWLk90hLcaL3Ip96yyJCCnR/6mz9Uj8Ug==""},
                {""name"":""search_history:migrated"",""value"":""1""}
            ]
        }
    ]
}";

    private LoginDomainService _target;

    private Mock<IHostEnvironment> _hostEnvironmentMock;
    private Mock<ILogger<LoginDomainService>> _loggerMock;
    private Mock<IOptions<SystemConfig>> _systemOptionsMock;
    private Mock<IQingLongApi> _qinglongApiMock;

    public LoginDomainServiceTests()
    {
        _hostEnvironmentMock = new();
        _loggerMock = new();
        _systemOptionsMock = new();
        _qinglongApiMock = new();

        _hostEnvironmentMock.Setup(x => x.ContentRootPath)
            .Returns(AppContext.BaseDirectory);

        _target = new LoginDomainService(_loggerMock.Object, _systemOptionsMock.Object, _qinglongApiMock.Object, _hostEnvironmentMock.Object);
    }

    [Fact]
    public void SaveStatesToJson_Test()
    {
        var account = new MyAccountInfo()
        {
            States = StatesSample
        };
        _target.SaveStatesToJsonFile(account);
    }
}