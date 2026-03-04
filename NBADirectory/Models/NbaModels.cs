namespace NBADirectory.Models
{
    // =============================================
    // BallDontLie API v1 共通
    // =============================================
    public class ApiResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public Meta? Meta { get; set; }
    }

    public class Meta
    {
        public int Total_pages { get; set; }
        public int Current_page { get; set; }
        public int Next_page { get; set; }
        public int Per_page { get; set; }
        public int Total_count { get; set; }
    }

    // =============================================
    // チーム
    // =============================================
    public class Team
    {
        public int Id { get; set; }
        public string Abbreviation { get; set; } = "";
        public string City { get; set; } = "";
        public string Conference { get; set; } = "";
        public string Division { get; set; } = "";
        public string Full_name { get; set; } = "";
        public string Name { get; set; } = "";
    }

    // =============================================
    // 選手
    // =============================================
    public class Player
    {
        public int Id { get; set; }
        public string First_name { get; set; } = "";
        public string Last_name { get; set; } = "";
        public string Position { get; set; } = "";
        public string Height { get; set; } = "";
        public string Weight { get; set; } = "";
        public string Jersey_number { get; set; } = "";
        public string College { get; set; } = "";
        public string Country { get; set; } = "";
        public int? Draft_year { get; set; }
        public int? Draft_round { get; set; }
        public int? Draft_number { get; set; }
        public Team? Team { get; set; }
        public double? AvgMinutes { get; set; }

        public string FullName => $"{First_name ?? ""} {Last_name ?? ""}".Trim();
        public string PositionDisplay => string.IsNullOrEmpty(Position) ? "N/A" : Position;
        public string HeightDisplay => string.IsNullOrEmpty(Height) ? "N/A" : Height;
        public string WeightDisplay => string.IsNullOrEmpty(Weight) ? "N/A" : $"{Weight} lbs";
        public string JerseyDisplay => string.IsNullOrEmpty(Jersey_number) ? "-" : $"#{Jersey_number}";
    }

    // =============================================
    // シーズン平均スタッツ
    // =============================================
    public class SeasonAverage
    {
        public int Player_id { get; set; }
        public double Min { get; set; }
        public double Pts { get; set; }
        public double Reb { get; set; }
        public double Ast { get; set; }
        public int Games_played { get; set; }
        public int Season { get; set; }
    }

    // =============================================
    // 試合
    // =============================================
    public class Game
    {
        public int Id { get; set; }
        public string Date { get; set; } = "";
        public string Status { get; set; } = "";
        public int Period { get; set; }
        public string Time { get; set; } = "";
        public int Home_team_score { get; set; }
        public int Visitor_team_score { get; set; }
        public Team Home_team { get; set; } = new();
        public Team Visitor_team { get; set; } = new();
        public int Season { get; set; }
        public bool Postseason { get; set; }

        public DateTime DateParsed =>
            DateTime.TryParse(Date, out var d) ? d.ToLocalTime() : DateTime.MinValue;

        public string DateDisplay =>
            DateParsed == DateTime.MinValue ? Date : DateParsed.ToString("yyyy/MM/dd");

        public bool IsLive =>
            Status != "Final" && Status != "" &&
            !Status.StartsWith("0") && Period > 0;

        public bool IsFinal => Status == "Final";

        public string StatusDisplay => Status switch
        {
            "Final" => "終了",
            "Halftime" => "ハーフタイム",
            _ => IsLive ? $"第{Period}Q {Time}" : Status
        };

        public string WinnerAbbr =>
            IsFinal
                ? (Home_team_score > Visitor_team_score
                    ? Home_team.Abbreviation
                    : Visitor_team.Abbreviation)
                : "";
    }

    // =============================================
    // 試合個人スタッツ
    // =============================================
    public class GameStat
    {
        public int Id { get; set; }
        public double Min { get; set; }
        public double Pts { get; set; }
        public double Reb { get; set; }
        public double Ast { get; set; }
        public double Stl { get; set; }
        public double Blk { get; set; }
        public double Turnover { get; set; }
        public double Fg_pct { get; set; }
        public double Fg3_pct { get; set; }
        public double Ft_pct { get; set; }
        public Player Player { get; set; } = new();
        public Team Team { get; set; } = new();
    }

    // =============================================
    // ViewModels
    // =============================================
    public class TeamPlayersViewModel
    {
        public Team Team { get; set; } = new();
        public List<Player> Players { get; set; } = new();
        public string? SearchQuery { get; set; }
        public string? SelectedConference { get; set; }
        public bool IsSortedByMinutes { get; set; }
        public int SortedSeason { get; set; }
    }

    public class HomeViewModel
    {
        public List<Team> EastTeams { get; set; } = new();
        public List<Team> WestTeams { get; set; } = new();
    }

    public class GamesViewModel
    {
        public List<Game> Games { get; set; } = new();
        public string SelectedDate { get; set; } = "";
        public DateTime DisplayDate { get; set; }
        public List<Game> LiveGames => Games.Where(g => g.IsLive).ToList();
        public List<Game> FinalGames => Games.Where(g => g.IsFinal).ToList();
        public List<Game> ScheduledGames => Games.Where(g => !g.IsLive && !g.IsFinal).ToList();
    }

    public class GameDetailViewModel
    {
        public Game Game { get; set; } = new();
        public List<GameStat> HomeStats { get; set; } = new();
        public List<GameStat> VisitorStats { get; set; } = new();
    }
}
