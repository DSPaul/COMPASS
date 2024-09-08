using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COMPASS.Models.XmlDtos
{
    public static class XmlMapper
    {
        #region Sanitize xml data
        //based on https://seattlesoftware.wordpress.com/2008/09/11/hexadecimal-value-0-is-an-invalid-character/
        /// <summary>
        /// Remove illegal XML characters from a string.
        /// </summary>
        private static string Sanitize(this string str)
        {
            if (str is null) return "";

            StringBuilder buffer = new(str.Length);
            foreach (char c in str.Where(c => IsLegalXmlChar(c)))
            {
                buffer.Append(c);
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Whether a given character is allowed by XML 1.0.
        /// </summary>
        private static bool IsLegalXmlChar(int character) => character switch
        {
            0x9 => true, // '\t' == 9 
            0xA => true, // '\n' == 10         
            0xD => true, // '\r' == 13        
            (>= 0x20) and (<= 0xD7FF) => true,
            (>= 0xE000) and (<= 0xFFFD) => true,
            (>= 0x10000) and (<= 0x10FFFF) => true,
            _ => false
        };

        #endregion

        #region Codex

        public static Codex ToModel(this CodexDto dto, IList<Tag> tags)
        {
            Codex codex = new()
            {
                // COMPASS related Metadata
                ID = dto.ID,
                CoverArt = dto.CoverArt,
                Thumbnail = dto.Thumbnail,

                //Codex related Metadata
                Title = dto.Title,
                SortingTitle = dto.SortingTitle,
                Authors = new(dto.Authors),
                Publisher = dto.Publisher,
                Description = dto.Description,
                ReleaseDate = dto.ReleaseDate,
                PageCount = dto.PageCount,
                Version = dto.Version,

                //User related Metadata
                PhysicallyOwned = dto.PhysicallyOwned,
                Rating = dto.Rating,
                Favorite = dto.Favorite,

                //User behaviour metadata
                DateAdded = dto.DateAdded,
                LastOpened = dto.LastOpened,
                OpenedCount = dto.OpenedCount,

                //Sources
                SourceURL = dto.SourceURL,
                Path = dto.Path,
                ISBN = dto.ISBN,
            };

            codex.Tags = new(tags.Where(tag => dto.TagIDs.Contains(tag.ID)));

            return codex;
        }

        public static CodexDto ToDto(this Codex model)
        {
            CodexDto dto = new()
            {
                // COMPASS related Metadata
                ID = model.ID,
                CoverArt = model.CoverArt.Sanitize(),
                Thumbnail = model.Thumbnail.Sanitize(),

                //Codex related Metadata
                Title = model.Title.Sanitize(),
                SortingTitle = model.UserDefinedSortingTitle.Sanitize(),
                Authors = model.Authors.Select(author => author.Sanitize()).ToList(),
                Publisher = model.Publisher.Sanitize(),
                Description = model.Description.Sanitize(),
                ReleaseDate = model.ReleaseDate,
                PageCount = model.PageCount,
                Version = model.Version,

                //User related Metadata
                PhysicallyOwned = model.PhysicallyOwned,
                Rating = model.Rating,
                Favorite = model.Favorite,
                TagIDs = model.Tags.Select(t => t.ID).ToList(),

                //User behaviour metadata
                DateAdded = model.DateAdded,
                LastOpened = model.LastOpened,
                OpenedCount = model.OpenedCount,

                //Sources
                SourceURL = model.SourceURL.Sanitize(),
                Path = model.Path.Sanitize(),
                ISBN = model.ISBN.Sanitize(),
            };

            return dto;
        }

        #endregion
    }
}
