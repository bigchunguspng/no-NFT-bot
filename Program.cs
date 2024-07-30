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
        = new(@"(nft|claim|hurry|degens|быстрее|заходите|топ|бабки).*\S\.\S", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            Log($"MAIN LOOP >> MEGA BRUH -> {e.Message}", ConsoleColor.Red);
            Console.ReadKey();
        }
    }

    private static void LogEnter(TelegramBotClient bot)
    {
        var me = bot.GetMeAsync().Result;

        Log($"MAIN LOOP >> ENTERING AS [{me.FirstName}] / @{me.Username}", ConsoleColor.Yellow);
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
            var (chat, title) = GetChatAndTitle(message);
            Log($"{chat} / {title} >> BRUH -> {exception.Message}", ConsoleColor.Red);
        }
    }

    private static async Task OnMessageInternal(Message message, ITelegramBotClient bot)
    {
        if (message.From is null) return;

        var text = message.Caption ?? message.Text;
        if (text is null) return;

        if (NFT_Spam.IsMatch(text))
        {
            var (chat, title) = GetChatAndTitle(message);

            Log($"{chat} / {title} >> NFT SPAM DETECTED", ConsoleColor.Gray);
            Log($"{chat} / {title} >> {text}", ConsoleColor.Blue);

            var member = await bot.GetChatMemberAsync(message.Chat.Id, message.From.Id);
            if (member.Status is not ChatMemberStatus.Administrator and not ChatMemberStatus.Member)
            {
                Log($"{chat} / {title} >> DELETING NFT SPAM!", ConsoleColor.Yellow);
                await bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
        }
    }

    private static Task HandlePollingError(ITelegramBotClient bot, Exception exception, CancellationToken token)
    {
        Log($"Telegram API Error x_x --> {exception.Message}", ConsoleColor.Red);
        return Task.CompletedTask;
    }

    private static (long, string) GetChatAndTitle(Message m) => (m.Chat.Id, m.Chat.Title ?? string.Empty);

    private static void Log(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
    }
}