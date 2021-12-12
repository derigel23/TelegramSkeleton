using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using EncodedBotId = Team23.TelegramSkeleton.EncodedId<long, int>;

namespace Team23.TelegramSkeleton
{
  public class TelegramController : Controller
  {
    private readonly IWebHookSaltProvider myWebHookSaltProvider;
    private readonly string myTelemetryTypeName;
    private readonly TelemetryClient myTelemetryClient;
    private readonly IEnumerable<Lazy<Func<Update, IUpdateHandler>, UpdateHandlerAttribute>> myUpdateHandlers;

    public TelegramController(IWebHookSaltProvider webHookSaltProvider, ITelegramBotClient bot, TelemetryClient telemetryClient, IEnumerable<Lazy<Func<Update, IUpdateHandler>, UpdateHandlerAttribute>> updateHandlers)
      : this(telemetryClient, webHookSaltProvider, updateHandlers, bot.GetType().Name) { }

    protected TelegramController(TelemetryClient telemetryClient, IWebHookSaltProvider webHookSaltProvider, IEnumerable<Lazy<Func<Update, IUpdateHandler>, UpdateHandlerAttribute>> updateHandlers, string? telemetryTypeName)
    {
      myWebHookSaltProvider = webHookSaltProvider;
      myTelemetryTypeName = telemetryTypeName ?? GetType().Namespace ?? string.Empty;
      myTelemetryClient = telemetryClient;
      myUpdateHandlers = updateHandlers;
    }

    public static string EncodeBotId(long? botId, IWebHookSaltProvider? webHookSaltProvider) =>
      botId is { } id ? new EncodedBotId(id, webHookSaltProvider?.GetSalt(id) ?? default) : EncodedBotId.Empty;
  
    public static object EncodeBotRouteId(long? botId, IWebHookSaltProvider? webHookSaltProvider) => new
    {
      encodedBotId = botId is {} id ? new EncodedBotId(id, webHookSaltProvider?.GetSalt(id) ?? default) : EncodedBotId.Empty
    };

    private static bool DecodeBotId(string? encodedBotId, IWebHookSaltProvider? webHookSaltProvider, out long botId)
    {
      EncodedBotId decodedBotId = encodedBotId;
      if (decodedBotId != EncodedBotId.Empty)
      {
        (botId, var salt) = decodedBotId;
        return (webHookSaltProvider?.GetSalt(botId) ?? default) == salt;
      }

      botId = 0;
      return false;
    }

    public static bool DecodeBotId(ActionContext context, IWebHookSaltProvider? webHookSaltProvider, out long botId)
    {
      botId = 0;
      return context.RouteData.Values.TryGetValue("encodedBotId", out var encodedBotId) &&
             DecodeBotId(encodedBotId?.ToString(), webHookSaltProvider, out botId);
    }
    
    [HttpPost("/update/{encodedBotId}")]
    public async Task<IActionResult> Update([FromBody] Update? update, string encodedBotId, CancellationToken cancellationToken = default)
    {
      var operation = myTelemetryClient.StartOperation(new DependencyTelemetry(myTelemetryTypeName, Request.Host.ToString(), update?.Type.ToString(), update?.Id.ToString()));
      DecodeBotId(encodedBotId, myWebHookSaltProvider, out var botId);
      try
      {
        if (update == null)
        {
          foreach (var errorEntry in ModelState)
          {
            operation.Telemetry.Properties[$"ModelState.{errorEntry.Key}"] = errorEntry.Value.AttemptedValue;
            var errors = errorEntry.Value.Errors;
            for (var i = 0; i < errors.Count; i++)
            {
              operation.Telemetry.Properties[$"ModelState.{errorEntry.Key}.{i}"] = errors[i].ErrorMessage;
              if (errors[i].Exception is { } exception)
              {
                myTelemetryClient.TrackException(exception, new Dictionary<string, string>
                {
                  { nameof(encodedBotId), encodedBotId },
                  { nameof(ITelegramBotClient.BotId), botId.ToString() },
                  { errorEntry.Key, errorEntry.Value.AttemptedValue ?? string.Empty }
                });
              }
            }
          }
          throw new ArgumentNullException(nameof(update));
        }

        if (await HandlerExtensions<bool?>.Handle(myUpdateHandlers.Bind(update), update, (OperationTelemetry) operation.Telemetry, cancellationToken).ConfigureAwait(false) is { } result)
        {
          operation.Telemetry.Success = result;
          return Ok();
        }

        operation.Telemetry.Success = true;
        return Ok() /* TODO: not handled */;
      }
      catch (OperationCanceledException operationCanceledException) when (!cancellationToken.IsCancellationRequested)
      {
        operation.Telemetry.Success = false;
        myTelemetryClient.TrackException(new ExceptionTelemetry(operationCanceledException) { SeverityLevel = SeverityLevel.Warning });
        return Ok();
      }
      catch (ApiRequestTimeoutException)
      {
        operation.Telemetry.Success = false;
        return Ok();
      }
      catch (Exception ex)
      {
        operation.Telemetry.Success = false;
        myTelemetryClient.TrackException(ex, new Dictionary<string, string>
        {
          { nameof(encodedBotId), encodedBotId },
          { nameof(ITelegramBotClient.BotId), botId.ToString() }
        });
        return Ok();
      }
      finally
      {
        operation.Dispose();
      }
    }
  }
}