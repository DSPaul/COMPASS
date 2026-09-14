using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Operations;

/// <summary>
/// Domain operations for <see cref="CodexCollection"/>
/// </summary>
public class CodexCollectionOperations(
    IUserFilesStorageService userFilesStorageService,
    ICoverStorageService coverStorageService,
    ICoverService coverService,
    ILogger logger,
    Lazy<CollectionManager> collectionManager,
    CodexOperations codexOperations)
{
    /// <summary>
    /// Merges all codices & tags from the source collection into the target collection.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="separateTags">Whether to separate tags into a group named after the source collection.</param>
    public void Merge(CodexCollection source, CodexCollection target, bool separateTags = false)
    {
        if(source == target)
        {
            logger.Warn("Attempted to merge a collection into itself. Operation aborted."); 
            return;
        }

        //Deep-copy tags so source and target never share Tag instances.
        List<Tag> rootsToImport = TagOperations.DeepCloneTags(source.RootTags, out Dictionary<Tag, Tag> tagOrigToClone);
        if (separateTags)
        {
            var rootTag = new Tag
            {
                IsGroup = true,
                Name = source.Name.Trim('_')
            };
            foreach (Tag clonedRoot in rootsToImport)
            {
                clonedRoot.Parent = rootTag;
                rootTag.Children.Add(clonedRoot);
            }
            rootsToImport = [rootTag];
        }
        target.AddTags(rootsToImport);

        //merge codices (with file copies)
        CopyCodicesToTarget(source, target, tagOrigToClone);

        //merge info
        target.Info.MergeWith(source.Info);
    }

    private void CopyCodicesToTarget(CodexCollection source, CodexCollection target, Dictionary<Tag, Tag> tagMap)
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

        HashSet<Tag> unmappedTags = [];
        foreach (Codex codex in source.AllCodices)
        {
            var copyCodex = new Codex(target);
            copyCodex.CopyFrom(codex);
            
            //replace tags from old collection to the clone in new collection
            copyCodex.Tags.Clear();
            foreach (Tag tag in codex.Tags)
            {
                if(tagMap.TryGetValue(tag, out Tag? clone))
                {
                    copyCodex.Tags.Add(clone);
                }
                else
                {
                    unmappedTags.Add(tag);
                }
            }

            //Give it a new id that is unique to this collection
            copyCodex.Id = Utils.GetAvailableId(target.AllCodices);
            copyCodex.GlobalId = Guid.NewGuid();

            //Copy thumbnail and cover
            coverStorageService.MoveCodexDataToCollection(copyCodex, target, copy: true);

            //Copy user files included in import
            if (canImportFiles)
            {
                userFilesStorageService.MoveCodexDataToCollection(copyCodex, target, source, copy: true);
            }

            target.AllCodices.Add(copyCodex);
        }

        if (unmappedTags.Count > 0)
        {
            logger.Warn($"Copied {source.AllCodices.Count} items from {source.Name} to {target.Name}, " +
                $"but {unmappedTags.Count} tags had no mapping and were dropped from the copies: " +
                $"{string.Join(", ", unmappedTags.Select(tag => tag.Name))}.");
        }
    }

    public Codex CreateNewCodex(CodexCollection collection)
    {
        var codex = new Codex(collection);
        codex.Id = Utils.GetAvailableId(collection.AllCodices);
        coverStorageService.InitCodexImagePaths(codex);
        return codex;
    }

    public async Task ImportFilesAsync(IList<string> paths, string targetCollectionId)
    {
        using CollectionHandle targetCollectionHandle = collectionManager.Value.LoadCollection(targetCollectionId)
                                                        ?? throw new LoadException(targetCollectionId);
        var targetCollection = targetCollectionHandle.CollectionVM.Collection;

        //filter out codices already in collection & banned paths
        IEnumerable<string> existingPaths = targetCollection.AllCodices.Select(codex => codex.Sources.Path);
        var sourceSets = paths
            .Except(existingPaths)
            .Except(targetCollection.Info.BanishedPaths)
            .Select(path => new SourceSet()
            {
                Path = path,
            })
            .ToList();

        await CreateCodicesAsync(sourceSets, targetCollectionId);
    }

    public async Task CreateCodicesAsync(IList<SourceSet> sourceSets, string? targetCollectionId = null)
    {
        targetCollectionId ??= TabsViewModel.GetInstance().ActiveTab?.CollectionVM.Identifier
                               ?? throw new NoTabException("There is no open tab, so no collection to import the items to");

        using CollectionHandle targetCollectionHandle = collectionManager.Value.LoadCollection(targetCollectionId)
                                                        ?? throw new LoadException(targetCollectionId);
        var targetCollection = targetCollectionHandle.CollectionVM.Collection;

        var progressVM = ProgressViewModel.GetInstance();

        progressVM.TotalAmount = sourceSets.Count;
        progressVM.ResetCounter();
        progressVM.Text = "Importing new items...";

        if (sourceSets.Count == 0) return;

        List<Codex> newCodices = [];

        //make new codices synchronously so they all have a valid ID
        foreach (var sourceSet in sourceSets)
        {
            try
            {
                ProgressViewModel.GlobalCancellationTokenSource.Token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                ProgressViewModel.GetInstance().ConfirmCancellation();
                break;
            }

            Codex newCodex = CreateNewCodex(targetCollection);
            newCodex.Sources = sourceSet;
            newCodices.Add(newCodex);
            targetCollection.AllCodices.Add(newCodex);

            LogEntry logEntry = new(Severity.Info, $"Importing {sourceSet}");
            progressVM.IncrementCounter();
            progressVM.AddLogEntry(logEntry);
        }

        targetCollection.Save();

        //now get metadata and cover async
        try
        {
            await codexOperations.StartGetMetaDataProcess(newCodices);
                await coverService.GetAndApplyCover(newCodices);
        }
        catch (OperationCanceledException ex)
        {
            logger.Warn("Import has been cancelled", ex);
            await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
            return;
        }

        foreach (Codex codex in newCodices)
        {
            codex.NotifyCoverChanged();
        }
    }
}
