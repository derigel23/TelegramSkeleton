using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Team23.TelegramSkeleton
{
  public static class TelegramBotEx
  {
    public static async Task EditMessageReplyMarkupAsync(
      this ITelegramBotClient botClient,
      CallbackQuery callbackQuery,
      InlineKeyboardMarkup? replyMarkup = default,
      CancellationToken cancellationToken = default
    )
    {
      if (callbackQuery.InlineMessageId is { } inlineMessageId)
      {
        await botClient.EditMessageReplyMarkupAsync(inlineMessageId, replyMarkup, cancellationToken);
      }
      else if (callbackQuery.Message is {} message)
      {
        await botClient.EditMessageReplyMarkupAsync(message.Chat, callbackQuery.Message.MessageId, replyMarkup, cancellationToken);
      }
      else
      {
        throw new ArgumentOutOfRangeException(nameof(callbackQuery));
      }
    }

  }
}