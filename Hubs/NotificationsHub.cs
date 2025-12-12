using Microsoft.AspNetCore.SignalR;

namespace DemoApi.Hubs;

public class NotificationsHub : Hub
{
    // Această metodă este apelată de CLIENT (Frontend)
    public async Task SendMessage(string user, string message)
    {
        // Trimitem mesajul către TOȚI clienții conectați
        Console.WriteLine($"userul {user} a trimis mesajul {message}");
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    // Putem suprascrie ce se întâmplă când cineva se conectează
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier; // ID-ul din Tokenul JWT
        await base.OnConnectedAsync();
        Console.WriteLine($"Userul s-a conectat la WebSocket!");
    }
}