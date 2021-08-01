using System;
using JetBrains.Annotations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public interface IBotCommandHandlerAttribute<in TContext> : IHandlerAttribute<MessageEntityEx, TContext>
  {
    public BotCommandScope Scope { get; [UsedImplicitly] set; }      
    public BotCommand Command { get; [UsedImplicitly] set; }      
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
  }
}