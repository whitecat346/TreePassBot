using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TreePassBot.Data;
using TreePassBot.Handlers;
using TreePassBot.Models;
using TreePassBot.Services;
using TreePassBot.Services.Interfaces;

namespace TreePassBot;
#nullable disable
internal class Program
{
    public static IHost AppHost { get; private set; }

    public static string[] InArgs { get; private set; }

    private static IHost Init()
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(new ConfigurationManager()
                .AddJsonFile("appsettings.json")
                .Build())
            .CreateLogger();

        try
        {
            Log.Information("Starting QQBot host...");

            var host = Host.CreateDefaultBuilder(InArgs)
                .UseSerilog((context, services, configration) => configration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext())
                .ConfigureServices(((hostContext, services) =>
                {
                    // 移除或注释掉这一行以避免IL2026/IL3050警告
                    // services.Configure<BotConfig>(hostContext.Configuration.GetSection("BotConfig"));

                    services.AddSingleton<JsonDataStore>();

                    services.AddScoped<IUserService, UserService>();
                    services.AddScoped<IAuditService, AudioService>();
                    services.AddScoped<IMessageService, MessageService>();

                    services.AddSingleton<GroupMessageEventHandler>();
                    services.AddSingleton<GroupRequestEventHandler>();

                    // 手动绑定配置，避免使用 Get<T>() 以消除 IL2026/IL3050 警告
                    var botConfigSection = hostContext.Configuration.GetSection("BotConfig");
                    var botConfig = new BotConfig();
                    botConfigSection.Bind(botConfig);
                    services.AddSingleton(botConfig);

                    services.AddHostedService<QqBotService>();
                }))
                .Build();

            Log.Information("QQBot host started successfully.");

            return host;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Hot rerminated unexpectedly.");
            throw;
        }
    }

    private static async Task Main(string[] args)
    {
        InArgs = args;
        AppHost = Init();

        await AppHost.RunAsync();
    }
}
#nullable restore