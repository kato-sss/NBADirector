using Microsoft.AspNetCore.Mvc;
using NBADirectory.Models;
using NBADirectory.Services;

namespace NBADirectory.Controllers
{
    public class TeamController : Controller
    {
        private readonly INbaApiService _nbaApiService;

        public TeamController(INbaApiService nbaApiService)
        {
            _nbaApiService = nbaApiService;
        }

        // GET: /Team/{id}
        public async Task<IActionResult> Index(int id, string? search)
        {
            var team = await _nbaApiService.GetTeamAsync(id);
            if (team == null) return NotFound();

            var players = await _nbaApiService.GetPlayersByTeamAsync(id);

            var viewModel = new TeamPlayersViewModel
            {
                Team = team,
                Players = players
            };
            return View(viewModel);
        }
    }

    // API Controller for JSON responses
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersApiController : ControllerBase
    {
        private readonly INbaApiService _nbaApiService;

        public PlayersApiController(INbaApiService nbaApiService)
        {
            _nbaApiService = nbaApiService;
        }

        // GET: /api/PlayersApi/teams
        [HttpGet("teams")]
        public async Task<IActionResult> GetTeams()
        {
            var teams = await _nbaApiService.GetAllTeamsAsync();
            return Ok(teams);
        }

        // GET: /api/PlayersApi/teams/{id}/players
        [HttpGet("teams/{id}/players")]
        public async Task<IActionResult> GetPlayersByTeam(int id, [FromQuery] string? search)
        {
            var players = await _nbaApiService.GetPlayersByTeamAsync(id);

            if (!string.IsNullOrWhiteSpace(search))
            {
                players = players.Where(p =>
                    p.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Position.Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            return Ok(new { team_id = id, count = players.Count, players });
        }
    }
}
