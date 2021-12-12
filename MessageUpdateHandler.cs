using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  [UpdateHandler(UpdateTypes = new[] { UpdateType.Message, UpdateType.EditedMessage, UpdateType.ChannelPost, UpdateType.EditedChannelPost })]
  public abstract class MessageUpdateHandler<TMessageHandler, TMessageContext, TMessageResult, TMessageMetadata> : IUpdateHandler
    where TMessageHandler : IMessageHandler<TMessageContext?, TMessageResult>
    where TMessageMetadata : Attribute, IHandlerAttribute<Message, (UpdateType, TMessageContext?)>
  {
    private readonly IEnumerable<Lazy<Func<Message, TMessageHandler>, TMessageMetadata>> myMessageHandlers;

    protected MessageUpdateHandler(IEnumerable<Lazy<Func<Message, TMessageHandler>, TMessageMetadata>> messageHandlers)
    {
      myMessageHandlers = messageHandlers;
    }
    
    public async Task<bool?> Handle(Update update, OperationTelemetry? telemetry, CancellationToken cancellationToken = default)
    {
      var updateType = update.Type;
      var message = updateType switch
      {
        UpdateType.Message => update.Message,
        UpdateType.EditedMessage => update.EditedMessage,
        UpdateType.ChannelPost => update.ChannelPost,
        UpdateType.EditedChannelPost => update.EditedChannelPost,
        _ => throw new ArgumentOutOfRangeException($"Not supported update type: {update.Type} ")
      };

      if (message == null) return default;

      if (telemetry != null)
      {
        telemetry.Context.User.AccountId = (message.From?.Id ?? message.ForwardFrom?.Id)?.ToString();
        telemetry.Context.User.AuthenticatedUserId = message.From?.Id.ToString() ?? message.ForwardFrom?.Id.ToString();
        telemetry.Properties["uid"] = message.From?.Id.ToString() ?? message.ForwardFrom?.Id.ToString();
        telemetry.Properties["username"] = message.From?.Username ?? message.ForwardFrom?.Username;
        telemetry.Properties["messageType"] = message.Type.ToString();
        telemetry.Properties["chat"] = message.Chat.Username;
        telemetry.Properties["cid"] = message.Chat.Id.ToString();
        telemetry.Properties["mid"] = message.MessageId.ToString();
      }

      var result = await ProcessMessage(async (msg, context, properties, ct) =>
      {
        foreach (var property in properties)
        {
          telemetry?.Properties.Add(property);
        }

        return await HandlerExtensions<TMessageResult>.Handle(myMessageHandlers.Bind(message), message, (updateType, context), ct).ConfigureAwait(false);
      }, message, cancellationToken);

      if (!EqualityComparer<TMessageResult>.Default.Equals(result, default))
        return true;
      
      return default;
    }
    
    protected virtual TMessageContext? GetMessageContext(Message message) => default;

    protected virtual Task<TMessageResult?> ProcessMessage(Func<Message, TMessageContext?, IDictionary<string, string>, CancellationToken, Task<TMessageResult?>> processor,
      Message message,  CancellationToken cancellationToken = default)
    {
      return processor(message, GetMessageContext(message), new Dictionary<string, string>(0), cancellationToken);
    }

  }
}