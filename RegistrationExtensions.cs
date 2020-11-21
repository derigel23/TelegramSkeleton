using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace Team23.TelegramSkeleton
{
  public static class RegistrationExtensions
  {
    public static void RegisterTelegramClients<TTelegramBotClient>(this IServiceCollection services, Func<IServiceProvider, string[]> tokensFactory)
      where TTelegramBotClient : class, ITelegramBotClientEx
      => RegisterTelegramClients<TTelegramBotClient, ITelegramBotClientEx>(services, tokensFactory);
    
    public static void RegisterTelegramClients<TTelegramBotClient, TITelegramBotClient>(this IServiceCollection services, Func<IServiceProvider, string[]> tokensFactory)
      where TTelegramBotClient : class, TITelegramBotClient
      where TITelegramBotClient : class, ITelegramBotClientEx
    {
      IEnumerable<T> BotCollectionFactory<T>(HttpClient client, IServiceProvider provider)
        where T : class, ITelegramBotClient
      {
        var retriever = provider.GetService<Func<HttpClient, string, TTelegramBotClient>>();
        if (tokensFactory(provider) is { } tokens)
        {
          var result = new T[tokens.Length];
          for (var i = 0; i < tokens.Length; i++)
          {
            result[i] = retriever(client, tokens[i]) as T;
          }

          return result;
        }

        return Enumerable.Empty<T>();
      }

      IDictionary<int, T> BotDictionaryFactory<T>(HttpClient client, IServiceProvider provider)
        where T : class, ITelegramBotClient
      {
        var retriever = provider.GetService<Func<HttpClient, string, TTelegramBotClient>>();
        if (tokensFactory(provider) is { } tokens)
        {
          var result = new Dictionary<int, T>(tokens.Length);
          foreach (var token in tokens)
          {
            var bot = retriever(client, token) as T;
            if (bot == null) continue;
            result[bot.BotId] = bot;
          }

          return result;
        }

        return new Dictionary<int, T>(0);

      }

      T BotFactory<T>(HttpClient client, IServiceProvider provider)
        where T : ITelegramBotClient
      {
        var registeredBots = provider.GetService<IDictionary<int, T>>();
        if (provider.GetService<IActionContextAccessor>() is {ActionContext: { } actionContext} && actionContext.RouteData.Values.TryGetValue(nameof(ITelegramBotClient.BotId), out var routeBotId))
        {
          var botId = Convert.ToInt32(routeBotId);
          if (registeredBots.TryGetValue(botId, out var bot))
            return bot;
        }

        // Specific bot is unknown
        return default;
      }

      services
        .AddHttpClient(nameof(TTelegramBotClient))
        .AddTypedClient(BotCollectionFactory<ITelegramBotClient>)
        .AddTypedClient(BotCollectionFactory<ITelegramBotClientEx>)
        .AddTypedClient(BotCollectionFactory<TITelegramBotClient>)
        .AddTypedClient(BotDictionaryFactory<ITelegramBotClient>)
        .AddTypedClient(BotDictionaryFactory<ITelegramBotClientEx>)
        .AddTypedClient(BotDictionaryFactory<TITelegramBotClient>)
        .AddTypedClient(BotFactory<ITelegramBotClient>)
        .AddTypedClient(BotFactory<ITelegramBotClientEx>)
        .AddTypedClient(BotFactory<TITelegramBotClient>);
    }

    public static void RegisterTelegramSkeleton<TTelegramBotClient>(this ContainerBuilder builder, Assembly assembly = null)
      where TTelegramBotClient : ITelegramBotClientEx
    {
      builder.RegisterType<TTelegramBotClient>().InstancePerDependency();
      
      builder
        .RegisterAssemblyTypes(Assembly.GetExecutingAssembly(), assembly ?? Assembly.GetCallingAssembly())
        .Where(t => t.InheritsOrImplements(typeof(IHandler<,,>)))
        .AsImplementedInterfaces()
        .AsClosedTypesOf(typeof(IHandler<,,>))
        .AsSelf()
        .WithMetadata(t =>
        {
          var metadata = new Dictionary<string, object>();
          foreach (var handlerAttribute in CustomAttributeExtensions.GetCustomAttributes(t.GetTypeInfo(), true))
          {
            if (!handlerAttribute.GetType().InheritsOrImplements(typeof(IHandlerAttribute<,>))) continue;
            foreach (var propertyInfo in handlerAttribute.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
              if (propertyInfo.CanRead && propertyInfo.CanWrite)
              {
                metadata.Add(propertyInfo.Name, propertyInfo.GetValue(handlerAttribute));
              }
            }
          }
          return metadata;
        })
        .InstancePerLifetimeScope();
    }
    
    public static bool InheritsOrImplements(this Type child, Type parent)
    {
      parent = ResolveGenericTypeDefinition(parent);

      var currentChild = child.IsGenericType
        ? child.GetGenericTypeDefinition()
        : child;

      while (currentChild != typeof (object))
      {
        if (parent == currentChild || HasAnyInterfaces(parent, currentChild))
          return true;

        currentChild = currentChild.BaseType != null
                       && currentChild.BaseType.IsGenericType
          ? currentChild.BaseType.GetGenericTypeDefinition()
          : currentChild.BaseType;

        if (currentChild == null)
          return false;
      }
      return false;
    }

    private static bool HasAnyInterfaces(Type parent, Type child)
    {
      return child.GetInterfaces()
        .Any(childInterface =>
        {
          var currentInterface = childInterface.IsGenericType
            ? childInterface.GetGenericTypeDefinition()
            : childInterface;

          return currentInterface == parent;
        });
    }

    private static Type ResolveGenericTypeDefinition(Type parent)
    {
      var shouldUseGenericType = !(parent.IsGenericType && parent.GetGenericTypeDefinition() != parent);

      if (parent.IsGenericType && shouldUseGenericType)
        parent = parent.GetGenericTypeDefinition();
      
      return parent;
    }
  }
}