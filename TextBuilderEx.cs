using System;
using System.Runtime.CompilerServices;
using System.Text;
using Autofac.Util;
using JetBrains.Annotations;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton;

public static class TextBuilderEx
{
  public static readonly string NewLineString = "\n";

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder NewLine([NotNull] this TextBuilder builder)
  {
    ((StringBuilder)builder).Append(NewLineString);
    return builder;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Append([NotNull] this TextBuilder builder, [CanBeNull] string text = null)
  {
    ((StringBuilder)builder).Append(text);
    return builder;
  }

  // TODO: check and remove
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Sanitize([NotNull] this TextBuilder builder, [CanBeNull] string text = null)
  {
    ((StringBuilder)builder).Append(text);
    return builder;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder SanitizeNickname([NotNull] this TextBuilder builder, [CanBeNull] string text = null)
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
  public static TextBuilder Bold([NotNull] this TextBuilder builder, [NotNull, InstantHandle] Action<TextBuilder> action)
  {
    using (builder.Bold())
    {
      action(builder);
      return builder;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Bold([NotNull] this TextBuilder builder, [NotNull, InstantHandle] Action<StringBuilder> action)
  {
    using (builder.Bold())
    {
      action(builder);
      return builder;
    }
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Bold([NotNull] this TextBuilder builder, [CanBeNull] string text) =>
    builder.Bold((StringBuilder b) => b.Append(text));

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static IDisposable Italic(this TextBuilder builder) =>
    builder.Entity(MessageEntityType.Italic);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Italic([NotNull] this TextBuilder builder, [NotNull, InstantHandle] Action<TextBuilder> action)
  {
    using (builder.Italic())
    {
      action(builder);
      return builder;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Italic([NotNull] this TextBuilder builder, [NotNull, InstantHandle] Action<StringBuilder> action)
  {
    using (builder.Italic())
    {
      action(builder);
      return builder;
    }
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Italic([NotNull] this TextBuilder builder, [CanBeNull] string text) =>
    builder.Italic((StringBuilder b) => b.Append(text));

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static IDisposable Code(this TextBuilder builder) =>
    builder.Entity(MessageEntityType.Code);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Code([NotNull] this TextBuilder builder, [NotNull, InstantHandle] Action<TextBuilder> action)
  {
    using (builder.Code())
    {
      action(builder);
      return builder;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Code([NotNull] this TextBuilder builder, [NotNull, InstantHandle] Action<StringBuilder> action)
  {
    using (builder.Code())
    {
      action(builder);
      return builder;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Code([NotNull] this TextBuilder builder, [CanBeNull] string text) =>
    builder.Code((StringBuilder b) => b.Append(text));

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Link([NotNull] this TextBuilder builder, [NotNull] Action<TextBuilder> action, [CanBeNull] string link)
  {
    using (string.IsNullOrEmpty(link) ? new Disposable() : builder.Entity(new MessageEntity { Type = MessageEntityType.TextLink, Url = link }))
    {
      action(builder);
      return builder;
    }
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Link([NotNull] this TextBuilder builder, [NotNull] string text, [CanBeNull] string link) =>
    builder.Link(sb => sb.Append(text), link);
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static TextBuilder Link([NotNull] this TextBuilder builder, [NotNull] Action<TextBuilder> action, [CanBeNull] User user)
  {
    using (user == null ? new Disposable() : builder.Entity(new MessageEntity { Type = MessageEntityType.TextMention, User = user }))
    {
      action(builder);
      return builder;
    }
  }

}