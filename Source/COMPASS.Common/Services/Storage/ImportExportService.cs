using Avalonia.Platform.Storage;
using COMPASS.Common.Interfaces.Repos;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using COMPASS.Infra.Tools;
using NuGet.Versioning;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers.Zip;
using System.Text.Json;
using Constants = COMPASS.Common.Models.Constants;
using Notification = COMPASS.Common.Models.Notification;

namespace COMPASS.Common.Services.Storage;

public class ImportExportService(
    IApplicationDataService applicationDataService,
    IFilesService filesService,
    IIOService ioService,
    INotificationService windowedNotificationService)
    : IImportExportService
{
    private const string CodicesFileName = "CodexInfo.xml";
    private const string TagsFileName = "Tags.xml";
    private const string CollectionInfoFileName = "CollectionInfo.xml";

    #region Import

    public async Task<CodexCollection?> OpenSatchel(string? satchelPath = null)
    {
        FilePickerOpenOptions options = new()
        {
            FileTypeFilter = [filesService.SatchelExtensionFilter],
            AllowMultiple = false,
            Title = "Choose a COMPASS Satchel file to import",
        };

        if (satchelPath == null)
        {
            //ask for satchel file using fileDialog
            var files = await filesService.OpenFilesAsync(options);

            if (!files.Any()) return null;
            using var file = files.Single();
            satchelPath = file.Path.LocalPath;
        }

        string satchelName = Path.GetFileName(satchelPath);

        //Check compatibility
        await using (var archive = await ZipArchive.OpenAsyncArchive(satchelPath))
        {
            var satchelInfoFile = await archive.EntriesAsync.SingleOrDefaultAsync(entry => entry.Key == Constants.SatchelInfoFileName);
            if (satchelInfoFile == null)
            {
                //No version information means we cannot ensure compatibility, so abort
                string message =
                    $"Cannot import {satchelName} because it does not contain version info, and might therefor not be compatible with your version v{ApplicationService.Version}.";
                Logger.Warn(message);
                Notification warnNotification = new($"Could not import {satchelName}", message, Severity.Warning);
                await windowedNotificationService.ShowDialog(warnNotification);
                return null;
            }

            //Read the file contents
            string json = string.Empty;
            using (var stream = new MemoryStream())
            {
                satchelInfoFile.WriteTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
                using StreamReader reader = new(stream);
                json = await reader.ReadToEndAsync();
            }

            var satchelInfo = JsonSerializer.Deserialize<SatchelInfo>(json);
            if (satchelInfo == null)
            {
                //No version information means we cannot ensure compatibility, so abort
                string message =
                    $"Cannot import {satchelName} because it does not contain version info, and might therefor not be compatible with your version v{ApplicationService.Version}.";
                Logger.Warn(message);
                Notification warnNotification = new($"Could not import {satchelName}", message, Severity.Warning);
                await windowedNotificationService.ShowDialog(warnNotification);
                return null;
            }

            SemanticVersion currentVersion = SemanticVersion.Parse(ApplicationService.Version);
            var minVersions = new List<SemanticVersion> { currentVersion }; //keep a list of min requirements

            var filesInZip = await archive.EntriesAsync.Select(entry => entry.Key).ToListAsync();

            //Check Codex version
            if (filesInZip.Contains(CodicesFileName))
            {
                SemanticVersion minCodexVersion = SemanticVersion.Parse(satchelInfo.MinCodexInfoVersion);
                minVersions.Add(minCodexVersion);
            }

            //Check tags version
            if (filesInZip.Contains(TagsFileName))
            {
                SemanticVersion minVersion = SemanticVersion.Parse(satchelInfo.MinTagsVersion);
                minVersions.Add(minVersion);
            }

            //Check collection info version
            if (filesInZip.Contains(CollectionInfoFileName))
            {
                SemanticVersion minVersion = SemanticVersion.Parse(satchelInfo.MinCollectionInfoVersion);
                minVersions.Add(minVersion);
            }

            //current version must exceed all min versions
            if (minVersions.Any() && minVersions.Max()! > currentVersion)
            {
                string message =
                    $"Cannot import {Path.GetFileName(satchelPath)} because it was created in a newer version of COMPASS (v{satchelInfo.CreationVersion}), " +
                    $"and has indicated to be incompatible with your version v{ApplicationService.Version}. Please update and try again.";
                Logger.Warn(message);
                Notification warnNotification = new($"Could not import {Path.GetFileName(satchelPath)}", message,
                    Severity.Warning);
                await windowedNotificationService.ShowDialog(warnNotification);
                return null;
            }
        }

        //unzip the file
        try
        {
            string unzipLocation = await UnZipCollection(satchelPath);
            var identifier = Path.GetFileName(unzipLocation);

            //Load collection once
            CodexCollection collection = new CodexCollection(identifier);
            var xmlservice = ServiceResolver.ResolveKeyed<ICodexCollectionRepository>(StorageStrategy.Xml);
            xmlservice.Load(collection);

            //Image paths need to be updated to point to the dir where the files where extracted
            var thumbnailService = ServiceResolver.Resolve<ICoverStorageService>();
            foreach(var codex in collection.AllCodices)
            {
                thumbnailService.InitCodexImagePaths(codex);
            }

            xmlservice.SaveCodices(collection);

            return collection;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to read {satchelPath}", ex);
            return null;
        }
    }

    /// <summary>
    /// Unzips a collection stored in a satchel file
    /// </summary>
    /// <param name="zipFile">Path to the satchel file</param>
    /// <returns>Path to unzipped folder</returns>
    private async Task<string> UnZipCollection(string zipFile)
    {
        string fileName = Path.GetFileName(zipFile);
        string tmpCollectionPath = Path.Combine(applicationDataService.UserDataPath, Constants.DIR_COLLECTIONS, $"__{fileName}");

        //make sure any previous temp data is gone
        ioService.ClearTmpData(tmpCollectionPath);

        //unzip the file to tmp folder
        await using var archive = await ZipArchive.OpenAsyncArchive(zipFile);

        //report progress
        var progressVM = ProgressViewModel.GetInstance();
        progressVM.Text = $"Reading {zipFile}";
        progressVM.ResetCounter();

        //extract
        try
        {
            Directory.CreateDirectory(tmpCollectionPath);
            await archive.WriteToDirectoryAsync(tmpCollectionPath, progress: progressVM);
        }
        catch
        {
            progressVM.Clear();
            throw;
        }

        return tmpCollectionPath;
    }

    #endregion

    #region Export

    public async Task ExportCollection(CodexCollection collection, IStorageFile? file, bool includeFiles, bool includeCovers)
    {
        var progressVM = ProgressViewModel.GetInstance();

        try
        {
            if (file == null)
            {
                file = await filesService.SaveFileAsync(new()
                {
                    FileTypeChoices = [filesService.SatchelExtensionFilter],
                    SuggestedFileName = $"{collection.Name}",
                    DefaultExtension = Constants.SatchelExtension,
                });
                if (file == null) return;
            }

            await using var archive = await ZipArchive.CreateAsyncArchive();

            //Add the files themselves as well as cover art if requested
            if (includeFiles)
            {
                await AddUserFilesToArchive(collection, archive);
            }
            else
            {
                foreach(var codex in collection.AllCodices)
                {
                    //Clear the paths as they will point to files that aren't there anyway
                    codex.Sources.Path = string.Empty;
                }
            }

            if (includeCovers)
            {
                await AddCoversToArchive(collection, archive);
            }

            //Now add metadata files
            await AddCollectionToArchive(archive, collection);

            //Add the version so we can check compatibility when importing
            SatchelInfo info = new();
            using var infoStream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(info));
            await archive.AddEntryAsync(Constants.SatchelInfoFileName, infoStream);

            //Prepare progress reporting
            progressVM.Text = "Exporting Collection";
            progressVM.ShowCount = false;
            progressVM.ResetCounter();

            //Write archive
            var writerOptions = new ZipWriterOptions(CompressionType.None)
            {
                Progress = progressVM
            };
            await using var stream = await file.OpenWriteAsync();
            await archive.SaveToAsync(stream, writerOptions);

            Logger.Info($"Exported {collection.Name} to {file.TryGetLocalPath()}");
        }
        catch (Exception ex)
        {
            Logger.Error("Export failed", ex);
            progressVM.Clear();
        }
        finally
        {
            file?.Dispose();
        }
    }

    public async Task ExportTags(CodexCollection collection)
    {
        //satchel uses xml files 
        var repo = ServiceResolver.ResolveKeyed<ICodexCollectionRepository>(StorageStrategy.Xml);

        using var selectedFile = await filesService.SaveFileAsync(new()
        {
            FileTypeChoices = [filesService.SatchelExtensionFilter],
            SuggestedFileName = $"{collection.Name}_Tags",
            DefaultExtension = Constants.SatchelExtension,
        });

        if (selectedFile == null) return;

        //Create archive
        await using var archive = await ZipArchive.CreateAsyncArchive();

        //Add Tags
        using var stream = new MemoryStream();
        bool savedTags = repo.SaveTags(collection, stream);
        if (!savedTags)
        {
            Logger.Warn($"Failed to save tags for {collection.Name} during export");
            return;
        }
        await archive.AddEntryAsync(TagsFileName, stream);

        //write archive
        var options = new ZipWriterOptions(CompressionType.None);
        await using var targetStream = await selectedFile.OpenWriteAsync();
        await archive.SaveToAsync(targetStream, options);

        Logger.Info($"Exported Tags from {collection.Name} to {selectedFile.TryGetLocalPath()}");
    }

    public void CompressUserDataToZip(string zipPath)
    {
        //In caes zip already exists with same name, delete it first
        if (File.Exists(zipPath))
        {
            try
            {
                File.Delete(zipPath);
            }
            catch (Exception ex)
            {
                Logger.Error("A backup with the same name already exists and could not be removed", ex);
                return;
            }
        }

        try
        {
            //zip up collections, easiest with system.IO.Compression
            System.IO.Compression.ZipFile.CreateFromDirectory(applicationDataService.UserDataPath, zipPath,
                System.IO.Compression.CompressionLevel.Optimal, true);
        }
        catch (Exception ex)
        {
            Logger.Error("Backup failed", ex);
        }
    }

    private async Task AddCollectionToArchive(IWritableAsyncArchive<ZipWriterOptions> archive, CodexCollection collection)
    {
        var repo = ServiceResolver.ResolveKeyed<ICodexCollectionRepository>(StorageStrategy.Xml);

        //Add codices
        var codicesStream = new MemoryStream();
        repo.SaveCodices(collection, codicesStream);
        await archive.AddEntryAsync(CodicesFileName, codicesStream);

        //Add Tags
        var tagsStream = new MemoryStream();
        repo.SaveTags(collection, tagsStream);
        await archive.AddEntryAsync(TagsFileName, tagsStream);

        //Add Info
        var infoStream = new MemoryStream();
        repo.SaveInfo(collection, infoStream);
        await archive.AddEntryAsync(CollectionInfoFileName, infoStream);
    }
    
    private async static Task AddUserFilesToArchive(CodexCollection collection, IWritableAsyncArchive<ZipWriterOptions> archive)
    {
        //Change Codex Path to relative and add those files if the options is set
        var itemsWithOfflineSource = collection.AllCodices
            .Where(codex => codex.Sources.HasOfflineSource())
            .ToList();
        string commonFolder = PathUtils.GetCommonFolder(itemsWithOfflineSource.Select(codex => codex.Sources.Path).ToList());
        foreach (Codex codex in itemsWithOfflineSource)
        {
            string relativePath = codex.Sources.Path[commonFolder.Length..].TrimStart(Path.DirectorySeparatorChar);

            //Add the file
            if (File.Exists(codex.Sources.Path))
            {
                await archive.AddEntryAsync(Path.Combine("Files", relativePath), codex.Sources.Path);
            }

            //keep the relative path, will be used during import to link the included files
            codex.Sources.Path = relativePath;
        }
    }

    private async static Task AddCoversToArchive(CodexCollection collection, IWritableAsyncArchive<ZipWriterOptions> archive)
    {
        var itemsWithCovers = collection.AllCodices
            .Where(codex => codex.CoverArtPath != null && File.Exists(codex.CoverArtPath))
            .ToList();
        string commonFolder = PathUtils.GetCommonFolder(itemsWithCovers.Select(codex => codex.CoverArtPath!).ToList());
        foreach (Codex codex in itemsWithCovers)
        {
            string relativePath = codex.CoverArtPath![commonFolder.Length..].TrimStart(Path.DirectorySeparatorChar);
            //Add the file
            await archive.AddEntryAsync(Path.Combine("CoverArt", relativePath), codex.CoverArtPath!);
            //keep the relative path, will be used during import to link the included files
            codex.CoverArtPath = relativePath;
        }
    }

    #endregion
}