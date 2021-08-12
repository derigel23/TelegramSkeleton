using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace Team23.TelegramSkeleton
{
  public static class TelegramBotEx
  {
    public static async Task EditMessageReplyMarkupAsync(
      this ITelegramBotClient botClient,
      CallbackQuery callbackQuery,
      InlineKeyboardMarkup replyMarkup = default,
      CancellationToken cancellationToken = default
    )
    {
      switch (callbackQuery)
      {
        case { InlineMessageId: { } inlineMessageId }:
          await botClient.EditMessageReplyMarkupAsync(inlineMessageId, replyMarkup, cancellationToken).ConfigureAwait(false);
          break;
        
        case { Message: { } message }:
          await botClient.EditMessageReplyMarkupAsync(message.Chat, callbackQuery.Message.MessageId, replyMarkup, cancellationToken).ConfigureAwait(false);
          break;
        
        default:
          throw new ArgumentOutOfRangeException(nameof(callbackQuery));
      }
    }

    public static async Task<Message> SendTextMessageAsync(
      this ITelegramBotClient botClient,
      ChatId chatId,
      InputTextMessageContent content,
      bool? disableNotification = default,
      int? replyToMessageId = default,
      bool? allowSendingWithoutReply = default,
      IReplyMarkup replyMarkup = default,
      CancellationToken cancellationToken = default) =>
        await botClient.SendTextMessageAsync(chatId, content.MessageText, content.ParseMode, content.Entities, content.DisableWebPagePreview,
          disableNotification, replyToMessageId, allowSendingWithoutReply, replyMarkup, cancellationToken).ConfigureAwait(false);

    public static async Task<Message> EditMessageTextAsync(
      this ITelegramBotClient botClient,
      ChatId chatId,
      int messageId,
      InputTextMessageContent content,
      InlineKeyboardMarkup replyMarkup = default,
      CancellationToken cancellationToken = default) =>
        await botClient.EditMessageTextAsync(chatId, messageId, content.MessageText, content.ParseMode, content.Entities, content.DisableWebPagePreview,
          replyMarkup, cancellationToken).ConfigureAwait(false);
    
    public static async Task EditMessageTextAsync(
      this ITelegramBotClient botClient,
      string inlineMessageId,
      InputTextMessageContent content,
      InlineKeyboardMarkup replyMarkup = default,
      CancellationToken cancellationToken = default) =>
        await botClient.EditMessageTextAsync(inlineMessageId, content.MessageText, content.ParseMode, content.Entities, content.DisableWebPagePreview,
          replyMarkup, cancellationToken).ConfigureAwait(false);


  }
}