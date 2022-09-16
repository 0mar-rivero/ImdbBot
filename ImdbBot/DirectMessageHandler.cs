using Imdb;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static ImdbBot.Filters;
using static ImdbBot.Handlers;
using static Imdb.Tools;

namespace ImdbBot;

public delegate Task<bool> MessageFilter(Message message);

public delegate Task MessageHandler(ITelegramBotClient bot, Message message);

public static class TextMessageHandler {
	public static async Task BotOnMessageReceived(this ITelegramBotClient bot, Message message) {
		if (await IsSearch(message)) {
			await Search(bot, message);
		}
	}
}

public static class Filters {
	private static Task<bool> IsText(Message message) => Task.FromResult(message.Type is MessageType.Text);

	public static async Task<bool> IsSearch(Message message) =>
		await IsText(message) && message.Text is not null && message.Text.ToLower().StartsWith("/search ");
}

public static class Handlers {
	public static async Task Search(ITelegramBotClient bot, Message message) {
		if (message.Text is "/search ") {
			await bot.SendTextMessageAsync(message.Chat.Id, "Query is too short", replyToMessageId: message.MessageId);
			return;
		}

		var replyMessage =
			await bot.SendTextMessageAsync(message.Chat.Id, "Searching...", replyToMessageId: message.MessageId);
		var query = message.Text?[8..];
		try {
			if (query is not null) {
				var results = await Tools.Search(query);
				if (results.Search.Length is 0) {
					await bot.EditMessageTextAsync(message.Chat.Id, replyMessage.MessageId, "No results found.");
				}
				else {
					await bot.EditMessageTextAsync(message.Chat.Id.ToString(), replyMessage.MessageId, "Search results:",
						replyMarkup: new InlineKeyboardMarkup(SearchButtonsGenerator(results)));
				}
			}
		}
		catch (Exception e) {
			Console.WriteLine(DateTime.Now + "\n" + e);
			throw;
		}
	}

	private static IEnumerable<IEnumerable<InlineKeyboardButton>> SearchButtonsGenerator(SearchResult searchResult) =>
		searchResult.Search
			.Select(searchItem => new[] {
				InlineKeyboardButton.WithCallbackData($"{searchItem.Title} • {searchItem.Type} ({searchItem.Year})",
					$"{searchItem.imdbID}")
			});
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