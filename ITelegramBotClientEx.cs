using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;

namespace Team23.TelegramSkeleton
{
  public interface ITelegramBotClientEx : ITelegramBotClient
  {
    Task AnswerInlineQueryWithValidationAsync(
      string inlineQueryId,
      IReadOnlyCollection<InlineQueryResult> results,
      int? cacheTime = null,
      bool isPersonal = false,
      string? nextOffset = null,
      string? switchPmText = null,
      string? switchPmParameter = null,
      CancellationToken cancellationToken = default);
  }
}