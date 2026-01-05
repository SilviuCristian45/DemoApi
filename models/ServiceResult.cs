namespace DemoApi.Models;

public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T Data { get; set; } // De ex: ID-ul comenzii
    public string ErrorMessage { get; set; }

    // Constructor privat ca să forțăm folosirea metodelor statice de mai jos
    private ServiceResult() { }

    // Metoda de succes (Factory Method)
    public static ServiceResult<T> Ok(T data)
    {
        return new ServiceResult<T> 
        { 
            Success = true, 
            Data = data,
            ErrorMessage = null
        };
    }

    // Metoda de eșec (Factory Method)
    public static ServiceResult<T> Fail(string message)
    {
        return new ServiceResult<T> 
        { 
            Success = false, 
            Data = default, 
            ErrorMessage = message 
        };
    }
}