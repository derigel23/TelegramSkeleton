using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public abstract class TextMessageHandler<TContext, TResult, TMetadata> : IMessageHandler<TContext, TResult>
    where TMetadata : DescriptionAttribute, IHandlerAttribute<MessageEntityEx, TContext>
  {
    private readonly ITelegramBotClient myBot;
    private readonly IEnumerable<Lazy<Func<Message, IMessageEntityHandler<TContext, TResult>>, TMetadata>> myMessageEntityHandlers;

    protected TextMessageHandler(ITelegramBotClient bot, IEnumerable<Lazy<Func<Message, IMessageEntityHandler<TContext, TResult>>, TMetadata>> messageEntityHandlers)
    {
      myBot = bot;
      myMessageEntityHandlers = messageEntityHandlers;
    }

    public virtual async Task<TResult> Handle(Message message, (UpdateType updateType, TContext context) _, CancellationToken cancellationToken = default)
    {
      var handlers = myMessageEntityHandlers.Bind(message).ToList();
      TResult result = default;
      string botName = null;
      foreach (var entity in message.Entities ?? Enumerable.Empty<MessageEntity>())
      {
        var entityEx = new MessageEntityEx(message, entity);
        // check bot name, if presents
        if (entityEx.Type == MessageEntityType.BotCommand && entityEx.CommandBot is { } commandBot && !StringSegment.IsNullOrEmpty(commandBot))  
        {
          if (!commandBot.Equals(botName ??= (await myBot.GetMeAsync(cancellationToken)).Username, StringComparison.OrdinalIgnoreCase))
            continue;
        }
        result = await HandlerExtensions<TResult>.Handle(handlers, entityEx, _.context, cancellationToken).ConfigureAwait(false);
        if (!EqualityComparer<TResult>.Default.Equals(result, default)) break;
        // internal command shows all supported commands
        if (entityEx.Type == MessageEntityType.BotCommand && entityEx.Command == "/commands")  
        {
          var fakeCommandEntity = new MessageEntityEx(message, new MessageEntity { Type = MessageEntityType.BotCommand});
          var content = new StringBuilder();
          foreach (var meta in myMessageEntityHandlers.OrderBy(meta => meta.Metadata.Order))
          {
            if (!meta.Metadata.ShouldProcess(fakeCommandEntity, _.context)) continue;
            if (string.IsNullOrEmpty(meta.Metadata.Description)) continue;
            content.AppendLine(meta.Metadata.Description);
          }
          if (content.Length == 0) continue;
          await myBot.SendTextMessageAsync(message.Chat,content.ToString(), cancellationToken: cancellationToken).ConfigureAwait(false);
          return default;
        }
      }

      return result;
    }
  }
}