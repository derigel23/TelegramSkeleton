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
    public BotCommandScope[] Scopes { get; set; }      
    public BotCommand Command { get; set; }      
    [CanBeNull] public string[] Aliases { get; set; }
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

    public static bool ShouldProcess<TContext, TResult>(this IBotCommandHandler<TContext, TResult> handler, MessageEntityEx entity, TContext context, Predicate<MessageEntityEx> alternativeCheck = default)
    {
      foreach (var metadata in handler.GetType().GetCustomAttributes().OfType<IBotCommandHandlerAttribute<TContext>>())
      {
        if (!ShouldProcess(metadata, entity, context, alternativeCheck))
          return false;
      }

      return true;
    }

    public static bool ShouldProcess<TContext>(IBotCommandHandlerAttribute<TContext> attribute, MessageEntityEx entity, TContext context, Predicate<MessageEntityEx> alternativeCheck = default)
    {
      if (entity.Type != MessageEntityType.BotCommand)
        return false;

      // check command (skip first slash)
      var command = entity.Command.Subsegment(1);
      var commandMatch = command.Equals(attribute.Command.Command, StringComparison.OrdinalIgnoreCase);

      var aliases = attribute.Aliases ?? Array.Empty<string>();
      for (var i = 0; i < aliases.Length && !commandMatch; i++)
      {
        commandMatch = command.Equals(aliases[i], StringComparison.OrdinalIgnoreCase);
      }

      if (!commandMatch) return false;
      
      var message = entity.Message;

      // check command scopes
      foreach (var commandScope in attribute.Scopes)
      {
        if (commandScope switch
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
          return true;
        }
      }

      if (alternativeCheck?.Invoke(entity) ?? false)
        return true;

      return false;
    }
  }
}