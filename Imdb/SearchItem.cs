namespace Imdb;

public record SearchItem(string Title, string Year, string imdbID, string Type, string Poster);

public record SearchResult(SearchItem[]? Search, string totalResults, string Response);

public record ItemInfo(string Title, string Year, string Rated, string Released, string Runtime, string Genre,
	string Director, string Writer, string Actors, string Plot, string Language, string Country, string Awards,
	string Poster, Rating[] Ratings, string Metascore, string imdbRating, string imdbVotes, string imdbID, string Type,
	string DVD, string BoxOffice, string Production, string Website, string Response) {

	public static ItemInfo Empty => new("", "", "", "", "", "", "", "", "", "", 
		"", "", "", "", Array.Empty<Rating>(), "", "", "",
		"", "", "", "", "", "", "");
}

public record Rating(string Source, string Value);