using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  [UpdateHandler(UpdateType = UpdateType.InlineQuery)]
  public class InlineQueryUpdateHandler : IUpdateHandler
  {
    private readonly IEnumerable<Lazy<Func<IInlineQueryHandler>, InlineQueryHandlerAttribute>> myInlineQueryHandlers;

    public InlineQueryUpdateHandler(IEnumerable<Lazy<Func<IInlineQueryHandler>, InlineQueryHandlerAttribute>> inlineQueryHandlers)
    {
      myInlineQueryHandlers = inlineQueryHandlers;
    }
    
    public async Task<bool?> Handle(Update update, OperationTelemetry? telemetry, CancellationToken cancellationToken = default)
    {
      if (update.InlineQuery is not {} inlineQuery) return default;
      
      if (telemetry != null)
      {
        telemetry.Properties["uid"] = inlineQuery.From?.Id.ToString();
        telemetry.Properties["username"] = inlineQuery.From?.Username;
        telemetry.Properties["query"] = inlineQuery.Query;
      }

      return await HandlerExtensions<bool?>.Handle(myInlineQueryHandlers, inlineQuery, new object(), cancellationToken).ConfigureAwait(false);
    }
  }
}