using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;

namespace Team23.TelegramSkeleton
{
  public class TelegramBotClientEx : TelegramBotClient, ITelegramBotClientEx
  {
    private readonly TelemetryClient myTelemetryClient;
    
    public TelegramBotClientEx(TelemetryClient telemetryClient, string token, HttpClient? httpClient = null) : base(token, httpClient)
    {
      myTelemetryClient = telemetryClient;
    }

    public async Task AnswerInlineQueryWithValidationAsync(string inlineQueryId, IReadOnlyCollection<InlineQueryResult> results, int? cacheTime = default,
      bool? isPersonal = default, string? nextOffset = default, InlineQueryResultsButton? button = default, CancellationToken cancellationToken = default)
    {
      ISet<string> processedIds = new HashSet<string>();
      foreach (var result in results)
      {
        if (!processedIds.Add(result.Id))
        {
          myTelemetryClient.TrackException(
            new DuplicateNameException(result.Id), new Dictionary<string, string?> { { nameof(ITelegramBotClient.BotId), BotId.ToString() } });
        }
      }
      await this
        .AnswerInlineQueryAsync(inlineQueryId, results, cacheTime, isPersonal, nextOffset, button, cancellationToken)
        .ConfigureAwait(false);
    }
  }
}