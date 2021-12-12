using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton
{
  public interface IUpdateHandler : IHandler<Update, OperationTelemetry, bool?>
  {
  }
  
  [MeansImplicitUse]
  [BaseTypeRequired(typeof(IUpdateHandler))]
  public class UpdateHandlerAttribute : Attribute, IHandlerAttribute<Update, OperationTelemetry>
  {
    public UpdateType UpdateType
    {
      get => UpdateType.Unknown; // never use
      set => UpdateTypes = new[] {value};
    }

    public UpdateType[]? UpdateTypes { get; set; }

    public bool ShouldProcess(Update update, OperationTelemetry? telemetry)
    {
      return UpdateTypes?.Contains(update.Type) ?? false;
    }

    public int Order => 0;
  }
}