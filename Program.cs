using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TreePassBot.Data;
using TreePassBot.Handlers;
using TreePassBot.Handlers.AdminCommands;
using TreePassBot.Models;
using TreePassBot.Services;
using TreePassBot.Services.Interfaces;
using TreePassBot.Utils;

namespace TreePassBot;
#nullable disable
internal static class Program
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
                    services.Configure<BotConfig>(hostContext.Configuration.GetSection("BotConfig"));

                    services.AddSingleton<JsonDataStore>();

                    services.AddScoped<IUserService, UserService>();
                    services.AddScoped<IAuditService, AuditService>();
                    services.AddScoped<IMessageService, MessageService>();

                    services.AddSingleton<PasscodeGeneratorUtil>();

                    services.AddCommandModules(Assembly.GetExecutingAssembly());

                    services.AddSingleton<CommandDispatcher>();

                    services.AddSingleton<GroupMessageEventHandler>();
                    services.AddSingleton<GroupRequestEventHandler>();
                    services.AddSingleton<GroupMemberEventHandler>();
                    services.AddSingleton<PrivateMessageEventHandler>();

                    services.AddHostedService<QqBotService>();
                }))
                .Build();

            Log.Information("QQBot host started successfully.");

            return host;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host rerminated unexpectedly.");
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