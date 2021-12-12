using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Team23.TelegramSkeleton
{
  public abstract class StatusController<TContext, TResult, TCommandHandlerAttribute> : Controller
    where TCommandHandlerAttribute : Attribute, IBotCommandHandlerAttribute<TContext>
  {
    private readonly IEnumerable<ITelegramBotClient> myBots;
    private readonly IEnumerable<IStatusProvider> myStatusProviders;

    private readonly IEnumerable<Lazy<Func<Message, IBotCommandHandler<TContext, TResult>>, TCommandHandlerAttribute>> myCommandHandlers;
    private readonly IWebHookSaltProvider? myWebHookSaltProvider;

    protected StatusController(IWebHookSaltProvider? webHookSaltProvider, IEnumerable<ITelegramBotClient> bots, IEnumerable<IStatusProvider> statusProviders, IEnumerable<Lazy<Func<Message, IBotCommandHandler<TContext, TResult>>, TCommandHandlerAttribute>> commandHandlers)
    {
      myWebHookSaltProvider = webHookSaltProvider;
      myBots = bots;
      myStatusProviders = statusProviders;
      myCommandHandlers = commandHandlers;
    }

    private static readonly JsonSerializerSettings NoUnixDateTimeJsonSerializerSettings = new()
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
        { "bots", await Task.WhenAll(myBots.Select(async bot => new
          {
            bot = await bot.GetMeAsync(cancellationToken),
            hook = await bot.GetWebhookInfoAsync(cancellationToken)
          }))
        },
        { "Framework", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription },
        { "is64BitProcess", Environment.Is64BitProcess },
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
      var botCommands = myCommandHandlers
        .SelectMany(handler =>
          handler.Metadata.Scopes.Select(scope => KeyValuePair.Create(scope.Type, handler.Metadata)))
        .ToLookup(pair => pair.Key, pair => pair.Value);

      foreach (var bot in myBots)
      {
        var webHookUrl = Url.Action("Update", "Telegram", TelegramController.EncodeBotId(bot.BotId, myWebHookSaltProvider), protocol: "https");
      
        await bot.SetWebhookAsync(webHookUrl!, cancellationToken: cancellationToken);

        foreach (var group in botCommands)
        {
          var commands = group
            .OrderBy(metadata => metadata.Order)
            .Select(metadata => metadata.Command);
          // TODO: use original scope from metadata
          await bot.SetMyCommandsAsync(commands, BotCommandHandler.GetScope(group.Key), cancellationToken: cancellationToken);
        }
      }

      return RedirectToAction("Status");
    }

    [HttpGet("/clear")]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
      foreach (var bot in myBots)
      {
        foreach (var scopeType in BotCommandHandler.SupportedBotCommandScopeTypes)
        {
          await bot.DeleteMyCommandsAsync(BotCommandHandler.GetScope(scopeType), cancellationToken: cancellationToken);
        }

        await bot.SetWebhookAsync("", cancellationToken: cancellationToken);
        await bot.GetUpdatesAsync(-1, 1, cancellationToken: cancellationToken);
      }

      return RedirectToAction("Status");
    }
  }

  public interface IStatusProvider : IHandler<IDictionary<string, object>, ControllerContext, IDictionary<string, object>>
  {
  }
}