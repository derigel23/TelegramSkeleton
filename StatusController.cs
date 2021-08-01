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

    protected StatusController(IEnumerable<ITelegramBotClient> bots, IEnumerable<IStatusProvider> statusProviders, IEnumerable<Lazy<Func<Message, IBotCommandHandler<TContext, TResult>>, TCommandHandlerAttribute>> commandHandlers)
    {
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
      var botCommands = myCommandHandlers
        .ToLookup(handler => handler.Metadata.Scope?.Type);

      foreach (var bot in myBots)
      {
        var webHookUrl = Url.Action("Update", "Telegram", new { bot.BotId }, protocol: "https");
      
        await bot.SetWebhookAsync(webHookUrl, cancellationToken: cancellationToken);

        var everyWhereCommands = botCommands[default].ToList();
        
        foreach (var botCommandScopeType in BotCommandHandler.SupportedBotCommandScopeTypes)
        {
          var commands = botCommands[botCommandScopeType].Concat(everyWhereCommands)
            .OrderBy(handler => handler.Metadata.Order)
            .Select(handler => handler.Metadata.Command);
          await bot.SetMyCommandsAsync(commands, BotCommandHandler.GetScope(botCommandScopeType), cancellationToken: cancellationToken);
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