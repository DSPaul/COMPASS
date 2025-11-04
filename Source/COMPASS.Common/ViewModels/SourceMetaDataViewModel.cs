using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media.Imaging;
using COMPASS.Common.Models;
using ImageMagick;

namespace COMPASS.Common.ViewModels;

public class SourceMetaDataViewModel : IDisposable
{    
    private readonly SourceMetaData _sourceMetaData;
    
    public SourceMetaDataViewModel(SourceMetaData sourceMetaData)
    {
        _sourceMetaData = sourceMetaData;
        Cover = sourceMetaData.Cover?.ToWriteableBitmap();
    }
    public string Title => _sourceMetaData.Title;
    public string AuthorsAsString => string.Join(", ", _sourceMetaData.Authors);
    public string Publisher => _sourceMetaData.Publisher;
    public string Description => _sourceMetaData.Description;
    public DateTime? ReleaseDate => _sourceMetaData.ReleaseDate;
    public int PageCount => _sourceMetaData.PageCount;
    public string Version => _sourceMetaData.Version;
    
    public IList<Tag> Tags => _sourceMetaData.Tags;
    public Bitmap? Cover { get; }

    //Make this a method rather than a property to avoid binding to it
    public SourceMetaData GetSource() =>  _sourceMetaData;
    
    public void Dispose()
    {
        Cover?.Dispose();
    }

    public void DeepDispose()
    {
        //We didn't create the _sourceMetadata so should dispose it by default
        _sourceMetaData.Dispose();
        Dispose();
    }
}