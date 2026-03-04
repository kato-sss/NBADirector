using Microsoft.AspNetCore.Mvc;
using NBADirectory.Models;
using NBADirectory.Services;
using System.Diagnostics;

namespace NBADirectory.Controllers
{
    public class HomeController : Controller
    {
        private readonly INbaApiService _nbaApiService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(INbaApiService nbaApiService, ILogger<HomeController> logger)
        {
            _nbaApiService = nbaApiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var teams = await _nbaApiService.GetAllTeamsAsync();
            var viewModel = new HomeViewModel
            {
                EastTeams = teams.Where(t => t.Conference == "East").OrderBy(t => t.Division).ThenBy(t => t.Full_name).ToList(),
                WestTeams = teams.Where(t => t.Conference == "West").OrderBy(t => t.Division).ThenBy(t => t.Full_name).ToList()
            };
            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
