using System;
using System.Runtime.CompilerServices;
using System.Text;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton;

public static partial class TextBuilderEx
{
  private class TextBuilderFormatProvider : IFormatProvider, ICustomFormatter
  {
    private readonly TextBuilder myBuilder;

    internal TextBuilderFormatProvider(TextBuilder builder)
    {
      myBuilder = builder;
    }
    
    object? IFormatProvider.GetFormat(Type? formatType)
    {
      return formatType == typeof(ICustomFormatter) ? this : null;
    }

    string ICustomFormatter.Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
      string result;
      if (arg is IFormattable formattable)
      {
        result = formattable.ToString(format, formatProvider);
      }
      else
      {
        result = arg?.ToString() ?? "";
      }

      MessageEntityType? entityType = format switch
      {
        "code" => MessageEntityType.Code,
        "bold" => MessageEntityType.Bold,
        "italic" => MessageEntityType.Italic,
        _ => null
      };

      using var disposable = entityType != null ? myBuilder.Entity(entityType.Value, result.Length) : null;
      return result;
    }
  }
  
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Append(this TextBuilder builder, FormattableString text)
  {
    StringBuilder stringBuilder = builder;
    if (text.ArgumentCount == 0)
    {
      stringBuilder.Append(text);
    }
    else
    {
      stringBuilder.AppendFormat(new TextBuilderFormatProvider(builder), text.Format, text.GetArguments());
    }

    return builder;
  }

}