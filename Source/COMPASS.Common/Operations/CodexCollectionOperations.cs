using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
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
    ILogger logger,
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

    public Codex CreateNewCodex(CodexCollection collection)
    {
        var codex = new Codex(collection);
        codex.Id = Utils.GetAvailableId(collection.AllCodices);
        coverStorageService.InitCodexImagePaths(codex);
        return codex;
    }

    public async Task ImportFilesAsync(IList<string> paths, string? targetCollectionId = null)
    {
        targetCollectionId ??= TabsViewModel.GetInstance().ActiveTab?.CollectionVM.Identifier
                               ?? throw new NoTabException("There is no open tab, so no collection to import the files to");

        using CollectionHandle targetCollectionHandle = CollectionManager.LoadCollection(targetCollectionId)
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

        using CollectionHandle targetCollectionHandle = CollectionManager.LoadCollection(targetCollectionId)
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
            await CoverService.GetAndApplyCover(newCodices);
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
