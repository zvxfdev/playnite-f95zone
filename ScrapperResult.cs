using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Playnite.SDK.Models;

namespace F95ZoneMetadataProvider
{
    public class ScrapperResult
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public List<string>? Labels { get; set; }
        public string? Version { get; set; }
        public string? Developer { get; set; }
        public string? Description { get; set; }
        public List<string>? Tags { get; set; }
        public double Rating { get; set; }
        public List<string>? Images { get; set; }
        public List<Link>? Links { get; set; }
    }

    public class ScrapperSearchResult
    {
        public string? Link { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
