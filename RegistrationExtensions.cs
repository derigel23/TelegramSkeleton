using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;
using Polly.RateLimit;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;

namespace Team23.TelegramSkeleton
{
  public static class RegistrationExtensions
  {
    public static void RegisterTelegramClients<TTelegramBotClient>(this IServiceCollection services, Func<IServiceProvider, string[]?> tokensFactory)
      where TTelegramBotClient : class, ITelegramBotClientEx
      => RegisterTelegramClients<TTelegramBotClient, ITelegramBotClientEx>(services, tokensFactory);
    
    public static void RegisterTelegramClients<TTelegramBotClient, TITelegramBotClient>(this IServiceCollection services, Func<IServiceProvider, string[]?> tokensFactory)
      where TTelegramBotClient : class, TITelegramBotClient
      where TITelegramBotClient : class, ITelegramBotClientEx
    {
      IEnumerable<T> BotCollectionFactory<T>(HttpClient client, IServiceProvider provider)
        where T : class, ITelegramBotClient
      {
        var retriever = provider.GetService<Func<HttpClient, string, TTelegramBotClient>>();
        if (tokensFactory(provider) is { } tokens)
        {
          var result = new List<T>(tokens.Length);
          foreach (var token in tokens)
          {
            if (retriever?.Invoke(client, token) is T bot)
            {
              bot.ExceptionsParser = ExceptionParser.Instance;
              result.Add(bot);
            }
          }

          return result;
        }

        return Enumerable.Empty<T>();
      }

      IDictionary<long, T> BotDictionaryFactory<T>(HttpClient client, IServiceProvider provider)
        where T : class, ITelegramBotClient
      {
        var retriever = provider.GetService<Func<HttpClient, string, TTelegramBotClient>>();
        if (tokensFactory(provider) is { } tokens)
        {
          var result = new Dictionary<long, T>(tokens.Length);
          foreach (var token in tokens)
          {
            if (retriever?.Invoke(client, token) is T { BotId: {} botId } bot)
            {
              bot.ExceptionsParser = ExceptionParser.Instance;
              bot.OnMakingApiRequest += BotOnOnMakingApiRequest;
              result[botId] = bot;
            }
          }

          return result;
        }

        return new Dictionary<long, T>(0);

      }

      T? BotFactory<T>(HttpClient client, IServiceProvider provider)
        where T : ITelegramBotClient
      {
        var registeredBots = provider.GetService<IDictionary<long, T>>();
        if (registeredBots != null &&
            provider.GetService<IActionContextAccessor>() is { ActionContext: { } actionContext } &&
            TelegramController.DecodeBotId(actionContext, provider.GetService<IWebHookSaltProvider>(), out var botId))
        {
          if (registeredBots.TryGetValue(botId, out var bot))
            return bot;
        }

        // Specific bot is unknown
        return default;
      }

      services.AddSingleton<CachedPolicyRegistry>();
      services
        .AddHttpClient(nameof(TTelegramBotClient))
        .AddPolicyHandler((provider, message) =>
        {
          var policyRegistry = provider.GetRequiredService<CachedPolicyRegistry>();
          IAsyncPolicy<HttpResponseMessage> policy = Policy.NoOpAsync<HttpResponseMessage>();
          
          // retry policy
          policy = policy.WrapAsync(policyRegistry.GetOrAdd("RetryPolicy", _ => Policy
            .HandleResult<HttpResponseMessage>(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .Or<RateLimitRejectedException>()
            .WaitAndRetryAsync(3, (_, result, _) =>
              {
                if (result.Exception is RateLimitRejectedException rateLimitRejectedException)
                {
                  return rateLimitRejectedException.RetryAfter;
                }
              
                var body = result.Result.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(body);
                return TimeSpan.FromSeconds(apiResponse?.Parameters?.RetryAfter ?? 3);
              },
              (_, _, _, _) => Task.CompletedTask)));

          var context = message.GetPolicyExecutionContext();
          // https://core.telegram.org/bots/faq#my-bot-is-hitting-limits-how-do-i-avoid-this
          if (context.TryGetValue(REQUEST_ID, out var request) && request is IRequest { MethodName: "sendMessage" } telegramRequest)
          {
            // general rate-limit policy
            policy = policy.WrapAsync(policyRegistry.GetOrAdd("GlobalLimitPolicy", _ =>
              Policy.RateLimitAsync<HttpResponseMessage>(30, TimeSpan.FromSeconds(1))));

            // specific rate-limit policy
            if (telegramRequest is IChatTargetable { ChatId: var chatId })
            {
              policy = policy.WrapAsync(policyRegistry.GetOrAdd($"SpecificLimitPolicyPerMinute{chatId.Identifier}", _ =>
                Policy.RateLimitAsync<HttpResponseMessage>(20, TimeSpan.FromMinutes(1))));

              policy = policy.WrapAsync(policyRegistry.GetOrAdd($"SpecificLimitPolicyPerSecond{chatId.Identifier}", _ =>
                Policy.RateLimitAsync<HttpResponseMessage>(1, TimeSpan.FromSeconds(1), 2)));
            }
          }
          
          return policy;
        })
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

    private const string REQUEST_ID = "Telegram.Request";
    
    private static ValueTask BotOnOnMakingApiRequest(ITelegramBotClient botClient, ApiRequestEventArgs args, CancellationToken cancellationToken)
    {
      var context = new Context
      {
        [REQUEST_ID] = args.Request
      };
      args.HttpRequestMessage.SetPolicyExecutionContext(context);
      return ValueTask.CompletedTask;
    }

    public static void RegisterTelegramSkeleton<TTelegramBotClient>(this ContainerBuilder builder, Assembly? assembly = null)
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
          var metadata = new Dictionary<string, object?>();
          foreach (var handlerAttribute in CustomAttributeExtensions.GetCustomAttributes(t.GetTypeInfo(), true))
          {
            if (!handlerAttribute.GetType().InheritsOrImplements(typeof(IHandlerAttribute<,>))) continue;
            foreach (var propertyInfo in handlerAttribute.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
              if (propertyInfo is { CanRead: true, CanWrite: true })
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

        currentChild = currentChild.BaseType is { IsGenericType: true }
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