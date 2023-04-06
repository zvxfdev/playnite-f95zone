using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace F95ZoneMetadataProvider
{
    public class F95ZoneMetadataProvider : MetadataPlugin
    {
        public static readonly ILogger logger = LogManager.GetLogger();
        private readonly Settings _settings;
        public static Settings settings;

        public override Guid Id { get; } = Guid.Parse("ab820846-6ffe-4883-ba22-e99af02a803f");
        
        public static List<MetadataField> Fields { get; } = new List<MetadataField>
        {
            MetadataField.Developers,
        MetadataField.Features,
        MetadataField.Genres,
        MetadataField.Icon,
        MetadataField.Links,
        MetadataField.Name,
        MetadataField.Tags,
        MetadataField.BackgroundImage,
        MetadataField.CommunityScore,
        MetadataField.CoverImage//TODO: FIELDS!!!
            // Include addition fields if supported by the metadata source
        };

        public override List<MetadataField> SupportedFields { get; } = Fields;

        // Change to something more appropriate
        public override string Name => "F95Zone";
        private readonly IPlayniteAPI _playniteAPI;
        public static IPlayniteAPI playniteAPI;

        public F95ZoneMetadataProvider(IPlayniteAPI api) : base(api)
        {
            _playniteAPI = api;
            playniteAPI = api;
            _settings = new Settings(this, _playniteAPI);
            F95ZoneMetadataProvider.settings = _settings;
            Properties = new MetadataPluginProperties
            {
                HasSettings = true
            };
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new F95ZoneMetadataProviderProvider(options, this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return _settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new F95ZoneMetadataProviderSettingsView();
        }
    }
}