using System.Runtime.CompilerServices;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace NoNFT_Bot;

internal static class Program
{
    private const string NANI_SORE =
        """
        😈 I will delete NFT spam in your group. Just give me permission to delete messages 😎👌

        <u><b>NTF spam be like</b></u>:
        😮 user from outside
        😳 sussy text!
        😳 sussy URL!
        😎 other people say it's NFT spam
        """;

    #region BOILER

    // MAIN

    public static void Main(string[] args)
    {
        try
        {
            var bot = new TelegramBotClient(File.ReadAllText("token").Trim());
            LogEnter(bot);

            var options = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.EditedMessage, UpdateType.CallbackQuery]
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
        var me = bot.GetMe().Result;

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

    // POLLING

    private static Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken token)
    {
        return update switch
        {
            { Message:       { } message  } => OnMessage (message,  bot),
            { EditedMessage: { } message  } => OnMessage (message,  bot),
            { CallbackQuery: { } callback } => OnCallback(callback, bot),
            _ => Task.CompletedTask
        };
    }

    private static async Task OnMessage(Message message, ITelegramBotClient bot)
    {
        try
        {
            var text = message.Text;
            if (text != null && text.StartsWith("/"))
            {
                if (text.StartsWith("/nani_sore"))
                {
                    await bot.SendMessage(message.Chat.Id, NANI_SORE, ParseMode.Html);
                }
            }

            await OnMessageInternal(message, bot);
        }
        catch (Exception exception)
        {
            var (chat, title) = GetChatAndTitle(message);
            Log(chat, title, $"BRUH -> {exception.Message}", ConsoleColor.Red);
        }
    }

    private static Task HandlePollingError(ITelegramBotClient bot, Exception exception, CancellationToken token)
    {
        Log($"Telegram API Error x_x --> {exception.Message}", ConsoleColor.Red);
        return Task.CompletedTask;
    }

    // LOGS

    private static (long, string) GetChatAndTitle(Message m) => (m.Chat.Id, m.Chat.Title ?? string.Empty);

    [MethodImpl(MethodImplOptions.Synchronized)]
    private static void Log(string message, ConsoleColor color)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{DateTime.Now:MM'/'dd' 'HH:mm:ss.fff}]\n\t");
        Console.ForegroundColor = color;
        Console.WriteLine(message);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private static void Log(long chat, string title, string message, ConsoleColor color)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{DateTime.Now:MM'/'dd' 'HH:mm:ss.fff}] - [{chat} / {title}]\n\t");
        Console.ForegroundColor = color;
        Console.WriteLine(message);
    }

    #endregion

    // DATA

    private const string IS_THAT_NFT_SPAM = "AgACAgIAAyEFAASSkTL6AAMKZ1ckuJR4buJzFwKx23WvMR-uxIIAAhXlMRtXsrhKVnGvavMiFMwBAAMCAAN5AAM2BA";

    private record DeleteRequest(long Chat, string Title, User User, int MessageSpam, int MessageQuestion);

    private static readonly Dictionary<Guid, DeleteRequest> _requests = new();

    private static readonly List<string> _keywordsUrl  = [".io/", "opensea", "fluff", "drop"];

    private static readonly List<string> _keywordsText =
    [
        "\u200b", "\u200c", "\u200d",
        "\u2060", "\u2061", "\u2062",
        "\u2063", "\u2064", "\u2068",
        "NFT", "claim", "ASAP", "hurry", "degens"
    ];

    // LOGIC

    private static async Task OnMessageInternal(Message message, ITelegramBotClient bot)
    {
        // SKIP technical messages

        var user = message.From;
        if (user is null) return;

        // SKIP empty messages

        var text = message.Caption ?? message.Text;
        if (text is null) return;

        if (message.GetURLs() is { } urls && (text.HasSussyURL(urls) || text.TextIsSussy()))
        {
            var (chat, title) = GetChatAndTitle(message);

            // ANALYZE FURTHER

            Log(chat, title, $"{message.Id} <- SUSSY MESSAGE", ConsoleColor.Gray);

            if (await user.IsGoodGuy(chat, bot)) return;

            Log(chat, title, $"{message.Id} <- MESSAGE FROM THE IMPOSTER", ConsoleColor.Gray);

            // PROMPT USER(s) FOR ACTION

            var guid = Guid.NewGuid();

            Log(chat, title, $"{message.Id} >> {text}", ConsoleColor.Blue);
            Log(chat, title, $"{message.Id} >> added to QUARANTINE", ConsoleColor.Yellow);

            var picture = InputFile.FromFileId(IS_THAT_NFT_SPAM);
            var replyTo = new ReplyParameters() { MessageId = message.Id };

            var y = InlineKeyboardButton.WithCallbackData("😎 YES (DELETE)", $"Y {guid}");
            var n = InlineKeyboardButton.WithCallbackData("😳 NO",           $"N {guid}");
            var buttons = new InlineKeyboardMarkup().AddButtons(y, n);

            var question = await bot.SendPhoto(chat, picture, replyParameters: replyTo, replyMarkup: buttons);

            _requests.Add(guid, new DeleteRequest(chat, title, user, message.Id, question.Id));

            WaitAndDeleteSpam(bot, guid);
        }
    }

    private static async void WaitAndDeleteSpam(ITelegramBotClient bot, Guid guid)
    {
        await Task.Delay(30_000);

        try // async void moment
        {
            if (_requests.Remove(guid, out var request))
            {
                var (chat, title) = (request.Chat, request.Title);

                Log(chat, title, $"{request.MessageSpam} >> DELETING NFT SPAM! (30s passed)", ConsoleColor.Magenta);

                await bot.DeleteMessage(request.Chat, request.MessageQuestion);
                await bot.DeleteMessage(request.Chat, request.MessageSpam);
            }
        }
        catch (Exception e)
        {
            Log(e.ToString(), ConsoleColor.Red);
        }
    }

    private static async Task OnCallback(CallbackQuery callback, ITelegramBotClient bot)
    {
        var data = callback.Data;
        if (data is null) return;

        var args = data.Split(' ');
        var guid = Guid.Parse(args[1]);

        if (_requests.TryGetValue(guid, out var request) == false) return;

        var (chat, title) = (request.Chat, request.Title);

        var user = callback.From;
        var name = user.FirstName;

        var userWillMatters = user.Id == request.User.Id || await user.CanBeatGoku(chat, bot);
        if (userWillMatters == false)
        {
            Log(chat, title, $"{request.MessageSpam} >> VOTE [{args[0]}] FROM {name} (unaccepted)", ConsoleColor.DarkGray);
            return;
        }

        // CHECKS PASSED

        _requests.Remove(guid);

        await bot.DeleteMessage(request.Chat, request.MessageQuestion);

        if (data.StartsWith("Y"))
        {
            Log(chat, title, $"{request.MessageSpam} >> DELETING NFT SPAM! (marked as spam by {name})", ConsoleColor.Magenta);

            await bot.DeleteMessage(request.Chat, request.MessageSpam);
        }
        else // "N"
        {
            Log(chat, title, $"{request.MessageSpam} >> MESSAGE KEPT! (marked as not spam by {name})", ConsoleColor.Green);
        }
    }

    // KOWALSKI ANALYSIS

    private static IEnumerable<MessageEntity> GetURLs(this Message message)
    {
        var entities = message.Entities;
        if (entities is null) return [];

        return entities.Where(x => x.Type is MessageEntityType.Url or MessageEntityType.TextLink);
    }

    private static bool TextIsSussy(this string text) => _keywordsText.Any(text.Contains);

    private static bool HasSussyURL(this string text, IEnumerable<MessageEntity> entities)
    {
        foreach (var entity in entities)
        {
            var url = entity.Type switch
            {
                MessageEntityType.Url => text.Substring(entity.Offset, entity.Length),
                MessageEntityType.TextLink => entity.Url!,
                _ => null
            };

            if (url is null) continue;

            if (_keywordsUrl.Any(x => url.Contains(x))) return true;
        }

        return false;
    }

    private static async Task<bool> IsGoodGuy(this User user, long chat, ITelegramBotClient bot)
    {
        if (user is { IsBot: true, Username: "Channel_Bot" or "GroupAnonymousBot" }) return true;

        var    member = await bot.GetChatMember(chat, user.Id);
        return member.Status is ChatMemberStatus.Member or ChatMemberStatus.Administrator or ChatMemberStatus.Creator;
    }

    private static async Task<bool> CanBeatGoku(this User user, long chat, ITelegramBotClient bot)
    {
        if (user is { IsBot: true, Username: "Channel_Bot" or "GroupAnonymousBot" }) return true;

        var    member = await bot.GetChatMember(chat, user.Id);
        return member.Status is ChatMemberStatus.Administrator or ChatMemberStatus.Creator;
    }
}