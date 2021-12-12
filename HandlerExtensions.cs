using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Team23.TelegramSkeleton
{
  public static class HandlerExtensions
  {
    public static IEnumerable<T> Bind<TParameter, T>(this IEnumerable<Func<TParameter, T>> items, TParameter parameter)
    {
      return items.Select(func => func(parameter));
    }

    public static IEnumerable<Lazy<Func<T>, TMetadata>> Bind<TParameter, T, TMetadata>(this IEnumerable<Lazy<Func<TParameter, T>, TMetadata>> items, TParameter parameter)
    {
      return items.Select(meta => new Lazy<Func<T>, TMetadata>(() => () => meta.Value(parameter), meta.Metadata));
    }
  }

  public static class HandlerExtensions<TResult>
  {
    public static async Task<TResult?> Handle<TData, TContext>(IEnumerable<IHandler<TData, TContext, TResult>> handlers, TData data, TContext? context = default, CancellationToken cancellationToken = default)
    {
      foreach (var handler in handlers)
      {
        var result = await handler.Handle(data, context, cancellationToken).ConfigureAwait(false);
        if (!EqualityComparer<TResult>.Default.Equals(result, default))
          return result;
      }

      return default;
    }

    public static async Task<TResult?> Handle<THandler, TData, TContext, TMetadata>(IEnumerable<Lazy<Func<THandler>, TMetadata>> handlers, TData data, TContext? context = default, CancellationToken cancellationToken = default)
      where THandler : IHandler<TData, TContext, TResult>
      where TMetadata : Attribute, IHandlerAttribute<TData, TContext>
    {
      foreach (var (handler, metadata) in handlers.OrderBy(meta => meta.Metadata.Order))
      {
        if (!metadata.ShouldProcess(data, context))
          continue;
        var result = await handler().Handle(data, context, cancellationToken).ConfigureAwait(false);
        if (!EqualityComparer<TResult>.Default.Equals(result, default))
          return result;
      }

      return default;
    }
  }
}