using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace Team23.TelegramSkeleton
{
  public class ExceptionParser : IExceptionParser
  {
    public static readonly ExceptionParser Instance = new();
    
    public ApiRequestException Parse(ApiResponse apiResponse)
    {
      switch (apiResponse.ErrorCode)
      {
        case 400 when apiResponse.Description == "Bad Request: query is too old and response timeout expired or query ID is invalid":
          return  new ApiRequestTimeoutException(apiResponse.Description, apiResponse.ErrorCode, apiResponse.Parameters);

        case 400 when apiResponse.Description == "Bad Request: MESSAGE_ID_INVALID":
          return  new ApiRequestNotFoundException(apiResponse.Description, apiResponse.ErrorCode, apiResponse.Parameters);

        default:
          return new(apiResponse.Description, apiResponse.ErrorCode, apiResponse.Parameters);
      }
    }
  }
  
  public class ApiRequestTimeoutException : ApiRequestException
  {
    public ApiRequestTimeoutException(string message, int errorCode, ResponseParameters? parameters = null) 
      : base(message, errorCode, parameters) { }
  }

  public class ApiRequestNotFoundException : ApiRequestException
  {
    public ApiRequestNotFoundException(string message, int errorCode, ResponseParameters? parameters = null) 
      : base(message, errorCode, parameters) { }
  }
}