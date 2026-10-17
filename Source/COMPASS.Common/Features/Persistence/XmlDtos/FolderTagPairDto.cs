using System.Xml.Serialization;

namespace COMPASS.Common.Features.Persistence.XmlDtos;

[XmlRoot("FolderTagPair"), XmlType("FolderTagPair")]
public class FolderTagPairDto
{
    public required string Folder { get; set; }
    public required int TagID { get; set; }
}