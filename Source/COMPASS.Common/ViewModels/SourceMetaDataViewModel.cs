using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media.Imaging;
using COMPASS.Common.Models;
using ImageMagick;

namespace COMPASS.Common.ViewModels;

public class SourceMetadataViewModel : IDisposable
{    
    private readonly SourceMetadata _sourceMetadata;
    
    public SourceMetadataViewModel(SourceMetadata sourceMetadata)
    {
        _sourceMetadata = sourceMetadata;
        Cover = sourceMetadata.Cover?.ToWriteableBitmap();
    }
    public string Title => _sourceMetadata.Title;
    public string AuthorsAsString => string.Join(", ", _sourceMetadata.Authors);
    public string Publisher => _sourceMetadata.Publisher;
    public string Description => _sourceMetadata.Description;
    public DateTime? ReleaseDate => _sourceMetadata.ReleaseDate;
    public int PageCount => _sourceMetadata.PageCount;
    public string Version => _sourceMetadata.Version;
    
    public IList<Tag> Tags => _sourceMetadata.Tags;
    public Bitmap? Cover { get; }

    //Make this a method rather than a property to avoid binding to it
    public SourceMetadata GetSourceMetadata() =>  _sourceMetadata;
    
    public void Dispose()
    {
        Cover?.Dispose();
    }

    public void DeepDispose()
    {
        //We didn't create the _sourceMetadata so should dispose it by default
        _sourceMetadata.Dispose();
        Dispose();
    }
}