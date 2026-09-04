using System.Xml.Serialization;

namespace COMPASS.Common.Models.XmlDtos;

[XmlRoot("Folder"), XmlType("Folder")]
public class FolderDto
{
    public bool HasAllSubFolders { get; set; } = true;
    public string FullPath { get; set; } = "";

    /// <summary>
    /// Explicit list of subfolders, null if HasAllSubFolders is true
    /// </summary>
    public List<FolderDto>? SubFolders { get; set; }
    
}