using Microsoft.AspNetCore.Mvc;
using NBADirectory.Models;
using NBADirectory.Services;

namespace NBADirectory.Controllers
{
    public class GamesController : Controller
    {
        private readonly INbaApiService _nbaApiService;

        public GamesController(INbaApiService nbaApiService)
        {
            _nbaApiService = nbaApiService;
        }

        // GET: /Games?date=2024-11-13
        public async Task<IActionResult> Index(string? date)
        {
            // 日付未指定の場合は前日（試合結果が出ている可能性が高い）
            var jstNow = DateTime.UtcNow.AddHours(9);
            var targetDate = string.IsNullOrEmpty(date)
                ? jstNow.AddDays(-1).ToString("yyyy-MM-dd")
                : date;

            var games = await _nbaApiService.GetGamesByDateAsync(targetDate);

            var viewModel = new GamesViewModel
            {
                Games = games,
                SelectedDate = targetDate,
                DisplayDate = DateTime.Parse(targetDate)
            };

            return View(viewModel);
        }

        // GET: /Games/Detail/{id}
        public async Task<IActionResult> Detail(int id)
        {
            var game = await _nbaApiService.GetGameAsync(id);
            if (game == null) return NotFound();

            var stats = await _nbaApiService.GetGameStatsAsync(id);

            var viewModel = new GameDetailViewModel
            {
                Game = game,
                HomeStats = stats.Where(s => s.Team.Id == game.Home_team.Id)
                                    .OrderByDescending(s => s.Pts).ToList(),
                VisitorStats = stats.Where(s => s.Team.Id == game.Visitor_team.Id)
                                    .OrderByDescending(s => s.Pts).ToList()
            };

            return View(viewModel);
        }
    }
}
