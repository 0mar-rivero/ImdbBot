using Imdb;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using static Imdb.Tools;

namespace ImdbBot;

internal static class InLineHandling {
	private static Dictionary<(long UserId, string MessageId), bool> _alreadyEdited = new();

	public static async Task BotOnInLineQueryReceived(this ITelegramBotClient bot, InlineQuery inlineQuery) {
		if (inlineQuery.Query.Length < 2) return;
		var searchResults = (await Search(inlineQuery.Query)).Search;
		var results = searchResults.Select(item =>
			new InlineQueryResultArticle($"{item.imdbID}", item.Title, new InputTextMessageContent("...searching...")) {
				Description = $"{item.Type} ({item.Year})", ThumbUrl = item.Poster,
				ReplyMarkup = InlineKeyboardButton.WithUrl("IMDB", "https://www.imdb.com/title/" + item.imdbID)
			}).Cast<InlineQueryResult>().ToList();

		await bot.AnswerInlineQueryAsync(inlineQuery.Id, results);
	}

	public static async Task BotOnChosenInlineResultReceived(this ITelegramBotClient botClient,
		ChosenInlineResult chosenInlineResult) {
		try {
			if (chosenInlineResult.InlineMessageId is null ||
			    _alreadyEdited.ContainsKey((chosenInlineResult.From.Id, chosenInlineResult.InlineMessageId))) return;
			_alreadyEdited[(chosenInlineResult.From.Id, chosenInlineResult.InlineMessageId)] = true;
			var info = await GetInfo(chosenInlineResult.ResultId);

			await botClient.EditMessageCaptionAsync(chosenInlineResult.InlineMessageId,
				InfoText(info), ParseMode.Markdown,
				replyMarkup: InlineKeyboardButton.WithUrl("IMDB",
					"https://www.imdb.com/title/" + chosenInlineResult.ResultId));
		}

		catch (Exception e) {
			Console.WriteLine(e);
		}
	}
}