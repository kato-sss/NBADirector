using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using NBADirectory.Models;

namespace NBADirectory.Services
{
    public interface INbaApiService
    {
        Task<List<Team>> GetAllTeamsAsync();
        Task<Team?> GetTeamAsync(int teamId);
        Task<List<Player>> GetPlayersByTeamAsync(int teamId);
        Task<List<SeasonAverage>> GetSeasonAveragesAsync(List<int> playerIds, int season);
        Task<List<Game>> GetGamesByDateAsync(string date);
        Task<Game?> GetGameAsync(int gameId);
        Task<List<GameStat>> GetGameStatsAsync(int gameId);
    }

    public class NbaApiService : INbaApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NbaApiService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        private const string BaseUrl = "https://api.balldontlie.io/v1";
        private const string GamesApiUrl = "https://api.balldontlie.io/nba/v1";
        private const string ApiKey = "050745e4-8652-4258-b7fe-50b6d91a48c2";

        // キャッシュキー
        private const string CacheKeyTeams = "nba:teams";
        private static string CacheKeyTeam(int id) => $"nba:team:{id}";
        private static string CacheKeyPlayers(int id) => $"nba:players:{id}";
        private static string CacheKeyGames(string d) => $"nba:games:{d}";
        private static string CacheKeyGame(int id) => $"nba:game:{id}";
        private static string CacheKeyStats(int id) => $"nba:stats:{id}";
        private static string CacheKeyAvg(int s, string ids) => $"nba:avg:{s}:{ids}";

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public NbaApiService(
            HttpClient httpClient,
            ILogger<NbaApiService> logger,
            IMemoryCache cache,
            IConfiguration config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _config = config;

            if (!string.IsNullOrEmpty(ApiKey))
                _httpClient.DefaultRequestHeaders.Add("Authorization", ApiKey);
        }

        // =============================================
        // 全チーム取得（24時間キャッシュ）
        // =============================================
        public async Task<List<Team>> GetAllTeamsAsync()
        {
            if (_cache.TryGetValue(CacheKeyTeams, out List<Team>? cached) && cached != null)
            {
                _logger.LogInformation("[Cache HIT] teams");
                return cached;
            }
            try
            {
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/teams?per_page=30");
                var result = JsonSerializer.Deserialize<ApiResponse<Team>>(response, _jsonOptions);
                var data = result?.Data ?? GetFallbackTeams();
                _cache.Set(CacheKeyTeams, data, GetExpiry("TeamsExpireHours", 24));
                _logger.LogInformation("[Cache SET] teams");
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch teams. Using fallback.");
                return GetFallbackTeams();
            }
        }

        // =============================================
        // チーム単体取得（24時間キャッシュ）
        // =============================================
        public async Task<Team?> GetTeamAsync(int teamId)
        {
            var key = CacheKeyTeam(teamId);
            if (_cache.TryGetValue(key, out Team? cached) && cached != null)
            {
                _logger.LogInformation("[Cache HIT] team:{TeamId}", teamId);
                return cached;
            }
            try
            {
                var response = await _httpClient.GetStringAsync($"{BaseUrl}/teams/{teamId}");
                var result = JsonSerializer.Deserialize<TeamResponse>(response, _jsonOptions);
                var data = result?.Data;
                if (data != null)
                {
                    _cache.Set(key, data, GetExpiry("TeamsExpireHours", 24));
                    _logger.LogInformation("[Cache SET] team:{TeamId}", teamId);
                }
                return data ?? GetFallbackTeams().FirstOrDefault(t => t.Id == teamId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch team {TeamId}. Using fallback.", teamId);
                return GetFallbackTeams().FirstOrDefault(t => t.Id == teamId);
            }
        }

        // =============================================
        // 選手一覧取得（24時間キャッシュ）
        // =============================================
        public async Task<List<Player>> GetPlayersByTeamAsync(int teamId)
        {
            var key = CacheKeyPlayers(teamId);
            if (_cache.TryGetValue(key, out List<Player>? cached) && cached != null)
            {
                _logger.LogInformation("[Cache HIT] players:{TeamId}", teamId);
                return cached;
            }
            var players = new List<Player>();
            try
            {
                int page = 1, totalPages = 1;
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

                var sorted = players.OrderBy(p => p.Last_name).ToList();
                _cache.Set(key, sorted, GetExpiry("PlayersExpireHours", 24));
                _logger.LogInformation("[Cache SET] players:{TeamId} ({Count}件)", teamId, sorted.Count);
                return sorted;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch players for team {TeamId}. Using fallback.", teamId);
                return GetFallbackPlayers(teamId);
            }
        }

        // =============================================
        // シーズン平均取得（24時間キャッシュ）
        // =============================================
        public async Task<List<SeasonAverage>> GetSeasonAveragesAsync(List<int> playerIds, int season)
        {
            var idsKey = string.Join("-", playerIds.OrderBy(x => x));
            var key = CacheKeyAvg(season, idsKey);
            if (_cache.TryGetValue(key, out List<SeasonAverage>? cached) && cached != null)
            {
                _logger.LogInformation("[Cache HIT] season_averages:{Season}", season);
                return cached;
            }
            try
            {
                var ids = string.Join("&", playerIds.Select(id => $"player_ids[]={id}"));
                var url = $"{BaseUrl}/season_averages?season={season}&{ids}";
                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<ApiResponse<SeasonAverage>>(response, _jsonOptions);
                var data = result?.Data ?? new List<SeasonAverage>();
                _cache.Set(key, data, GetExpiry("PlayersExpireHours", 24));
                _logger.LogInformation("[Cache SET] season_averages:{Season}", season);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch season averages.");
                return new List<SeasonAverage>();
            }
        }

        // =============================================
        // 試合一覧取得（10分キャッシュ）
        // =============================================
        public async Task<List<Game>> GetGamesByDateAsync(string date)
        {
            var key = CacheKeyGames(date);
            if (_cache.TryGetValue(key, out List<Game>? cached) && cached != null)
            {
                _logger.LogInformation("[Cache HIT] games:{Date}", date);
                return cached;
            }
            try
            {
                var url = $"{GamesApiUrl}/games?dates[]={date}&per_page=15";
                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<ApiResponse<Game>>(response, _jsonOptions);
                var data = result?.Data ?? new List<Game>();

                // ライブ試合は10分、終了試合は24時間キャッシュ
                var hasLive = data.Any(g => g.IsLive);
                var expiry = hasLive
                    ? TimeSpan.FromMinutes(10)
                    : TimeSpan.FromHours(24);
                _cache.Set(key, data, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry
                });
                _logger.LogInformation("[Cache SET] games:{Date} ({Count}件, live={HasLive})",
                    date, data.Count, hasLive);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch games for date {Date}.", date);
                return new List<Game>();
            }
        }

        // =============================================
        // 試合単体取得（10分キャッシュ）
        // =============================================
        public async Task<Game?> GetGameAsync(int gameId)
        {
            var key = CacheKeyGame(gameId);
            if (_cache.TryGetValue(key, out Game? cached) && cached != null)
            {
                _logger.LogInformation("[Cache HIT] game:{GameId}", gameId);
                return cached;
            }
            try
            {
                var url = $"{GamesApiUrl}/games/{gameId}";
                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<GameSingleResponse>(response, _jsonOptions);
                var data = result?.Data;
                if (data != null)
                {
                    var expiry = data.IsLive ? TimeSpan.FromMinutes(10) : TimeSpan.FromHours(24);
                    _cache.Set(key, data, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiry
                    });
                }
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch game {GameId}.", gameId);
                return null;
            }
        }

        // =============================================
        // 試合個人スタッツ取得（10分キャッシュ）
        // =============================================
        public async Task<List<GameStat>> GetGameStatsAsync(int gameId)
        {
            var key = CacheKeyStats(gameId);
            if (_cache.TryGetValue(key, out List<GameStat>? cached) && cached != null)
            {
                _logger.LogInformation("[Cache HIT] stats:{GameId}", gameId);
                return cached;
            }
            try
            {
                var url = $"{GamesApiUrl}/stats?game_ids[]={gameId}&per_page=50";
                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<ApiResponse<GameStat>>(response, _jsonOptions);
                var data = result?.Data ?? new List<GameStat>();
                _cache.Set(key, data, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
                _logger.LogInformation("[Cache SET] stats:{GameId} ({Count}件)", gameId, data.Count);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch stats for game {GameId}.", gameId);
                return new List<GameStat>();
            }
        }

        // =============================================
        // ヘルパー
        // =============================================
        private MemoryCacheEntryOptions GetExpiry(string configKey, int defaultHours)
        {
            var hours = _config.GetValue<int>($"CacheSettings:{configKey}", defaultHours);
            return new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(hours)
            };
        }

        // =============================================
        // フォールバックデータ
        // =============================================
        private static List<Team> GetFallbackTeams() => new()
        {
            new() { Id = 1,  Abbreviation = "ATL", City = "Atlanta",       Conference = "East", Division = "Southeast", Full_name = "Atlanta Hawks",          Name = "Hawks"        },
            new() { Id = 2,  Abbreviation = "BOS", City = "Boston",        Conference = "East", Division = "Atlantic",  Full_name = "Boston Celtics",         Name = "Celtics"      },
            new() { Id = 3,  Abbreviation = "BKN", City = "Brooklyn",      Conference = "East", Division = "Atlantic",  Full_name = "Brooklyn Nets",          Name = "Nets"         },
            new() { Id = 4,  Abbreviation = "CHA", City = "Charlotte",     Conference = "East", Division = "Southeast", Full_name = "Charlotte Hornets",      Name = "Hornets"      },
            new() { Id = 5,  Abbreviation = "CHI", City = "Chicago",       Conference = "East", Division = "Central",   Full_name = "Chicago Bulls",          Name = "Bulls"        },
            new() { Id = 6,  Abbreviation = "CLE", City = "Cleveland",     Conference = "East", Division = "Central",   Full_name = "Cleveland Cavaliers",    Name = "Cavaliers"    },
            new() { Id = 7,  Abbreviation = "DAL", City = "Dallas",        Conference = "West", Division = "Southwest", Full_name = "Dallas Mavericks",       Name = "Mavericks"    },
            new() { Id = 8,  Abbreviation = "DEN", City = "Denver",        Conference = "West", Division = "Northwest", Full_name = "Denver Nuggets",         Name = "Nuggets"      },
            new() { Id = 9,  Abbreviation = "DET", City = "Detroit",       Conference = "East", Division = "Central",   Full_name = "Detroit Pistons",        Name = "Pistons"      },
            new() { Id = 10, Abbreviation = "GSW", City = "Golden State",  Conference = "West", Division = "Pacific",   Full_name = "Golden State Warriors",  Name = "Warriors"     },
            new() { Id = 11, Abbreviation = "HOU", City = "Houston",       Conference = "West", Division = "Southwest", Full_name = "Houston Rockets",        Name = "Rockets"      },
            new() { Id = 12, Abbreviation = "IND", City = "Indiana",       Conference = "East", Division = "Central",   Full_name = "Indiana Pacers",         Name = "Pacers"       },
            new() { Id = 13, Abbreviation = "LAC", City = "Los Angeles",   Conference = "West", Division = "Pacific",   Full_name = "Los Angeles Clippers",   Name = "Clippers"     },
            new() { Id = 14, Abbreviation = "LAL", City = "Los Angeles",   Conference = "West", Division = "Pacific",   Full_name = "Los Angeles Lakers",     Name = "Lakers"       },
            new() { Id = 15, Abbreviation = "MEM", City = "Memphis",       Conference = "West", Division = "Southwest", Full_name = "Memphis Grizzlies",      Name = "Grizzlies"    },
            new() { Id = 16, Abbreviation = "MIA", City = "Miami",         Conference = "East", Division = "Southeast", Full_name = "Miami Heat",             Name = "Heat"         },
            new() { Id = 17, Abbreviation = "MIL", City = "Milwaukee",     Conference = "East", Division = "Central",   Full_name = "Milwaukee Bucks",        Name = "Bucks"        },
            new() { Id = 18, Abbreviation = "MIN", City = "Minnesota",     Conference = "West", Division = "Northwest", Full_name = "Minnesota Timberwolves", Name = "Timberwolves" },
            new() { Id = 19, Abbreviation = "NOP", City = "New Orleans",   Conference = "West", Division = "Southwest", Full_name = "New Orleans Pelicans",   Name = "Pelicans"     },
            new() { Id = 20, Abbreviation = "NYK", City = "New York",      Conference = "East", Division = "Atlantic",  Full_name = "New York Knicks",        Name = "Knicks"       },
            new() { Id = 21, Abbreviation = "OKC", City = "Oklahoma City", Conference = "West", Division = "Northwest", Full_name = "Oklahoma City Thunder",  Name = "Thunder"      },
            new() { Id = 22, Abbreviation = "ORL", City = "Orlando",       Conference = "East", Division = "Southeast", Full_name = "Orlando Magic",          Name = "Magic"        },
            new() { Id = 23, Abbreviation = "PHI", City = "Philadelphia",  Conference = "East", Division = "Atlantic",  Full_name = "Philadelphia 76ers",     Name = "76ers"        },
            new() { Id = 24, Abbreviation = "PHX", City = "Phoenix",       Conference = "West", Division = "Pacific",   Full_name = "Phoenix Suns",           Name = "Suns"         },
            new() { Id = 25, Abbreviation = "POR", City = "Portland",      Conference = "West", Division = "Northwest", Full_name = "Portland Trail Blazers", Name = "Trail Blazers"},
            new() { Id = 26, Abbreviation = "SAC", City = "Sacramento",    Conference = "West", Division = "Pacific",   Full_name = "Sacramento Kings",       Name = "Kings"        },
            new() { Id = 27, Abbreviation = "SAS", City = "San Antonio",   Conference = "West", Division = "Southwest", Full_name = "San Antonio Spurs",      Name = "Spurs"        },
            new() { Id = 28, Abbreviation = "TOR", City = "Toronto",       Conference = "East", Division = "Atlantic",  Full_name = "Toronto Raptors",        Name = "Raptors"      },
            new() { Id = 29, Abbreviation = "UTA", City = "Utah",          Conference = "West", Division = "Northwest", Full_name = "Utah Jazz",              Name = "Jazz"         },
            new() { Id = 30, Abbreviation = "WAS", City = "Washington",    Conference = "East", Division = "Southeast", Full_name = "Washington Wizards",     Name = "Wizards"      },
        };

        private static List<Player> GetFallbackPlayers(int teamId)
        {
            var allFallback = new Dictionary<int, List<Player>>
            {
                [14] = new()
                {
                    new() { Id = 1, First_name = "LeBron",   Last_name = "James",     Position = "F",   Height = "6-9",  Weight = "250", Jersey_number = "23", Country = "USA"   },
                    new() { Id = 2, First_name = "Anthony",  Last_name = "Davis",     Position = "F-C", Height = "6-10", Weight = "253", Jersey_number = "3",  Country = "USA"   },
                    new() { Id = 3, First_name = "Austin",   Last_name = "Reaves",    Position = "G",   Height = "6-5",  Weight = "206", Jersey_number = "15", Country = "USA"   },
                    new() { Id = 4, First_name = "D'Angelo", Last_name = "Russell",   Position = "G",   Height = "6-4",  Weight = "193", Jersey_number = "1",  Country = "USA"   },
                    new() { Id = 5, First_name = "Rui",      Last_name = "Hachimura", Position = "F",   Height = "6-8",  Weight = "230", Jersey_number = "28", Country = "Japan" },
                },
                [2] = new()
                {
                    new() { Id = 10, First_name = "Jayson",   Last_name = "Tatum",     Position = "F",   Height = "6-8", Weight = "210", Jersey_number = "0",  Country = "USA"               },
                    new() { Id = 11, First_name = "Jaylen",   Last_name = "Brown",     Position = "G-F", Height = "6-6", Weight = "223", Jersey_number = "7",  Country = "USA"               },
                    new() { Id = 12, First_name = "Kristaps", Last_name = "Porzingis", Position = "C",   Height = "7-2", Weight = "240", Jersey_number = "8",  Country = "Latvia"            },
                    new() { Id = 13, First_name = "Jrue",     Last_name = "Holiday",   Position = "G",   Height = "6-4", Weight = "205", Jersey_number = "4",  Country = "USA"               },
                    new() { Id = 14, First_name = "Al",       Last_name = "Horford",   Position = "F-C", Height = "6-9", Weight = "240", Jersey_number = "42", Country = "Dominican Republic" },
                },
            };
            return allFallback.TryGetValue(teamId, out var p) ? p : new List<Player>
            {
                new() { Id = 99, First_name = "Sample", Last_name = "Player", Position = "G", Height = "6-3", Weight = "200", Jersey_number = "00", Country = "USA", College = "State University" }
            };
        }
    }

    public class TeamResponse { public Team? Data { get; set; } }
    public class GameSingleResponse { public Game? Data { get; set; } }
}
