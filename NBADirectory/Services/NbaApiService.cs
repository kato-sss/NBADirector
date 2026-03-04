using System.Text.Json;
using NBADirectory.Models;

namespace NBADirectory.Services
{
    public interface INbaApiService
    {
        Task<List<Team>> GetAllTeamsAsync();
        Task<Team?> GetTeamAsync(int teamId);
        Task<List<Player>> GetPlayersByTeamAsync(int teamId);
    }

    public class NbaApiService : INbaApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NbaApiService> _logger;
        private const string BaseUrl = "https://api.balldontlie.io/v1";

        // API Key - In production, store in appsettings.json or environment variables
        private const string ApiKey = "050745e4-8652-4258-b7fe-50b6d91a48c2"; // BallDontLie API key (free tier available)

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public NbaApiService(HttpClient httpClient, ILogger<NbaApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            if (!string.IsNullOrEmpty(ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", ApiKey);
            }
        }

        public async Task<List<Team>> GetAllTeamsAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/teams?per_page=30");
                var result = JsonSerializer.Deserialize<ApiResponse<Team>>(response, _jsonOptions);
                return result?.Data ?? GetFallbackTeams();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch teams from API. Using fallback data.");
                return GetFallbackTeams();
            }
        }

        public async Task<Team?> GetTeamAsync(int teamId)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/teams/{teamId}");
                var result = JsonSerializer.Deserialize<TeamResponse>(response, _jsonOptions);
                return result?.Data;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch team {TeamId}. Using fallback data.", teamId);
                return GetFallbackTeams().FirstOrDefault(t => t.Id == teamId);
            }
        }

        public async Task<List<Player>> GetPlayersByTeamAsync(int teamId)
        {
            var players = new List<Player>();
            try
            {
                int page = 1;
                int totalPages = 1;

                do
                {
                    var url = $"{BaseUrl}/players?team_ids[]={teamId}&per_page=100&page={page}";
                    var response = await _httpClient.GetStringAsync(url);
                    var result = JsonSerializer.Deserialize<ApiResponse<Player>>(response, _jsonOptions);

                    if (result?.Data != null)
                    {
                        players.AddRange(result.Data);
                        totalPages = result.Meta?.Total_pages ?? 1;
                    }
                    page++;
                } while (page <= totalPages && totalPages > 1);

                return players.OrderBy(p => p.Last_name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch players for team {TeamId}. Using fallback data.", teamId);
                return GetFallbackPlayers(teamId);
            }
        }

        // Fallback static data when API is unavailable
        private static List<Team> GetFallbackTeams()
        {
            return new List<Team>
            {
                new() { Id = 1, Abbreviation = "ATL", City = "Atlanta", Conference = "East", Division = "Southeast", Full_name = "Atlanta Hawks", Name = "Hawks" },
                new() { Id = 2, Abbreviation = "BOS", City = "Boston", Conference = "East", Division = "Atlantic", Full_name = "Boston Celtics", Name = "Celtics" },
                new() { Id = 3, Abbreviation = "BKN", City = "Brooklyn", Conference = "East", Division = "Atlantic", Full_name = "Brooklyn Nets", Name = "Nets" },
                new() { Id = 4, Abbreviation = "CHA", City = "Charlotte", Conference = "East", Division = "Southeast", Full_name = "Charlotte Hornets", Name = "Hornets" },
                new() { Id = 5, Abbreviation = "CHI", City = "Chicago", Conference = "East", Division = "Central", Full_name = "Chicago Bulls", Name = "Bulls" },
                new() { Id = 6, Abbreviation = "CLE", City = "Cleveland", Conference = "East", Division = "Central", Full_name = "Cleveland Cavaliers", Name = "Cavaliers" },
                new() { Id = 7, Abbreviation = "DAL", City = "Dallas", Conference = "West", Division = "Southwest", Full_name = "Dallas Mavericks", Name = "Mavericks" },
                new() { Id = 8, Abbreviation = "DEN", City = "Denver", Conference = "West", Division = "Northwest", Full_name = "Denver Nuggets", Name = "Nuggets" },
                new() { Id = 9, Abbreviation = "DET", City = "Detroit", Conference = "East", Division = "Central", Full_name = "Detroit Pistons", Name = "Pistons" },
                new() { Id = 10, Abbreviation = "GSW", City = "Golden State", Conference = "West", Division = "Pacific", Full_name = "Golden State Warriors", Name = "Warriors" },
                new() { Id = 11, Abbreviation = "HOU", City = "Houston", Conference = "West", Division = "Southwest", Full_name = "Houston Rockets", Name = "Rockets" },
                new() { Id = 12, Abbreviation = "IND", City = "Indiana", Conference = "East", Division = "Central", Full_name = "Indiana Pacers", Name = "Pacers" },
                new() { Id = 13, Abbreviation = "LAC", City = "Los Angeles", Conference = "West", Division = "Pacific", Full_name = "Los Angeles Clippers", Name = "Clippers" },
                new() { Id = 14, Abbreviation = "LAL", City = "Los Angeles", Conference = "West", Division = "Pacific", Full_name = "Los Angeles Lakers", Name = "Lakers" },
                new() { Id = 15, Abbreviation = "MEM", City = "Memphis", Conference = "West", Division = "Southwest", Full_name = "Memphis Grizzlies", Name = "Grizzlies" },
                new() { Id = 16, Abbreviation = "MIA", City = "Miami", Conference = "East", Division = "Southeast", Full_name = "Miami Heat", Name = "Heat" },
                new() { Id = 17, Abbreviation = "MIL", City = "Milwaukee", Conference = "East", Division = "Central", Full_name = "Milwaukee Bucks", Name = "Bucks" },
                new() { Id = 18, Abbreviation = "MIN", City = "Minnesota", Conference = "West", Division = "Northwest", Full_name = "Minnesota Timberwolves", Name = "Timberwolves" },
                new() { Id = 19, Abbreviation = "NOP", City = "New Orleans", Conference = "West", Division = "Southwest", Full_name = "New Orleans Pelicans", Name = "Pelicans" },
                new() { Id = 20, Abbreviation = "NYK", City = "New York", Conference = "East", Division = "Atlantic", Full_name = "New York Knicks", Name = "Knicks" },
                new() { Id = 21, Abbreviation = "OKC", City = "Oklahoma City", Conference = "West", Division = "Northwest", Full_name = "Oklahoma City Thunder", Name = "Thunder" },
                new() { Id = 22, Abbreviation = "ORL", City = "Orlando", Conference = "East", Division = "Southeast", Full_name = "Orlando Magic", Name = "Magic" },
                new() { Id = 23, Abbreviation = "PHI", City = "Philadelphia", Conference = "East", Division = "Atlantic", Full_name = "Philadelphia 76ers", Name = "76ers" },
                new() { Id = 24, Abbreviation = "PHX", City = "Phoenix", Conference = "West", Division = "Pacific", Full_name = "Phoenix Suns", Name = "Suns" },
                new() { Id = 25, Abbreviation = "POR", City = "Portland", Conference = "West", Division = "Northwest", Full_name = "Portland Trail Blazers", Name = "Trail Blazers" },
                new() { Id = 26, Abbreviation = "SAC", City = "Sacramento", Conference = "West", Division = "Pacific", Full_name = "Sacramento Kings", Name = "Kings" },
                new() { Id = 27, Abbreviation = "SAS", City = "San Antonio", Conference = "West", Division = "Southwest", Full_name = "San Antonio Spurs", Name = "Spurs" },
                new() { Id = 28, Abbreviation = "TOR", City = "Toronto", Conference = "East", Division = "Atlantic", Full_name = "Toronto Raptors", Name = "Raptors" },
                new() { Id = 29, Abbreviation = "UTA", City = "Utah", Conference = "West", Division = "Northwest", Full_name = "Utah Jazz", Name = "Jazz" },
                new() { Id = 30, Abbreviation = "WAS", City = "Washington", Conference = "East", Division = "Southeast", Full_name = "Washington Wizards", Name = "Wizards" },
            };
        }

        private static List<Player> GetFallbackPlayers(int teamId)
        {
            // Sample fallback data for Lakers (14) and Celtics (2)
            var allFallback = new Dictionary<int, List<Player>>
            {
                [14] = new List<Player>
                {
                    new() { Id = 1, First_name = "LeBron", Last_name = "James", Position = "F", Height = "6-9", Weight = "250", Jersey_number = "23", Country = "USA" },
                    new() { Id = 2, First_name = "Anthony", Last_name = "Davis", Position = "F-C", Height = "6-10", Weight = "253", Jersey_number = "3", Country = "USA" },
                    new() { Id = 3, First_name = "Austin", Last_name = "Reaves", Position = "G", Height = "6-5", Weight = "206", Jersey_number = "15", Country = "USA" },
                    new() { Id = 4, First_name = "D'Angelo", Last_name = "Russell", Position = "G", Height = "6-4", Weight = "193", Jersey_number = "1", Country = "USA" },
                    new() { Id = 5, First_name = "Rui", Last_name = "Hachimura", Position = "F", Height = "6-8", Weight = "230", Jersey_number = "28", Country = "Japan" },
                },
                [2] = new List<Player>
                {
                    new() { Id = 10, First_name = "Jayson", Last_name = "Tatum", Position = "F", Height = "6-8", Weight = "210", Jersey_number = "0", Country = "USA" },
                    new() { Id = 11, First_name = "Jaylen", Last_name = "Brown", Position = "G-F", Height = "6-6", Weight = "223", Jersey_number = "7", Country = "USA" },
                    new() { Id = 12, First_name = "Kristaps", Last_name = "Porzingis", Position = "C", Height = "7-2", Weight = "240", Jersey_number = "8", Country = "Latvia" },
                    new() { Id = 13, First_name = "Jrue", Last_name = "Holiday", Position = "G", Height = "6-4", Weight = "205", Jersey_number = "4", Country = "USA" },
                    new() { Id = 14, First_name = "Al", Last_name = "Horford", Position = "F-C", Height = "6-9", Weight = "240", Jersey_number = "42", Country = "Dominican Republic" },
                },
            };

            if (allFallback.TryGetValue(teamId, out var players))
            {
                return players;
            }

            // Generic placeholder for other teams
            return new List<Player>
            {
                new() { Id = 99, First_name = "Sample", Last_name = "Player", Position = "G", Height = "6-3", Weight = "200", Jersey_number = "00", Country = "USA", College = "State University" }
            };
        }
    }

    public class TeamResponse
    {
        public Team? Data { get; set; }
    }
}
