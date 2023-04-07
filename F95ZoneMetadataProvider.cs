using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace F95ZoneMetadataProvider
{
    public class F95ZoneMetadataProvider : MetadataPlugin
    {
        public static readonly ILogger Logger = LogManager.GetLogger();

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
            MetadataField.CoverImage 
        };

        public override List<MetadataField> SupportedFields { get; } = Fields;
        
        public override string Name => "F95Zone";
        public static IPlayniteAPI Api = null!;
        public static Settings Settings = null!;


        public F95ZoneMetadataProvider(IPlayniteAPI api) : base(api)
        {
            Api = api;
            Settings = new Settings(this, api);

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
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new F95ZoneMetadataProviderSettingsView();
        }
    }
}