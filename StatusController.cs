using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Telegram.Bot;

namespace Team23.TelegramSkeleton
{
  public class StatusController : Controller
  {
    private readonly ITelegramBotClient myBot;
    private readonly IEnumerable<IStatusProvider> myStatusProviders;

    public StatusController(ITelegramBotClient bot, IEnumerable<IStatusProvider> statusProviders)
    {
      myBot = bot;
      myStatusProviders = statusProviders;
    }

    private static readonly JsonSerializerSettings NoUnixDateTimeJsonSerializerSettings = new JsonSerializerSettings()
    {
      ContractResolver = new NoUnixDateTimeContractResolver()
    };
    
    private class NoUnixDateTimeContractResolver : DefaultContractResolver
    {
      protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
      {
        var jsonProperty = base.CreateProperty(member, memberSerialization);
        jsonProperty.Converter = jsonProperty.Converter is UnixDateTimeConverter ? null : jsonProperty.Converter;
        return jsonProperty;
      }
    }
    
    [HttpGet("/status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
      return Json(await GetStatusData(cancellationToken).ConfigureAwait(false), NoUnixDateTimeJsonSerializerSettings);
    }

    private async Task<IDictionary<string, object>> GetStatusData(CancellationToken cancellationToken)
    {
      var status = new Dictionary<string, object>
      {
        { "botInfo", await myBot.GetMeAsync(cancellationToken) },
        { "webhookInfo", await myBot.GetWebhookInfoAsync(cancellationToken) },
        { "is64BitProcess", System.Environment.Is64BitProcess },
      };
      foreach (var statusProvider in myStatusProviders)
      {
        await statusProvider.Handle(status, ControllerContext, cancellationToken);
      }

      return status;
    }
    
    [HttpGet("/refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
      var webHookUrl = Url.Action("Update", "Telegram", null, protocol: "https");
      
      await myBot.SetWebhookAsync(webHookUrl, cancellationToken: cancellationToken);

      return RedirectToAction("Status");
    }

    [HttpGet("/clear")]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
      await myBot.SetWebhookAsync("", cancellationToken: cancellationToken);

      await myBot.GetUpdatesAsync(-1, 1, cancellationToken: cancellationToken);

      return RedirectToAction("Status");
    }
  }

  public interface IStatusProvider : IHandler<IDictionary<string, object>, ControllerContext, IDictionary<string, object>>
  {
  }
}