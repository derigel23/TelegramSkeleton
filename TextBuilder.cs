using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace Team23.TelegramSkeleton;

public class TextBuilder 
{
  private readonly StringBuilder myStringBuilder;
  private readonly List<MessageEntity> myEntities = new();

  public TextBuilder()
  {
    myStringBuilder = new();
  }

  public TextBuilder(string text)
  {
    myStringBuilder = new(text);
  }

  public TextBuilder Append(TextBuilder other)
  {
    var offset = myStringBuilder.Length;
    myStringBuilder.Append(other.myStringBuilder);
    foreach (var otherEntity in other.myEntities)
    {
      myEntities.Add(new MessageEntity
      {
        Offset = otherEntity.Offset + offset,
        Length = otherEntity.Length,
        Type = otherEntity.Type,
        Url = otherEntity.Url,
        User = otherEntity.User,
        Language = otherEntity.Language,
      });
    }

    return this;
  }
  
  public static implicit operator StringBuilder(TextBuilder builder) => builder.myStringBuilder;
  public static StringBuilder operator ~(TextBuilder builder) => builder.myStringBuilder;
  
  public int Length
  {
    get => myStringBuilder.Length;
    set => myStringBuilder.Length = value;
  }

  public override string ToString()
  {
    return myStringBuilder.ToString();
  }

  public IDisposable Entity(MessageEntity entity) =>
    new EntityAction(entity, this);

  public IDisposable Entity(MessageEntityType entityType, int? length = null) =>
    new EntityAction(new MessageEntity { Type = entityType, Length = length ?? 0 }, this);

  public InputTextMessageContent ToTextMessageContent(bool? disableWebPreview = default) =>
    new (myStringBuilder.ToString())
    {
      Entities = myEntities.ToArray(),
      ParseMode = null,
      DisableWebPagePreview = disableWebPreview
    };

  private class EntityAction : IDisposable
  {
    private readonly MessageEntity myEntity;
    private readonly TextBuilder myBuilder;

    public EntityAction(MessageEntity entity, TextBuilder builder)
    {
      myEntity = entity;
      myBuilder = builder;
      myEntity.Offset = myBuilder.myStringBuilder.Length;
      myBuilder.myEntities.Add(myEntity);
    }
    
    public void Dispose()
    {
      // set entity length only if not predefined in ctor
      if (myEntity.Length == 0)
      {
        myEntity.Length = myBuilder.myStringBuilder.Length - myEntity.Offset;
      }
      if (myEntity.Length < 0)
        throw new InvalidEnumArgumentException("Negative length");
    }
  }
}