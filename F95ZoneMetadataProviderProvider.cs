using Playnite.SDK.Models;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace F95ZoneMetadataProvider
{
    public class F95ZoneMetadataProviderProvider : OnDemandMetadataProvider
    {
        private const string IconUrl = "https://static.f95zone.to/assets/favicon-32x32.png";

       // private readonly ILogger<F95ZoneMetadataProvider> _logger;

        public override List<MetadataField> AvailableFields => F95ZoneMetadataProvider.Fields;

        private readonly MetadataRequestOptions options;
        private readonly F95ZoneMetadataProvider plugin;
        private Game Game => options.GameData;
        private bool IsBackgroundDownload => options.IsBackgroundDownload;

       // public override List<MetadataField> AvailableFields => throw new NotImplementedException();

        public F95ZoneMetadataProviderProvider(MetadataRequestOptions options, F95ZoneMetadataProvider plugin)
        {
            this.options = options;
            this.plugin = plugin;
        }

        private ScrapperResult? _result;
        private bool _didRun;

        private static string? GetIdFromLink(string link)
        {
            if (!link.StartsWith(Scrapper.DefaultBaseUrl, StringComparison.OrdinalIgnoreCase)) return null;

            var threadId = link.Substring(Scrapper.DefaultBaseUrl.Length);
            if (threadId.EndsWith("/"))
                threadId = threadId.Substring(0, threadId.Length - 1);

            var dotIndex = threadId.IndexOf('.');
            return dotIndex == -1 ? threadId : threadId.Substring(dotIndex + 1);
        }

        public static string? GetIdFromGame(Game game)
        {
            if (game.Name is not null)
            {
                {
                    var threadId = GetIdFromLink(game.Name);
                    if (threadId is not null) return threadId;
                }

                if (game.Name.StartsWith("F95-", StringComparison.OrdinalIgnoreCase))
                {
                    var threadId = game.Name.Substring(4);
                    return threadId;
                }
            }

            var f95Link = game.Links?.FirstOrDefault(link => link.Name.Equals("F95zone", StringComparison.OrdinalIgnoreCase));
            if (f95Link is not null && !string.IsNullOrWhiteSpace(f95Link.Url))
            {
                return GetIdFromLink(f95Link.Url);
            }

            return null;
        }

        public static Scrapper SetupScrapper(Settings settings)
        {
            var clientHandler = new HttpClientHandler();
            clientHandler.Properties.Add("User-Agent", "Playnite.Extensions");

            var cookieContainer = settings.CreateCookieContainer();
            if (cookieContainer is not null)
            {
                clientHandler.UseCookies = true;
                clientHandler.CookieContainer = cookieContainer;
            }

            var scrapper = new Scrapper(F95ZoneMetadataProvider.Logger/*CustomLogger.GetLogger<Scrapper>(nameof(Scrapper))*/, clientHandler);
            return scrapper;
        }

        private ScrapperResult? GetResult(GetMetadataFieldArgs args)
        {
            if (_didRun) return _result;

            var scrapper = SetupScrapper(F95ZoneMetadataProvider.Settings);

            var id = GetIdFromGame(Game);
            if (id is null)
            {
                if (string.IsNullOrWhiteSpace(Game.Name))
                {
                    F95ZoneMetadataProvider.Logger.Error("Unable to get Id from Game and Name is null or whitespace!");
                    _didRun = true;
                    return null;
                }

                if (IsBackgroundDownload)
                {
                    // background download so we just choose the first item

                    var searchTask = scrapper.ScrapSearchPage(Game.Name, args.CancelToken);
                    searchTask.Wait(args.CancelToken);

                    var searchResult = searchTask.Result;
                    if (searchResult is null || !searchResult.Any())
                    {
                        F95ZoneMetadataProvider.Logger.Error($"Search return nothing for {Game.Name}, make sure you are logged in!");
                        _didRun = true;
                        return null;
                    }

                    id = GetIdFromLink(searchResult.First().Link ?? string.Empty);
                    if (id is null)
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    var item = F95ZoneMetadataProvider.Api.Dialogs.ChooseItemWithSearch(
                        new List<GenericItemOption>(),
                        searchString =>
                        {
                            var searchTask = scrapper.ScrapSearchPage(searchString, args.CancelToken);
                            searchTask.Wait(args.CancelToken);

                            var searchResult = searchTask.Result;
                            if (searchResult is null || !searchResult.Any())
                            {
                                F95ZoneMetadataProvider.Logger.Error("Search return nothing, make sure you are logged in!");
                                _didRun = true;
                                return null;
                            }

                            var items = searchResult
                                .Where(x => x.Name is not null && x.Link is not null)
                                .Select(x => new GenericItemOption(x.Name, x.Link))
                                .ToList();

                            return items;
                        }, Game.Name, "Search F95zone");

                    if (item is null)
                    {
                        _didRun = true;
                        return null;
                    }

                    var link = item.Description;

                    id = GetIdFromLink(link ?? string.Empty);

                    if (id is null)
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            var task = scrapper.ScrapPage(id, args.CancelToken);
            task.Wait(args.CancelToken);
            _result = task.Result;
            _didRun = true;

            // TODO: there is no override function for this
            if (_result?.Version != null)
            {
                Game.Version = _result.Version;
            }

            return _result;
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            return GetResult(args)?.Name ?? base.GetName(args);
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            return GetResult(args)?.Description ?? base.GetName(args);
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            var dev = GetResult(args)?.Developer;
            if (dev is null) return base.GetDevelopers(args);

            var company = F95ZoneMetadataProvider.Api.Database.Companies.Where(x => x.Name is not null).FirstOrDefault(x => x.Name.Equals(dev, StringComparison.OrdinalIgnoreCase));
            if (company is not null) return new[] { new MetadataIdProperty(company.Id) };
            return new[] { new MetadataNameProperty(dev) };
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            var result = GetResult(args);
            var id = result?.Id;
            var fetchedLinks = result?.Links;
            if (id == null)
            {
                return base.GetLinks(args);
            }

            Link defaultLink = new Link("F95zone", Scrapper.DefaultBaseUrl + id);

            if (fetchedLinks == null)
            {
                return new[] { defaultLink };
            }
            
            fetchedLinks.Add(defaultLink);
            return fetchedLinks;
        }

        private IEnumerable<MetadataProperty>? GetProperties(GetMetadataFieldArgs args, PlayniteProperty currentProperty)
        {
            // Tags
            var tagProperties = PlaynitePropertyHelper.ConvertValuesIfPossible(
                F95ZoneMetadataProvider.Api,
                F95ZoneMetadataProvider.Settings.TagProperty,
                currentProperty,
                () => GetResult(args)?.Tags);

            // Labels
            var labelProperties = PlaynitePropertyHelper.ConvertValuesIfPossible(
                F95ZoneMetadataProvider.Api,
                 F95ZoneMetadataProvider.Settings.LabelProperty,
                currentProperty,
                () => GetResult(args)?.Labels);

            return PlaynitePropertyHelper.MultiConcat(tagProperties, labelProperties);
        }

        public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
        {
            return GetProperties(args, PlayniteProperty.Tags) ?? base.GetTags(args);
        }

        public override IEnumerable<MetadataProperty> GetFeatures(GetMetadataFieldArgs args)
        {
            return GetProperties(args, PlayniteProperty.Features) ?? base.GetFeatures(args);
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            return GetProperties(args, PlayniteProperty.Genres) ?? base.GetGenres(args);
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            var rating = GetResult(args)?.Rating;
            return rating switch
            {
                null => base.GetCommunityScore(args),
                double.NaN => base.GetCommunityScore(args),
                _ => (int)(rating.Value / 5 * 100)
            };
        }

        private MetadataFile? SelectImage(GetMetadataFieldArgs args, string caption)
        {
            var images = GetResult(args)?.Images;
            if (images is null || !images.Any()) return null;

            if (IsBackgroundDownload)
            {
                return new MetadataFile(images.First());
            }

            var imageFileOption = F95ZoneMetadataProvider.Api.Dialogs.ChooseImageFile(images.Select(image => new ImageFileOption(image)).ToList(), caption);
            return imageFileOption == null ? null : new MetadataFile(imageFileOption.Path);
        }

        public override MetadataFile? GetCoverImage(GetMetadataFieldArgs args)
        {
            return SelectImage(args, "Select Cover Image");
        }

        public override MetadataFile? GetBackgroundImage(GetMetadataFieldArgs args)
        {
            return SelectImage(args, "Select Background Image");
        }

        public override MetadataFile GetIcon(GetMetadataFieldArgs args)
        {
            return F95ZoneMetadataProvider.Settings.SetDefaultIcon ? new MetadataFile(IconUrl) : base.GetIcon(args);
        }
    }
}