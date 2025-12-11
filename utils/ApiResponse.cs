namespace DemoApi.Utils;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string Message { get; set; }
    public ResponseType Type { get; set; }

    // Constructor simplu
    public ApiResponse(T? data, string message, ResponseType type)
    {
        Data = data;
        Message = message;
        Type = type;
    }

    // --- HELPER METHODS (Syntax Sugar) ---
    // Astea te ajută să scrii mai puțin cod în Controller
    
    public static ApiResponse<T> Success(T data, string message = "Operațiune reușită")
    {
        return new ApiResponse<T>(data, message, ResponseType.Success);
    }

    public static ApiResponse<T> Error(string message)
    {
        // default(T) pune null pentru obiecte sau 0 pentru numere
        return new ApiResponse<T>(default, message, ResponseType.Error);
    }
    
    public static ApiResponse<T> Warn(T data, string message)
    {
        return new ApiResponse<T>(data, message, ResponseType.Warn);
    }
}