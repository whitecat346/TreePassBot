using Makabaka.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreePassBot.Models;
using TreePassBot.Services.Interfaces;

namespace TreePassBot.Exceptions;

public class CatchUnhandledException
{
    private readonly ILogger<CatchUnhandledException> _logger;
    private readonly IMessageService _messageService;
    private readonly BotConfig _config;

    public CatchUnhandledException(ILogger<CatchUnhandledException> logger, IMessageService messageService,
                                   IOptions<BotConfig> config)
    {
        _logger = logger;
        _messageService = messageService;
        _config = config.Value;

        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
    }

    private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception ex)
        {
            return;
        }

        _logger.LogError("Catched unhandled exception: {Exception}", ex);

        try
        {
            _messageService.SendPrivateMessageAsync(_config.OwnerId,
            [
                new TextSegment("捕获到未处理的异常：\n"),
                new TextSegment(ex.ToString())
            ]);
        }
        catch (Exception exception)
        {
            _logger.LogError("Failed to send private message: {Exception}", exception);
        }
    }
}