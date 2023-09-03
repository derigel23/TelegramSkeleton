using System;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Registry;

namespace Team23.TelegramSkeleton;

public class CachedPolicyRegistry : PolicyRegistry
{
  private readonly IMemoryCache myMemoryCache;

  public CachedPolicyRegistry(IMemoryCache memoryCache)
  {
    myMemoryCache = memoryCache;
  }
  
  public TPolicy? GetOrAdd<TPolicy>(string key, Func<string, ICacheEntry, TPolicy> policyFactory) where TPolicy : IsPolicy
  {
    return myMemoryCache.GetOrCreate(key, entry =>
    {
      entry.RegisterPostEvictionCallback((kkey, value, _, _) =>
      {
        var a = TryRemove((string)kkey, out IsPolicy policy) && policy == value;
      });
      return policyFactory(key, entry);
    });
  }
}