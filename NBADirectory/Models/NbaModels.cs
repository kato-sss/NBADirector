namespace NBADirectory.Models
{
    // BallDontLie API v1 response models
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
        public string FullName => $"{First_name ?? ""} {Last_name ?? ""}".Trim();
        public string PositionDisplay => string.IsNullOrEmpty(Position) ? "N/A" : Position;
        public string HeightDisplay => string.IsNullOrEmpty(Height) ? "N/A" : Height;
        public string WeightDisplay => string.IsNullOrEmpty(Weight) ? "N/A" : $"{Weight} lbs";
        public string JerseyDisplay => string.IsNullOrEmpty(Jersey_number) ? "-" : $"#{Jersey_number}";
        // Season Averages から取得した平均出場時間（ソート用）
        public double? AvgMinutes { get; set; }
        public string AvgMinutesDisplay => AvgMinutes.HasValue
            ? $"{AvgMinutes:F1} min/g"
            : null!;
    }
    public class SeasonAverage
    {
        public int Player_id { get; set; }
        public double Min { get; set; }       // 平均出場時間
        public double Pts { get; set; }       // 平均得点
        public double Reb { get; set; }       // 平均リバウンド
        public double Ast { get; set; }       // 平均アシスト
        public int Games_played { get; set; }
        public int Season { get; set; }
    }

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
}