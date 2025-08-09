using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TreePassBot.Data;
using TreePassBot.Handlers;
using TreePassBot.Models;
using TreePassBot.Services;
using TreePassBot.Services.Interfaces;

namespace TreePassBot
{
    internal class Program
    {
        public static IHost AppHost { get; private set; }

        private static IHost Init(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationManager()
                    .AddJsonFile("appsettings.json")
                    .Build())
                .CreateLogger();

            try
            {
                Log.Information("Starting QQBot host...");

                var host = Host.CreateDefaultBuilder(args)
                    .UseSerilog((context, services, configration) => configration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext())
                    .ConfigureServices(((hostContext, services) =>
                    {
                        services.Configure<BotConfig>(hostContext.Configuration.GetSection("BotConfig"));
                        services.AddSingleton<JsonDataStore>();

                        services.AddScoped<IUserService, UserService>();
                        services.AddScoped<IAuditService, AudioService>();
                        services.AddScoped<IMessageService, MessageService>();

                        services.AddSingleton<PrivateMessageEventHandler>();
                        services.AddSingleton<GroupRequestEventHandler>();

                        // 替换原有的配置绑定方式，手动绑定配置以避免IL2026和IL3050警告
                        var botConfigSection = hostContext.Configuration.GetSection("BotConfig");
                        var botConfig = botConfigSection.Get<BotConfig>()!;
                        services.AddSingleton(botConfig);

                        services.AddHostedService<QQBotService>();
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
            AppHost = Init(args);

            await AppHost.RunAsync();
        }
    }
}
