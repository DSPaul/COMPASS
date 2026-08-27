using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Operations;

/// <summary>
/// Domain operations for <see cref="CodexCollection"/>
/// </summary>
public class CodexCollectionOperations(
    IUserFilesStorageService userFilesStorageService,
    ICoverStorageService coverStorageService,
    ILogger logger)
{
    /// <summary>
    /// Merges all codices & tags from the source collection into the target collection.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="separateTags">Whether to separate tags into a group named after the source collection.</param>
    public void Merge(CodexCollection source, CodexCollection target, bool separateTags = false)
    {
        //Merge Tags
        if (separateTags)
        {
            var rootTag = new Tag(source.AllTags)
            {
                IsGroup = true,
                Name = source.Name.Trim('_'),
                Children = new(source.RootTags)
            };
            source.RootTags = [rootTag];
        }
        target.AddTags(source.RootTags);

        //merge codices (with file moves)
        CopyCodicesToTarget(source, target);

        //merge info
        target.Info.MergeWith(source.Info);
    }

    private void CopyCodicesToTarget(CodexCollection source, CodexCollection target)
    {
        bool canImportFiles = false;
        if (userFilesStorageService.HasUserFiles(source))
        {
            canImportFiles = userFilesStorageService.EnsureDirectoryExists(target);
            if (!canImportFiles)
            {
                logger.Warn("The files referenced by the import items could not be copied.");
            }
        }

        foreach (Codex codex in source.AllCodices)
        {
            //Give it a new id that is unique to this collection
            codex.Id = Utils.GetAvailableId(target.AllCodices);

            //Move thumbnail and cover
            coverStorageService.MoveCodexDataToCollection(codex, target);

            //move user files included in import
            if (canImportFiles)
            {
                userFilesStorageService.MoveCodexDataToCollection(codex, target, source, copy: true);
            }
            target.AllCodices.Add(codex);
        }
    }
}
