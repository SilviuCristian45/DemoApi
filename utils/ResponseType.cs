using System.Text.Json.Serialization;

// JsonStringEnumConverter face ca în JSON să apară "Success" în loc de 0, "Warn" în loc de 1.
[JsonConverter(typeof(JsonStringEnumConverter))] 
public enum ResponseType
{
    Success,
    Warn,
    Error
}