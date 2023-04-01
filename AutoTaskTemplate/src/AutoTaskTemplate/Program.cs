using AutoTaskTemplate.Agents;
using AutoTaskTemplate.AppService;
using AutoTaskTemplate.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ray.Infrastructure.AutoTask;
using Ray.Serilog.Sinks.PushPlusBatched;
using Ray.Serilog.Sinks.ServerChanBatched;
using Ray.Serilog.Sinks.TelegramBatched;
using Ray.Serilog.Sinks.WorkWeiXinBatched;
using Refit;
using Serilog;
using Serilog.Events;

namespace AutoTaskTemplate;

public class Program
{
    private const string EnvPrefix = "AutoTaskTemplate_";

    public static async Task<int> Main(string[] args)
    {
        Log.Logger = CreateLogger(args);
        try
        {
            Log.Logger.Information("Starting console host.");

            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
                {
                    IList<IConfigurationSource> list = configurationBuilder.Sources;
                    list.ReplaceWhile(
                        configurationSource => configurationSource is EnvironmentVariablesConfigurationSource,
                        new EnvironmentVariablesConfigurationSource()
                        {
                            Prefix = EnvPrefix
                        }
                    );
                })
                .ConfigureServices(RegisterServices)
                .UseSerilog()
                .RunConsoleAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            Log.Logger.Fatal("·开始推送·{task}·{user}", "任务异常", "");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static ILogger CreateLogger(string[] args)
    {
        var hb = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
            {
                IList<IConfigurationSource> list = configurationBuilder.Sources;
                list.ReplaceWhile(
                    configurationSource => configurationSource is EnvironmentVariablesConfigurationSource,
                    new EnvironmentVariablesConfigurationSource()
                    {
                        Prefix = EnvPrefix
                    }
                );
            });
        var tempHost = hb.Build();
        var config = tempHost.Services.GetRequiredService<IConfiguration>();

        return new LoggerConfiguration()
            .MinimumLevel.Information()
#if DEBUG
            .MinimumLevel.Debug()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Async(c =>
            {
                c.File($"Logs/{DateTime.Now.ToString("yyyy-MM-dd")}/{DateTime.Now.ToString("HH-mm-ss")}.txt",
                    restrictedToMinimumLevel: LogEventLevel.Debug);
            })
            .WriteTo.Console()
            .WriteTo.PushPlusBatched(
                config["Notify:PushPlus:Token"],
                config["Notify:PushPlus:Channel"],
                config["Notify:PushPlus:Topic"],
                config["Notify:PushPlus:Webhook"],
                restrictedToMinimumLevel: LogEventLevel.Information
            )
            .WriteTo.TelegramBatched(
                config["Notify:Telegram:BotToken"],
                config["Notify:Telegram:ChatId"],
                config["Notify:Telegram:Proxy"],
                restrictedToMinimumLevel: LogEventLevel.Information
            )
            .WriteTo.ServerChanBatched(
                "",
                turboScKey: config["Notify:ServerChan:TurboScKey"],
                restrictedToMinimumLevel: LogEventLevel.Information
            )
            .WriteTo.WorkWeiXinBatched(
                config["Notify:WorkWeiXin:WebHookUrl"],
                restrictedToMinimumLevel: LogEventLevel.Information
            )
            .CreateLogger();
    }

    private static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
    {
        var config = (IConfigurationRoot)hostBuilderContext.Configuration;

        services.AddHostedService<MyHostedService>();

        services.AddSingleton(typeof(TargetAccountManager<>));

        #region config
        services.Configure<List<AccountOptions>>(config.GetSection("Accounts"));
        services.Configure<HttpClientCustomOptions>(config.GetSection("HttpCustomConfig"));
        #endregion

        #region Api
        services.AddSingleton<TargetAccountManager<TargetAccountInfo>>();

        services.AddTransient<DelayHttpMessageHandler>();
        services.AddTransient<LogHttpMessageHandler>();
        services.AddTransient<ProxyHttpClientHandler>();
        services.AddTransient<CookieHttpClientHandler>();
        services
            .AddRefitClient<IIkuuuApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://ikuuu.eu");

                var ua = config["UserAgent"];
                if (!string.IsNullOrWhiteSpace(ua))
                    c.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
            })
            .AddHttpMessageHandler<DelayHttpMessageHandler>()
            .AddHttpMessageHandler<LogHttpMessageHandler>()
            .ConfigurePrimaryHttpMessageHandler<ProxyHttpClientHandler>()
            .ConfigurePrimaryHttpMessageHandler<CookieHttpClientHandler>()
            ;
        #endregion

        services.AddTransient<HelloWorldService>();
    }
}