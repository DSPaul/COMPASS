﻿namespace COMPASS.Models.CodexProperties
{
    public class CoverArtProperty : FileProperty
    {
        public CoverArtProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override void SetProp(IHasCodexMetadata target, IHasCodexMetadata source)
        {
            target.CoverArt = source.CoverArt;
            target.Thumbnail = source.Thumbnail;
        }
    }
}
