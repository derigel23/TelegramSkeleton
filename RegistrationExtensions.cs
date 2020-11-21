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
      Func<HttpClient, IServiceProvider, IEnumerable<TTelegramBotClient>> botCollectionFactory = (client, provider) =>
      {
        var retriever = provider.GetService<Func<HttpClient, string, TTelegramBotClient>>();
        if (tokensFactory(provider) is {} tokens)
        {
          var result = new TTelegramBotClient[tokens.Length];
          for (var i = 0; i < tokens.Length; i++)
          {
            result[i] = retriever(client, tokens[i]);
          }
          return result;
        }

        return Enumerable.Empty<TTelegramBotClient>();
      };

      TTelegramBotClient BotFactory(HttpClient client, IServiceProvider provider)
      {
        var registeredBots = provider.GetServices<TTelegramBotClient>();
        if (provider.GetService<IActionContextAccessor>() is {ActionContext: { } actionContext} && actionContext.RouteData.Values.TryGetValue(nameof(ITelegramBotClient.BotId), out var routeBotId))
        {
          var botId = Convert.ToInt32(routeBotId);
          foreach (var telegramBotClient in registeredBots)
          {
            if (telegramBotClient.BotId == botId) return telegramBotClient;
          }
        }

        // Specific bot is unknown
        return default;
      }

      services
        .AddHttpClient(nameof(TTelegramBotClient))
        .AddTypedClient<IEnumerable<ITelegramBotClient>>(botCollectionFactory)
        .AddTypedClient<IEnumerable<ITelegramBotClientEx>>(botCollectionFactory)
        .AddTypedClient<IEnumerable<TITelegramBotClient>>(botCollectionFactory)
        .AddTypedClient<IDictionary<int, ITelegramBotClient>>((client, provider) => botCollectionFactory(client, provider).ToDictionary(_ => _.BotId, _ => (ITelegramBotClient)_))
        .AddTypedClient<IDictionary<int, ITelegramBotClientEx>>((client, provider) => botCollectionFactory(client, provider).ToDictionary(_ => _.BotId, _ => (ITelegramBotClientEx)_))
        .AddTypedClient<IDictionary<int, TITelegramBotClient>>((client, provider) => botCollectionFactory(client, provider).ToDictionary(_ => _.BotId, _ => (TITelegramBotClient)_))
        .AddTypedClient<ITelegramBotClient>((Func<HttpClient, IServiceProvider, TTelegramBotClient>) BotFactory)
        .AddTypedClient<ITelegramBotClientEx>((Func<HttpClient, IServiceProvider, TTelegramBotClient>) BotFactory)
        .AddTypedClient<TITelegramBotClient>((Func<HttpClient, IServiceProvider, TTelegramBotClient>) BotFactory);
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