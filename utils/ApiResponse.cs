namespace DemoApi.Utils;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public List<string> Message { get; set; }
    public ResponseType Type { get; set; }

    // Constructor simplu
    public ApiResponse(T? data, string message, ResponseType type)
    {
        Data = data;
        Message = new List<string>();
        Message.Add(message);
        Type = type;
    }

    public ApiResponse() { Message = new List<string>(); }

    public static ApiResponse<T> Success(T data, string message = "Operațiune reușită")
    {
        return new ApiResponse<T>(data, message, ResponseType.Success);
    }

    public static ApiResponse<T> Success(T data, List<string> messages)
    {
        return new ApiResponse<T>(data, messages.ElementAt(0), ResponseType.Success);
    }

    public static ApiResponse<T>? Error(List<string>? errors = null)
    {
        return new ApiResponse<T>
        { 
            Data = default(T),
            Type = ResponseType.Error, 
            Message = new List<string>(errors), 
        };
    }

     public static ApiResponse<T>? Error(string error)
    {
        return new ApiResponse<T> 
        { 
            Data = default(T),
            Type = ResponseType.Error, 
            Message = new List<string>() { error }, 
        };
    }
    
    public static ApiResponse<T> Warn(T data, string message)
    {
        return new ApiResponse<T>(data, message, ResponseType.Warn);
    }
}