using System.Security.AccessControl;
using Imdb;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static ImdbBot.Filters;
using static ImdbBot.Handlers;
using static Imdb.Tools;

namespace ImdbBot;

public delegate Task<bool> MessageFilter(ITelegramBotClient bot, Message message);

public delegate Task MessageHandler(ITelegramBotClient bot, Message message);

public static class TextMessageHandler {
	private static readonly Dictionary<MessageFilter, List<MessageHandler>> Handlers = new();

	public static void LoadHandlers(ITelegramBotClient bot) {
		AddHandler(Command(bot,"search"), Search);
		AddHandler(Command(bot,"start"), Start);
	}

	private static void AddHandler(MessageFilter filter, MessageHandler handler) {
		if (!Handlers.ContainsKey(filter)) Handlers.Add(filter, new List<MessageHandler>());
		Handlers[filter].Add(handler);
	}

	public static async Task BotOnMessageReceived(this ITelegramBotClient bot, Message message) {
		foreach (var (filter, handlers) in Handlers) {
			if (!await filter(bot, message)) continue;
			foreach (var handler in handlers) {
				await handler(bot, message);
			}
		}
	}
}

internal static class Filters {
	private static Task<bool> IsPrivate(ITelegramBotClient bot, Message message) =>
		Task.FromResult(message.Chat.Type is ChatType.Private);

	public static MessageFilter And(this MessageFilter filter1, MessageFilter filter2) => new MessageFilter(
		(bot, message) => Task.FromResult(filter1(bot, message).Result && filter2(bot, message).Result));

	public static MessageFilter Or(this MessageFilter filter1, MessageFilter filter2) => new MessageFilter(
		(bot, message) => Task.FromResult(filter1(bot, message).Result || filter2(bot, message).Result));

	public static MessageFilter Not(this MessageFilter filter) =>
		new MessageFilter((bot, message) => Task.FromResult(!filter(bot, message).Result));

	public static MessageFilter Command(ITelegramBotClient bot, string command) =>
		StartsWith($"/{command}@{bot.GetMeAsync().Result.Username}").Or(StartsWith($"/{command}").And(IsPrivate));

	public static Task<bool> IsTextMessage(ITelegramBotClient bot, Message message) =>
		Task.FromResult(message.Type is MessageType.Text && message.Text is not null);

	public static MessageFilter StartsWith(string text) => And(IsTextMessage, (_, message) => Task.FromResult(message.Text!.StartsWith(text)));
}

public static class Handlers {
	public static async Task Search(ITelegramBotClient bot, Message message) {
		Console.WriteLine("aqui llego	");
		var username = (await bot.GetMeAsync()).Username;
		var isPrivate = message.Chat.Type is ChatType.Private;
		const string searchCommand = "/search";
		var groupSearchCommand = $"/search@{username}";
		if ((isPrivate && message.Text == searchCommand) || message.Text == groupSearchCommand) {
			await bot.SendTextMessageAsync(message.Chat.Id,
				$"Type query after {(isPrivate ? searchCommand : groupSearchCommand)}",
				replyToMessageId: message.MessageId);
			return;
		}

		var query = message.Text?.Remove(0,
			message.Text.StartsWith(groupSearchCommand) ? groupSearchCommand.Length : searchCommand.Length);
		if (query is null || !query.StartsWith(" ")) return;
		query = query.Trim();
		var replyMessage =
			await bot.SendTextMessageAsync(message.Chat.Id, "Searching...", replyToMessageId: message.MessageId);

		try {
			var results = await Tools.Search(query);
			if (results.Search is null) {
				await bot.EditMessageTextAsync(message.Chat.Id, replyMessage.MessageId, "No results found.");
			}
			else {
				await bot.EditMessageTextAsync(message.Chat.Id.ToString(), replyMessage.MessageId, "Search results:",
					replyMarkup: new InlineKeyboardMarkup(SearchButtonsGenerator(results)));
			}
		}
		catch (Exception e) {
			Console.WriteLine(DateTime.Now + "\n" + e);
			throw;
		}
	}

	public static async Task Start(ITelegramBotClient bot, Message message) {
		await bot.SendTextMessageAsync(message.Chat.Id, "Welcome to IMDB Bot!", replyToMessageId:message.MessageId);
	}
	private static IEnumerable<IEnumerable<InlineKeyboardButton>> SearchButtonsGenerator(SearchResult searchResult) =>
		searchResult.Search?
			.Select(searchItem => new[] {
				InlineKeyboardButton.WithCallbackData($"{searchItem.Title} • {searchItem.Type} ({searchItem.Year})",
					$"{searchItem.imdbID}")
			}) ?? Enumerable.Empty<IEnumerable<InlineKeyboardButton>>();
}

public static class CallbackQueryHandler {
	public static async Task BotOnCallbackQueryReceived(this ITelegramBotClient bot, CallbackQuery callbackQuery) {
		await SearchInfo(bot, callbackQuery);
	}

	private static async Task SearchInfo(ITelegramBotClient bot, CallbackQuery callbackQuery) {
		if (callbackQuery.Message is not null)
			await bot.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, "Loading...");
		if (callbackQuery.Data is not null) {
			var info = await GetInfo(callbackQuery.Data);
			var text = InfoText(info);
			if (callbackQuery.Message is not null)
				await bot.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, text,
					parseMode: ParseMode.Markdown,
					replyMarkup: new InlineKeyboardMarkup(
						InlineKeyboardButton.WithUrl("IMDb", $"https://www.imdb.com/title/{info.imdbID}/")
					));
		}
	}
}