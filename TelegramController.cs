using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Team23.TelegramSkeleton
{
  public class TelegramController : Controller
  {
    private readonly string myTelemetryTypeName;
    private readonly TelemetryClient myTelemetryClient;
    private readonly IEnumerable<Lazy<Func<Update, IUpdateHandler>, UpdateHandlerAttribute>> myUpdateHandlers;

    public TelegramController(ITelegramBotClient bot, TelemetryClient telemetryClient, IEnumerable<Lazy<Func<Update, IUpdateHandler>, UpdateHandlerAttribute>> updateHandlers)
      : this(telemetryClient, updateHandlers, bot.GetType().Name) { }

    protected TelegramController(TelemetryClient telemetryClient, IEnumerable<Lazy<Func<Update, IUpdateHandler>, UpdateHandlerAttribute>> updateHandlers, string telemetryTypeName = null)
    {
      myTelemetryTypeName = telemetryTypeName;
      myTelemetryClient = telemetryClient;
      myUpdateHandlers = updateHandlers;
    }

    [HttpPost("/update/{" + nameof(ITelegramBotClient.BotId) + ":long?}")]
    // ReSharper disable once InconsistentNaming
    public async Task<IActionResult> Update([CanBeNull, FromBody] Update update, int? BotId = null, CancellationToken cancellationToken = default)
    {
      var operation = myTelemetryClient.StartOperation(new DependencyTelemetry(myTelemetryTypeName ?? GetType().Namespace, Request.Host.ToString(), update?.Type.ToString(), update?.Id.ToString()));
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
                  { nameof(ITelegramBotClient.BotId), BotId?.ToString() },
                  { errorEntry.Key, errorEntry.Value.AttemptedValue }
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
      catch (Exception ex)
      {
        operation.Telemetry.Success = false;
        myTelemetryClient.TrackException(ex, new Dictionary<string, string>{ { nameof(ITelegramBotClient.BotId), BotId?.ToString() }});
        return Ok();
      }
      finally
      {
        operation.Dispose();
      }
    }
  }
}