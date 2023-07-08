using System;
using System.Linq;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace F95ZoneMetadataProvider
{
    public class UpdateChecker
    {
        private readonly IPlayniteAPI _api;
        private readonly Scrapper _scrapper;

        public UpdateChecker(IPlayniteAPI api, Scrapper scrapper)
        {
            _api = api;
            _scrapper = scrapper;
        }

        public async void CheckAllGamesForUpdates()
        {
            try
            {
                foreach (var game in _api.Database.Games)
                {
                    Link? link = game.Links?.FirstOrDefault(link => link.Url.StartsWith("https://f95zone.to/threads/"));
                    if (link == null) continue;
                    await CheckGameForUpdates(game, link);
                }
            }
            catch (Exception ex)
            {
                _api.Notifications.Add(Guid.NewGuid().ToString(),
                    "[F95Zone] Failed to check for updates (check your internet connection), error: " + ex.Message + ex.StackTrace, NotificationType.Info);
            }
        }

        private async Task CheckGameForUpdates(Game game, Link link)
        {
            // Check if game has f95zone link added
            var scraped = await this._scrapper.ScrapPage(link.Url.Split(new[] { "https://f95zone.to/threads/" },
                StringSplitOptions.None)[1]);
            var latestVersion = scraped?.Version;
            if (latestVersion == null) return;

            // Mismatched version, send notification!
            if (latestVersion != game.Version)
            {
                _api.Notifications.Add(Guid.NewGuid().ToString(), $"Game update available: {game.Name}, link: {link.Url}, (Old Version: {game.Version}, New Version: {latestVersion})",
                    /*"Game update available: " + game.Name + ", link: " + link.Url + " (Old Version: " + game.Version + ", New Version: " + latestVersion + ")",*/ NotificationType.Info);
            }
        }
    }
}