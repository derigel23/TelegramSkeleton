using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public interface IBotCommandHandlerAttribute<in TContext> : IHandlerAttribute<MessageEntityEx, TContext>
  {
    public BotCommandScope Scope { get; }      
    public BotCommand Command { get; }      
    [CanBeNull] public string[] Aliases { get; }      
  }

  public static class BotCommandHandler
  {
    public static readonly BotCommandScopeType[] SupportedBotCommandScopeTypes =
    {
      BotCommandScopeType.Default,
      BotCommandScopeType.AllPrivateChats,
      BotCommandScopeType.AllGroupChats,
      BotCommandScopeType.AllChatAdministrators
    };
    
    public static BotCommandScope GetScope(BotCommandScopeType botCommandScopeType)
    {
      return botCommandScopeType switch
      {
        BotCommandScopeType.Default => BotCommandScope.Default(),
        BotCommandScopeType.AllPrivateChats => BotCommandScope.AllPrivateChats(),
        BotCommandScopeType.AllGroupChats => BotCommandScope.AllGroupChats(),
        BotCommandScopeType.AllChatAdministrators => BotCommandScope.AllChatAdministrators(),
        _ => throw new ArgumentOutOfRangeException(nameof(botCommandScopeType), botCommandScopeType, null)
      };
    }

    public static bool ShouldProcess<TContext, TResult>(this IBotCommandHandler<TContext, TResult> handler, MessageEntityEx entity, TContext context)
    {
      foreach (var metadata in handler.GetType().GetCustomAttributes().OfType<IBotCommandHandlerAttribute<TContext>>())
      {
        if (!ShouldProcess(metadata, entity, context))
          return false;
      }

      return true;
    }

    public static bool ShouldProcess<TContext>(IBotCommandHandlerAttribute<TContext> attribute, MessageEntityEx entity, TContext context)
    {
      if (entity.Type != MessageEntityType.BotCommand)
        return false;

      var message = entity.Message;

      if (attribute.Scope switch
      {
        BotCommandScopeDefault => true,
        BotCommandScopeAllPrivateChats => message.Chat.Type is ChatType.Private or ChatType.Sender,
        BotCommandScopeAllGroupChats => message.Chat.Type is ChatType.Group or ChatType.Supergroup,
        BotCommandScopeAllChatAdministrators => message.Chat.Type is ChatType.Group or ChatType.Supergroup, // TODO: Check for admins
        BotCommandScopeChat scope => message.Chat == scope.ChatId,
        BotCommandScopeChatAdministrators scope => message.Chat == scope.ChatId, // TODO: Check for admins
        BotCommandScopeChatMember scope => message.Chat == scope.ChatId && message.From?.Id == scope.UserId,
        _ => true
      })
      {
        // check command (skip first slash)
        var command = entity.Command.Subsegment(1);
        if (command.Equals(attribute.Command.Command, StringComparison.OrdinalIgnoreCase))
          return true;

        var aliases = attribute.Aliases ?? Array.Empty<string>();
        for (var i = 0; i < aliases.Length; i++)
        {
          if (command.Equals(aliases[i], StringComparison.OrdinalIgnoreCase))
            return true;
        }
      }

      return false;
    }
  }
}