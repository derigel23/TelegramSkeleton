using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Team23.TelegramSkeleton;

public class TextBuilder 
{
  private readonly StringBuilder myStringBuilder = new ();
  private readonly List<MessageEntity> myEntities = new ();

  public IDisposable Bold()
  {
    return new Entity(MessageEntityType.Bold, this);
  }

  public TextBuilder Bold(string text)
  {
    using (Bold())
    {
      myStringBuilder.Append(text);
    }

    return this;
  }

  private class Entity : IDisposable
  {
    private readonly MessageEntity myEntity;
    private readonly TextBuilder myBuilder;

    public Entity(MessageEntityType entityType, TextBuilder builder) :
      this(new MessageEntity { Type = entityType }, builder) { }

    public Entity(MessageEntity entity, TextBuilder builder)
    {
      myEntity = entity;
      myBuilder = builder;
      myEntity.Offset = myBuilder.myStringBuilder.Length;
      myBuilder.myEntities.Add(myEntity);
    }
    
    public void Dispose()
    {
      myEntity.Length = myBuilder.myStringBuilder.Length - myEntity.Offset;
      if (myEntity.Length < 0)
        throw new InvalidEnumArgumentException("Negative length");
    }
  }
}