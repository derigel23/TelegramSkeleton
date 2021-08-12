using System;

namespace Team23.TelegramSkeleton
{
  public static class LazyEx
  {
    public static void Deconstruct<TValue, TMetadata>(this Lazy<TValue, TMetadata> lazy, out TValue value, out TMetadata metadata)
    {
      value = lazy.Value;
      metadata = lazy.Metadata;
    }
  }
}