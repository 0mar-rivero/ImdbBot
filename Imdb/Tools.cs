using System.Text.Json;

namespace Imdb;

public static class Tools {
	public static async Task<SearchResult> Search(string query) {
		var url = $"http://www.omdbapi.com/?s={query.Replace(' ', '+')}&apikey=37a2b719";
		var client = new HttpClient();
		try {
			return JsonSerializer.Deserialize<SearchResult>(await (await client.GetAsync(url)).Content.ReadAsStringAsync()) ??
			       new SearchResult(Array.Empty<SearchItem>(), "0", "False");
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return new SearchResult(Array.Empty<SearchItem>(), "0", "False");
		}
	}

	public static async Task<ItemInfo> GetInfo(string id) {
		var url = $"http://www.omdbapi.com/?i={id}&apikey=37a2b719";
		var client = new HttpClient();
		try {
			return JsonSerializer.Deserialize<ItemInfo>(await (await client.GetAsync(url)).Content.ReadAsStringAsync()) ??
			       ItemInfo.Empty;
		}
		catch (Exception e) {
			Console.WriteLine(e);
			return ItemInfo.Empty;
		}
	}

	public static async Task<ItemInfo> GetInfo(this SearchItem item) => await GetInfo(item.imdbID);

	public static string InfoText(ItemInfo info) =>
		$"{info.Title} • " + $"{info.Type}\n" +
		$"{info.Runtime} ⭐️ {info.imdbRating} IMBD\n\n" +
		$"Directors: {info.Director}\n" +
		$"Actors: {info.Actors}\n\n" +
		$"Genres: {info.Genre}\n" +
		$"{info.Plot}\n" +
		$"[🖼]({info.Poster})";
}