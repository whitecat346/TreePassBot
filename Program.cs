using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Reflection;
using TreePassBot.Data;
using TreePassBot.Exceptions;
using TreePassBot.Handlers;
using TreePassBot.Handlers.Commands;
using TreePassBot.Handlers.MessageHandler;
using TreePassBot.Handlers.MessageHandler.Handlers;
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
                           .UseSerilog((context, services, configuration) =>
                                           configuration
                                              .ReadFrom.Configuration(context.Configuration)
                                              .ReadFrom.Services(services)
                                              .Enrich.FromLogContext())
                           .ConfigureServices(((hostContext, services) =>
                            {
                                services
                                   // Data Store
                                   .Configure<BotConfig>(hostContext.Configuration.GetSection("BotConfig"))
                                   .AddSingleton<JsonDataStore>()

                                   // Services
                                   .AddScoped<IUserService, UserService>()
                                   .AddScoped<IAuditService, AuditService>()
                                   .AddScoped<IMessageService, MessageService>()

                                   // Command Dispatcher
                                   .AddCommandModules(Assembly.GetExecutingAssembly())
                                   .AddSingleton<CommandDispatcher>()

                                   // Factory
                                   .AddSingleton<HandlerLinkNodeFactory>()

                                   // Event Handlers
                                   .AddMessageModules(Assembly.GetExecutingAssembly())
                                   .AddSingleton((provider =>
                                    {
                                        var logger = provider.GetRequiredService<ILogger<MessageHandlerDispatcher>>();
                                        var dispatcher = new MessageHandlerDispatcher(logger, provider);

                                        dispatcher.UseHandler<UriBlockerMessageHandler>()
                                                  .UseHandler<AdminCommandMessageHandler>()
                                                  .UseHandler<AuditCommandMessageHandler>();

                                        return dispatcher;
                                    }))
                                   .AddSingleton<GroupMessageEventHandler>()
                                   .AddSingleton<GroupRequestEventHandler>()
                                   .AddSingleton<GroupMemberEventHandler>()
                                   .AddSingleton<PrivateMessageEventHandler>()
                                   .AddSingleton<CatchUnhandledException>()

                                   // Utils
                                   .AddSingleton<PasscodeGeneratorUtil>()
                                   .AddSingleton<ArgumentsSpiliterUtil>()

                                   // Host
                                   .AddHostedService<QqBotService>();
                            }))
                           .Build();

            Log.Information("QQBot host started successfully");

            return host;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host rerminated unexpectedly");
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