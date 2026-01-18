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
using System.Text.Json;
using Notification = COMPASS.Common.Models.Notification;

namespace COMPASS.Common.Services.Storage;

public class ImportExportService(
    IEnvironmentVarsService environmentVarsService,
    IFilesService filesService,
    IIOService ioService,
    INotificationService windowedNotificationService)
    : IImportExportService
{
    private string _collectionsPath = Path.Combine(environmentVarsService.CompassDataPath, "Collections");

    private const string CodicesFileName = "CodexInfo.xml";
    private const string TagsFileName = "Tags.xml";
    private const string CollectionInfoFileName = "CollectionInfo.xml";

    #region Import

    /// <summary>
    /// Unpack the satchel at the given location
    /// </summary>
    /// <param name="satchelPath"></param>
    /// <returns> The collection id which is also the name of the extracted folder </returns>
    public async Task<string?> OpenSatchel(string? satchelPath = null)
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
            satchelPath = file.Path.AbsolutePath;
        }

        //Check compatibility
        using (ZipArchive archive = ZipArchive.Open(satchelPath))
        {
            var satchelInfoFile = archive.Entries.SingleOrDefault(entry => entry.Key == Constants.SatchelInfoFileName);
            if (satchelInfoFile == null)
            {
                //No version information means we cannot ensure compatibility, so abort
                string message =
                    $"Cannot import {Path.GetFileName(satchelPath)} because it does not contain version info, and might therefor not be compatible with your version v{ApplicationService.Version}.";
                Logger.Warn(message);
                Notification warnNotification = new($"Could not import {Path.GetFileName(satchelPath)}", message, Severity.Warning);
                await windowedNotificationService.ShowDialog(warnNotification);
                return null;
            }

            //Read the file contents
            using var stream = new MemoryStream();
            satchelInfoFile.WriteTo(stream);
            stream.Seek(0, SeekOrigin.Begin);
            using StreamReader reader = new(stream);
            string json = await reader.ReadToEndAsync();

            var satchelInfo = JsonSerializer.Deserialize<SatchelInfo>(json);
            if (satchelInfo == null)
            {
                //No version information means we cannot ensure compatibility, so abort
                string message =
                    $"Cannot import {Path.GetFileName(satchelPath)} because it does not contain version info, and might therefor not be compatible with your version v{ApplicationService.Version}.";
                Logger.Warn(message);
                Notification warnNotification = new($"Could not import {Path.GetFileName(satchelPath)}", message, Severity.Warning);
                await windowedNotificationService.ShowDialog(warnNotification);
                return null;
            }

            SemanticVersion currentVersion = SemanticVersion.Parse(ApplicationService.Version);
            var minVersions = new List<SemanticVersion> { currentVersion }; //keep a list of min requirements

            var filesInZip = archive.Entries.Select(entry => entry.Key).ToList();

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
            return Path.GetFileName(unzipLocation);
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
        string tmpCollectionPath = Path.Combine(_collectionsPath, $"__{fileName}");

        //make sure any previous temp data is gone
        ioService.ClearTmpData(tmpCollectionPath);

        //unzip the file to tmp folder
        using ZipArchive archive = ZipArchive.Open(zipFile);

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

            using var archive = ZipArchive.Create();

            //Add the files themselves as well as cover art if requested
            if (includeFiles)
            {
                AddUserFilesToArchive(collection, archive);
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
                AddCoversToArchive(collection, archive);
            }

            //Now add metadata files
            AddCollectionToArchive(archive, collection);

            //Add the version so we can check compatibility when importing
            SatchelInfo info = new();
            archive.AddEntry(Constants.SatchelInfoFileName, new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(info)));

            //Prepare progress reporting
            progressVM.Text = "Exporting Collection";
            progressVM.ShowCount = false;
            progressVM.ResetCounter();

            //Write archive
            var writerOptions = new SharpCompress.Writers.WriterOptions(CompressionType.None)
            {
                LeaveStreamOpen = false,
                Progress = progressVM
            };
            var stream = await file.OpenWriteAsync();
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
        using var archive = ZipArchive.Create();

        //Add Tags
        using (var stream = new MemoryStream())
        {
            var tags = repo.SaveTags(collection, stream);
            archive.AddEntry(TagsFileName, stream);
        }

        //write archive
        var options = new SharpCompress.Writers.WriterOptions(CompressionType.None)
        {
            LeaveStreamOpen = false
        };

        var targetStream = await selectedFile.OpenWriteAsync();
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
            System.IO.Compression.ZipFile.CreateFromDirectory(_collectionsPath, zipPath,
                System.IO.Compression.CompressionLevel.Optimal, true);
        }
        catch (Exception ex)
        {
            Logger.Error("Backup failed", ex);
        }
    }

    private void AddCollectionToArchive(ZipArchive archive, CodexCollection collection)
    {
        var repo = ServiceResolver.ResolveKeyed<ICodexCollectionRepository>(StorageStrategy.Xml);

        //Add codices
        var codicesStream = new MemoryStream();
        repo.SaveCodices(collection, codicesStream);
        archive.AddEntry(CodicesFileName, codicesStream);

        //Add Tags
        var tagsStream = new MemoryStream();
        repo.SaveTags(collection, tagsStream);
        archive.AddEntry(TagsFileName, tagsStream);

        //Add Info
        var infoStream = new MemoryStream();
        repo.SaveInfo(collection, infoStream);
        archive.AddEntry(CollectionInfoFileName, infoStream);
    }
    
    private static void AddUserFilesToArchive(CodexCollection collection, ZipArchive archive)
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
                archive.AddEntry(Path.Combine("Files", relativePath), codex.Sources.Path);
            }

            //keep the relative path, will be used during import to link the included files
            codex.Sources.Path = relativePath;
        }
    }

    private static void AddCoversToArchive(CodexCollection collection, ZipArchive archive)
    {
        var itemsWithCovers = collection.AllCodices
            .Where(codex => codex.CoverArtPath != null && File.Exists(codex.CoverArtPath))
            .ToList();
        string commonFolder = PathUtils.GetCommonFolder(itemsWithCovers.Select(codex => codex.CoverArtPath!).ToList());
        foreach (Codex codex in itemsWithCovers)
        {
            string relativePath = codex.CoverArtPath![commonFolder.Length..].TrimStart(Path.DirectorySeparatorChar);
            //Add the file
            archive.AddEntry(Path.Combine("CoverArt", relativePath), codex.CoverArtPath!);
            //keep the relative path, will be used during import to link the included files
            codex.CoverArtPath = relativePath;
        }
    }

    #endregion
}