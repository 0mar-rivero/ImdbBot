using System.Security.Authentication;
using Imdb;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;
using static Imdb.Tools;

namespace ImdbBot;

static class InLineHandling {
	public static async Task BotOnInLineQueryReceived(this ITelegramBotClient bot, InlineQuery inlineQuery) {
		if (inlineQuery.Query.Length < 2) return;
		var results = new List<InlineQueryResult>();
		var searchResults = (await Tools.Search(inlineQuery.Query)).Search ?? Array.Empty<SearchItem>();
		foreach (var item in searchResults) {
			results.Add(new InlineQueryResultArticle($"{item.imdbID}", item.Title,
				new InputTextMessageContent(await VideoMessageGenerator(item))) {
				Description = $"{item.Type} ({item.Year})",
				ThumbUrl = item.Poster,
				ReplyMarkup = InlineKeyboardButton.WithUrl("IMDB","https://www.imdb.com/title/"+item.imdbID)
			});
		}

		await bot.AnswerInlineQueryAsync(inlineQuery.Id, results);
	}

	public static async Task BotOnChosenInlineResultReceived(this ITelegramBotClient botClient,
		ChosenInlineResult chosenInlineResult) {
		if (chosenInlineResult.InlineMessageId is not null)
			await botClient.EditMessageCaptionAsync(chosenInlineResult.InlineMessageId, await VideoMessageGenerator(chosenInlineResult.ResultId));
	}

	private static async Task<string> VideoMessageGenerator(string imdbID) {
		var info = await GetInfo(imdbID);
		return $"{info.Title}•{info.Type}\n" +
		       $"{info.Runtime} ⭐️ {info.imdbRating} IMBD\n\n" +
		       $"Directors: {info.Director}\n" +
		       $"Actors: {info.Actors}\n\n" +
		       $"Genres: {info.Genre}\n" +
		       $"{info.Plot}";
	}

	private static async Task<string> VideoMessageGenerator(SearchItem item) => await VideoMessageGenerator(item.imdbID);
}