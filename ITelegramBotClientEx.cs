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
      int? cacheTime = default,
      bool? isPersonal = default,
      string? nextOffset = default,
      InlineQueryResultsButton? button = default,
      CancellationToken cancellationToken = default);
  }
}