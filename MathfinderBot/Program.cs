using Discord;
using Discord.Commands;
using Discord.WebSocket;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();
    
    
    private static  CommandService      commandService => new CommandService();
    private static  DiscordSocketClient client => new DiscordSocketClient();

    public LoggingService logger;
    
    
    public async Task MainAsync()
    {
        logger = new LoggingService(client, commandService);
        

        var token = "MTAwMzg0NDYyODg0MTIzODU4OA.G7kN_9.Q5WgQp222LF52A5_uge958ElOzePLOtNq6TOzo";

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();
        await Task.Delay(-1);
    }

   
}