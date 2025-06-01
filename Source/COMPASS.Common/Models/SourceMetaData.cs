using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using ImageMagick;

namespace COMPASS.Common.Models;

/// <summary>
/// MetaData that can be provided by a metadata source
/// </summary>
public class SourceMetaData : IHasCodexMetadata
{
    public SourceMetaData() { }
    
    public SourceMetaData(Codex codex)
    {
        Title = codex.Title;
        Authors = [ ..codex.Authors];
        Publisher = codex.Publisher;
        Description = codex.Description;
        ReleaseDate = codex.ReleaseDate;
        PageCount = codex.PageCount;
        Version = codex.Version;
        Cover = File.Exists(codex.CoverArtPath) ? new MagickImage(codex.CoverArtPath) : null;
        Tags = [ ..codex.Tags];
    }
    public string Title { get; set; } = "";
    public IList<string> Authors { get; set; } = [];
    public string Publisher { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime? ReleaseDate { get; set; }
    public int PageCount { get; set; }
    public string Version { get; set; } = "";
    public IList<Tag> Tags { get; set; } = [];
    public IMagickImage? Cover { get; set; }
}