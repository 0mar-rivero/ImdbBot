using ImdbBot;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botToken = "";//change this ;)

if (botToken == "") {
	Console.WriteLine("Please set your bot token in the source code!");
	Environment.Exit(0);
}

var botClient = new TelegramBotClient(botToken);
using var cts = new CancellationTokenSource();
var me = await botClient.GetMeAsync();
var receiverOptions = new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() };

botClient.StartReceiving(
	updateHandler: HandleUpdateAsync,
	pollingErrorHandler: HandlePollingErrorAsync,
	receiverOptions: receiverOptions,
	cancellationToken: cts.Token
);
TextMessageHandler.LoadHandlers(botClient);

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken) {
	await (update.Type switch {
		UpdateType.Message => bot.BotOnMessageReceived(update.Message!),
		UpdateType.InlineQuery => bot.BotOnInLineQueryReceived(update.InlineQuery!),
		UpdateType.ChosenInlineResult => bot.BotOnChosenInlineResultReceived(update.ChosenInlineResult!),
		UpdateType.CallbackQuery => bot.BotOnCallbackQueryReceived(update.CallbackQuery!),
		_ => Task.CompletedTask
	});
}

Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken) {
	var errorMessage = exception switch {
		ApiRequestException apiRequestException
			=> $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
		_ => exception.ToString()
	};

	Console.WriteLine(errorMessage);
	return Task.FromResult(Task.CompletedTask);
}

Console.WriteLine(
	$"I'm alive my name is {me.FirstName} and my username is {me.Username}. I'm listening for updates...");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();