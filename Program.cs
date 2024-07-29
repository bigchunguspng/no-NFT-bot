using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace NoNFT_Bot;

internal static class Program
{
    private static readonly Regex NFT_Spam
        = new(@"(nft|claim|hurry|degens|быстрее|заходите)(\S*\s*)*https?:\/\/", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static void Main(string[] args)
    {
        try
        {
            var bot = new TelegramBotClient(File.ReadAllText("token").Trim());

            LogEnter(bot);

            var options = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.EditedMessage]
            };

            bot.StartReceiving(HandleUpdate, HandlePollingError, options);

            WaitForExit();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"MAIN LOOP >> MEGA BRUH -> {e.Message}");
            Console.ReadKey();
        }
    }

    private static void LogEnter(TelegramBotClient bot)
    {
        var me = bot.GetMeAsync().Result;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"MAIN LOOP >> ENTERING AS [{me.FirstName}] / @{me.Username}");
    }

    private static void WaitForExit()
    {
        ConsoleKeyInfo input = default;
        while (input.Key != ConsoleKey.Q)
        {
            Console.ResetColor();
            input = Console.ReadKey();
        }
        
    }

    private static Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken token)
    {
        return update switch
        {
            { Message:       { } message } => OnMessage(message, bot),
            { EditedMessage: { } message } => OnMessage(message, bot),
        };
    }

    private static async Task OnMessage(Message message, ITelegramBotClient bot)
    {
        try
        {
            await OnMessageInternal(message, bot);
        }
        catch (Exception exception)
        {
            var chat = message.Chat.Id;
            var title = message.Chat.Title ?? string.Empty;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{chat} / {title} >> BRUH -> {exception.Message}");
        }
    }

    private static async Task OnMessageInternal(Message message, ITelegramBotClient bot)
    {
        if (message.From is null) return;

        var text = message.Caption ?? message.Text;
        if (text is null) return;

        if (NFT_Spam.IsMatch(text))
        {
            var chat = message.Chat.Id;
            var title = message.Chat.Title ?? string.Empty;

            Console.ResetColor();
            Console.WriteLine($"{chat} / {title} >> NFT SPAM DETECTED");

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{chat} / {title} >> {text}");

            var member = await bot.GetChatMemberAsync(message.Chat.Id, message.From.Id);
            if (member.Status is not ChatMemberStatus.Administrator and not ChatMemberStatus.Member)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{chat} / {title} >> DELETING NFT SPAM!"); ;

                await bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
        }
    }

    private static Task HandlePollingError(ITelegramBotClient bot, Exception exception, CancellationToken token)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Telegram API Error x_x --> {exception.Message}");

        return Task.CompletedTask;
    }
}