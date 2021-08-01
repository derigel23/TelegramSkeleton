namespace Team23.TelegramSkeleton
{
  public interface IMessageEntityHandler<in TContext, TResult> : IHandler<MessageEntityEx, TContext, TResult>
  {
  }

  public interface IBotCommandHandler<in TContext, TResult> : IMessageEntityHandler<TContext, TResult>
  {
  }
}