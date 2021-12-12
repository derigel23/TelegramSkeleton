using JetBrains.Annotations;

namespace Team23.TelegramSkeleton;

public interface IWebHookSaltProvider
{
  [PublicAPI] public int? GetSalt(long? botId);
}