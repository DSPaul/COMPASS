using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services;
using COMPASS.Infra.Tools;
using ImageMagick;

namespace COMPASS.Common.Models.CodexProperties
{
    public class CoverProperty : CodexProperty
    {
        public CoverProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override bool IsEmpty(IHasCodexMetadata codex)
        {
            return codex switch
            {
                Codex c => !File.Exists(c.CoverArtPath),
                SourceMetaData md => md.Cover == null,
                _ => true
            };
        }

        public override void Copy(SourceMetaData target, SourceMetaData source) => target.Cover = source.Cover;
        public override void Apply(SourceMetaData source, Codex target)
        {
            throw new InvalidOperationException("Cover is special case, does IO so use ApplyAsync instead");
        }

        public async Task ApplyAsync(SourceMetaData source, Codex target, ICoverService coverService)
        {
            if (source.Cover == null) return;
            await coverService.SaveCover(target, source.Cover);
            source.Cover.Dispose();
        }

        public override bool HasNewValue(SourceMetaData toEvaluate, Codex reference)
        {
            IMagickImage? coverToEval = GetCover(toEvaluate);

            if (coverToEval == null) return false;
            
            IMagickImage? coverRef = GetCover(reference);

            if (coverRef == null) return true;
            
            //resize to same size for proper compare
            coverToEval.Resize(coverRef.Width, coverRef.Height);
            var isEqual = coverRef.Compare(coverToEval).MeanErrorPerPixel == 0;
            
            coverToEval.Dispose();
            coverRef.Dispose();
            
            return isEqual;
        }

        private MagickImage? GetCover(IHasCodexMetadata hasCodexMetadata)
        {
            return hasCodexMetadata switch
            {
                Codex c => new(c.CoverArtPath),
                SourceMetaData md => md.Cover is IMagickImage<byte> mi ? new MagickImage(mi) : null,
                _ => null
            };
        }
    }
}
