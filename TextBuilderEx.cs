using System;
using System.Runtime.CompilerServices;
using System.Text;
using Autofac.Util;
using JetBrains.Annotations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton;

public static partial class TextBuilderEx
{
  public static readonly string NewLineString = "\n";

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder NewLine(this TextBuilder builder)
  {
    ((StringBuilder)builder).Append(NewLineString);
    return builder;
  }
  
  // TODO: check and remove
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Sanitize(this TextBuilder builder, string? text = null)
  {
    ((StringBuilder)builder).Append(text);
    return builder;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder SanitizeNickname(this TextBuilder builder, string? text = null)
  {
    if (string.IsNullOrEmpty(text)) return builder;
    StringBuilder sb = builder;
    foreach (var c in text)
    {
      if (char.IsAscii(c))
        sb.Append(c);
    }
    return builder;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static IDisposable Bold(this TextBuilder builder) =>
    builder.Entity(MessageEntityType.Bold);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Bold(this TextBuilder builder, [InstantHandle] Action<TextBuilder> action)
  {
    using (builder.Bold())
    {
      action(builder);
      return builder;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Bold(this TextBuilder builder, [InstantHandle] Action<StringBuilder> action)
  {
    using (builder.Bold())
    {
      action(builder);
      return builder;
    }
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Bold(this TextBuilder builder, string? text) =>
    builder.Bold(b => b.Append(text));

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static IDisposable Italic(this TextBuilder builder) =>
    builder.Entity(MessageEntityType.Italic);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Italic(this TextBuilder builder, [InstantHandle] Action<TextBuilder> action)
  {
    using (builder.Italic())
    {
      action(builder);
      return builder;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Italic(this TextBuilder builder, [InstantHandle] Action<StringBuilder> action)
  {
    using (builder.Italic())
    {
      action(builder);
      return builder;
    }
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Italic(this TextBuilder builder, string? text) =>
    builder.Italic(b => b.Append(text));

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static IDisposable Code(this TextBuilder builder) =>
    builder.Entity(MessageEntityType.Code);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Code(this TextBuilder builder, [InstantHandle] Action<TextBuilder> action)
  {
    using (builder.Code())
    {
      action(builder);
      return builder;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Code(this TextBuilder builder, [InstantHandle] Action<StringBuilder> action)
  {
    using (builder.Code())
    {
      action(builder);
      return builder;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Code(this TextBuilder builder, string? text) =>
    builder.Code(b => b.Append(text));

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Link(this TextBuilder builder, Action<TextBuilder> action, string? link)
  {
    using (string.IsNullOrEmpty(link) ? null : builder.Entity(new MessageEntity { Type = MessageEntityType.TextLink, Url = link }))
    {
      action(builder);
      return builder;
    }
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Link(this TextBuilder builder, string text, string? link) =>
    builder.Link(sb => ((StringBuilder)sb).Append(text), link);
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Link(this TextBuilder builder, Action<TextBuilder> action, User? user)
  {
    using (user == null ? new Disposable() : builder.Entity(new MessageEntity { Type = MessageEntityType.TextMention, User = user }))
    {
      action(builder);
      return builder;
    }
  }

}