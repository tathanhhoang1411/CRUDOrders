namespace CRUDOrders.Application.DTOs;

public class Result<T>
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public T? Data { get; set; }

    public static Result<T> Ok(T data)
    {
        return new Result<T>
        {
            Success = true,
            Data = data
        };
    }

    public static Result<T> Error(string errorCode, string errorMessage)
    {
        return new Result<T>
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            Data = default
        };
    }
}
