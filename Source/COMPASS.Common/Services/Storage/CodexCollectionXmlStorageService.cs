using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Autofac.Features.AttributeFilters;
using Avalonia.Platform.Storage;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Operations;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using Notification = COMPASS.Common.Models.Notification;

namespace COMPASS.Common.Services.Storage;

public class CodexCollectionXmlStorageService : ICodexCollectionStorageService
{
    public CodexCollectionXmlStorageService(
        IEnvironmentVarsService environmentVarsService,
        [KeyFilter(NotificationDisplayType.Windowed)] INotificationService windowedNotificationService,
        IFilesService filesService)
    {
        _environmentVarsService = environmentVarsService;
        _filesService = filesService;
        _windowedNotificationService = windowedNotificationService;

        _collectionsPath = Path.Combine(environmentVarsService.CompassDataPath, "Collections");
    }

    private readonly INotificationService _windowedNotificationService;
    private readonly IEnvironmentVarsService _environmentVarsService;
    private readonly IFilesService _filesService;

    private string _collectionsPath;
    private readonly Lock _codicesLocker = new();
    private readonly Lock _tagsLocker = new();
    private readonly Lock _infoLocker = new();

    private const string CodicesFileName = "CodexInfo.xml";
    private const string TagsFileName = "Tags.xml";
    private const string CollectionInfoFileName = "CollectionInfo.xml";

    public string CollectionDataPath(string collectionName) => Path.Combine(_collectionsPath, collectionName);
    public string CodicesDataFilePath(string collectionName) => Path.Combine(CollectionDataPath(collectionName), CodicesFileName);
    public string TagsDataFilePath(string collectionName) => Path.Combine(CollectionDataPath(collectionName), TagsFileName);
    public string CollectionInfoFilePath(string collectionName) => Path.Combine(CollectionDataPath(collectionName), CollectionInfoFileName);

    public async Task AllocateNewCollection(CodexCollection collection)
    {
        collection.LoadedCodices = true;
        collection.LoadedInfo = true;
        collection.LoadedTags = true;

        await CreateDirectories(collection.Name);
    }

    public IList<CodexCollection> GetAllCollections()
    {
        try
        {
            //Get all collections by folder name
            return Directory
                .GetDirectories(_collectionsPath)
                .Select(dir => Path.GetFileName(dir))
                .Where(dir => CodexCollectionOperations.IsLegalCollectionName(dir, []))
                .Select(dir => new CodexCollection(dir))
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to find existing collections in {_collectionsPath}", ex);
            return [];
        }
    }

    public void EnsureDirectoryExists()
    {
        while (!Directory.Exists(_collectionsPath))
        {
            try
            {
                Directory.CreateDirectory(_collectionsPath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create folder to store user data, so data cannot be saved", ex);
                string msg = $"Failed to create a folder to store user data at {_environmentVarsService.CompassDataPath}, " +
                             $"please pick a new location to save your data. Creation failed with the following error {ex.Message}";
                IOService.AskNewCompassDataPath(msg).Wait();
                _collectionsPath = Path.Combine(_environmentVarsService.CompassDataPath, "Collections");
            }
        }
    }

    /// <summary>
    /// Rename the directory to match the new collection name
    /// </summary>
    /// <param name="oldName"></param>
    /// <param name="newName"></param>
    public void OnCollectionRenamed(string oldName, string newName)
    {
        try
        {
            Directory.Move(Path.Combine(_collectionsPath, oldName), Path.Combine(_collectionsPath, newName));
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to move data files from {oldName} to {newName}", ex);
        }
    }
    
    #region Load Data From File

    /// <summary>
    /// Loads the collection and unless MakeStartupCollection, sets it as the new default to load on startup
    /// </summary>
    /// <returns>int that gives status: 0 for success, -1 for failed tags, -2 for failed codices, -4 for failed info, or combination of those</returns>
    public int Load(CodexCollection collection)
    {
        int result = 0;
        bool loadedTags = LoadTags(collection);
        bool loadedCodices = LoadCodices(collection);
        bool loadedInfo = LoadInfo(collection);
        if (!loadedTags)
        {
            result -= 1;
        }

        if (!loadedCodices)
        {
            result -= 2;
        }

        if (!loadedInfo)
        {
            result -= 4;
        }

        return result;
    }

    //Loads the RootTags from a file and constructs the AllTags list from it
    public bool LoadTags(CodexCollection collection)
    {
        List<TagDto> loadedTags = [];
        if (File.Exists(TagsDataFilePath(collection.Name)))
        {
            lock (_tagsLocker) //lock so file cannot change while we are reading it
            {
                using var reader = new StreamReader(TagsDataFilePath(collection.Name));
                XmlSerializer serializer = GetSerializer(typeof(List<TagDto>));
                try
                {
                    loadedTags = serializer.Deserialize(reader) as List<TagDto> ?? [];
                }
                catch (Exception ex)
                {
                    Logger.Error($"Could not load {TagsDataFilePath(collection.Name)}.", ex);
                    return false;
                }
            }
        }

        collection.RootTags = loadedTags.Select(dto => dto.ToModel()).ToList();

        collection.LoadedTags = true;
        return true;
    }

    //Loads AllCodices list from Files
    public bool LoadCodices(CodexCollection collection)
    {
        //Tags should be loaded before codices
        Debug.Assert(collection.LoadedTags);

        CodexDto[] dtos = [];
        if (File.Exists(CodicesDataFilePath(collection.Name)))
        {
            lock (_codicesLocker) //lock so file cannot change while we are reading it
            {
                using var reader = new StreamReader(CodicesDataFilePath(collection.Name));
                XmlSerializer serializer = GetSerializer(typeof(CodexDto[]));
                try
                {
                    dtos = serializer.Deserialize(reader) as CodexDto[] ?? [];
                }
                catch (Exception ex)
                {
                    Logger.Error($"Could not load {CodicesDataFilePath(collection.Name)}", ex);
                    return false;
                }
            }
        }

        collection.AllCodices = new(dtos.Select(dto => dto.ToModel(collection)));

        collection.LoadedCodices = true;
        return true;
    }

    public bool LoadInfo(CodexCollection collection)
    {
        Debug.Assert(collection.LoadedTags);

        if (File.Exists(CollectionInfoFilePath(collection.Name)))
        {
            lock (_infoLocker) //lock so file cannot change while we are reading it
            {
                try
                {
                    using var reader = new StreamReader(CollectionInfoFilePath(collection.Name));
                    XmlSerializer serializer = GetSerializer(typeof(CollectionInfoDto));
                    if (serializer.Deserialize(reader) is not CollectionInfoDto loadedInfo)
                    {
                        Logger.Warn($"Could not load info for {CollectionInfoFilePath(collection.Name)}");
                        return false;
                    }

                    collection.Info = loadedInfo.ToModel(collection.AllTags);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Could not load info for {CollectionInfoFilePath(collection.Name)}", ex);
                    return false;
                }
            }
        }
        else
        {
            collection.Info = new();
        }

        collection.LoadedInfo = true;
        return true;
    }

    private XmlSerializer GetSerializer(Type type)
    {
        //Obsolete properties should still be deserialized for backwards compatibility
        var overrides = new XmlAttributeOverrides();
        var obsoleteAttributes = new XmlAttributes { XmlIgnore = false };
        var obsoleteProperties = Reflection.GetObsoleteProperties(type);
        foreach (string prop in obsoleteProperties)
        {
            overrides.Add(type, prop, obsoleteAttributes);
        }
        
        return new(type, overrides);
    }

    #endregion

    #region Save Data To XML File

    public async Task CreateDirectories(string collectionName)
    {
        try
        {
            Directory.CreateDirectory(CollectionDataPath(collectionName));
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to create folder to store user data for this collection.", ex);

            string msg = $"Failed to create the necessary folders to store data about this collection. The following error occured";
            Notification failedFolderCreation = new("Failed to save collection", msg, Severity.Error)
            {
                Details = ex.ToString()
            };
            await _windowedNotificationService.Show(failedFolderCreation);
        }
    }

    public bool Save(CodexCollection collection)
    {
        try
        {
            Directory.CreateDirectory(CollectionDataPath(collection.Name));
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to create the folder to save the data for this collection", ex);
            return false;
        }

        bool savedTags = SaveTags(collection);
        bool savedCodices = SaveCodices(collection);
        bool savedInfo = SaveInfo(collection);

        if (savedCodices || savedTags || savedInfo)
        {
            Logger.Info($"Saved {collection.Name}");
            return true;
        }

        return false;
    }

    public bool SaveTags(CodexCollection collection)
    {
        if (!collection.LoadedTags)
        {
            //Should always load a collection before it can be saved
            return false;
        }

        var toSave = collection.RootTags.Select(c => c.ToDto()).ToList();

        try
        {
            string tempFileName = TagsDataFilePath(collection.Name) + ".tmp";

            lock (_tagsLocker)
            {
                using (var writer = XmlWriter.Create(tempFileName, XmlService.XmlWriteSettings))
                {
                    XmlSerializer serializer = new(typeof(List<TagDto>));
                    serializer.Serialize(writer, toSave);
                }

                //if successfully written to the tmp file, move to actual path
                File.Move(tempFileName, TagsDataFilePath(collection.Name), true);
                File.Delete(tempFileName);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Error($"Access denied when trying to save Tags to {TagsDataFilePath(collection.Name)}", ex);
            return false;
        }
        catch (IOException ex)
        {
            Logger.Error($"IO error occurred when saving Tags to {TagsDataFilePath(collection.Name)}", ex);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save Tags to {TagsDataFilePath(collection.Name)}", ex);
            return false;
        }

        return true;
    }

    public bool SaveCodices(CodexCollection collection)
    {
        if (!collection.LoadedCodices)
        {
            //Should always load a collection before it can be saved
            return false;
        }

        var toSave = collection.AllCodices.Select(c => c.ToDto()).ToList();

        try
        {
            string tempFileName = CodicesDataFilePath(collection.Name) + ".tmp";

            lock (_codicesLocker)
            {
                using (var writer = XmlWriter.Create(tempFileName, XmlService.XmlWriteSettings))
                {
                    XmlSerializer serializer = new(typeof(List<CodexDto>));
                    serializer.Serialize(writer, toSave);
                }

                //if successfully written to the tmp file, move to actual path
                File.Move(tempFileName, CodicesDataFilePath(collection.Name), true);
                File.Delete(tempFileName);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Error($"Access denied when trying to save Codex Info to {CodicesDataFilePath(collection.Name)}", ex);
            return false;
        }
        catch (IOException ex)
        {
            Logger.Error($"IO error occurred when saving Codex Info to {CodicesDataFilePath(collection.Name)}", ex);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save Codex Info to {CodicesDataFilePath(collection.Name)}", ex);
            return false;
        }

        return true;
    }

    public bool SaveInfo(CodexCollection collection)
    {
        if (!collection.LoadedInfo)
        {
            //Should always load a collection before it can be saved
            return false;
        }

        try
        {
            string tempFileName = CollectionInfoFilePath(collection.Name) + ".tmp";

            lock (_infoLocker)
            {
                using (var writer = XmlWriter.Create(tempFileName, XmlService.XmlWriteSettings))
                {
                    XmlSerializer serializer = new(typeof(CollectionInfoDto));
                    serializer.Serialize(writer, collection.Info.ToDto());
                }

                //if successfully written to the tmp file, move to actual path
                File.Move(tempFileName, CollectionInfoFilePath(collection.Name), true);
                File.Delete(tempFileName);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Error($"Access denied when trying to save Collection Info to {CollectionInfoFilePath(collection.Name)}", ex);
            return false;
        }
        catch (IOException ex)
        {
            Logger.Error($"IO error occurred when saving Collection Info to {CollectionInfoFilePath(collection.Name)}", ex);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save Collection Info to {CollectionInfoFilePath(collection.Name)}", ex);
            return false;
        }

        return true;
    }

    #endregion

    #region Import

    public async Task<CodexCollection?> OpenSatchel(string? satchelPath = null)
    {
        FilePickerOpenOptions options = new()
        {
            FileTypeFilter = [_filesService.SatchelExtensionFilter],
            AllowMultiple = false,
            Title = "Choose a COMPASS Satchel file to import",
        };

        if (satchelPath == null)
        {
            //ask for satchel file using fileDialog
            var files = await _filesService.OpenFilesAsync(options);

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
                    $"Cannot import {Path.GetFileName(satchelPath)} because it does not contain version info, and might therefor not be compatible with your version v{Reflection.Version}.";
                Logger.Warn(message);
                Notification warnNotification = new($"Could not import {Path.GetFileName(satchelPath)}", message, Severity.Warning);
                await _windowedNotificationService.Show(warnNotification);
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
                    $"Cannot import {Path.GetFileName(satchelPath)} because it does not contain version info, and might therefor not be compatible with your version v{Reflection.Version}.";
                Logger.Warn(message);
                Notification warnNotification = new($"Could not import {Path.GetFileName(satchelPath)}", message, Severity.Warning);
                await _windowedNotificationService.Show(warnNotification);
                return null;
            }

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version!;
            var minVersions = new List<Version> { currentVersion }; //keep a list of min requirements

            var filesInZip = archive.Entries.Select(entry => entry.Key).ToList();

            //Check Codex version
            if (filesInZip.Contains(CodicesFileName))
            {
                Version minCodexVersion = Version.Parse(satchelInfo.MinCodexInfoVersion);
                minVersions.Add(minCodexVersion);
            }

            //Check tags version
            if (filesInZip.Contains(TagsFileName))
            {
                Version minVersion = Version.Parse(satchelInfo.MinTagsVersion);
                minVersions.Add(minVersion);
            }

            //Check collection info version
            if (filesInZip.Contains(CollectionInfoFileName))
            {
                Version minVersion = Version.Parse(satchelInfo.MinCollectionInfoVersion);
                minVersions.Add(minVersion);
            }

            //current version must exceed all min versions
            if (minVersions.Max() > currentVersion)
            {
                string message =
                    $"Cannot import {Path.GetFileName(satchelPath)} because it was created in a newer version of COMPASS (v{satchelInfo.CreationVersion}), " +
                    $"and has indicated to be incompatible with your version v{Reflection.Version}. Please update and try again.";
                Logger.Warn(message);
                Notification warnNotification = new($"Could not import {Path.GetFileName(satchelPath)}", message,
                    Severity.Warning);
                await _windowedNotificationService.Show(warnNotification);
                return null;
            }
        }

        //unzip the file
        try
        {
            string unzipLocation = await UnZipCollection(satchelPath);
            return new(Path.GetFileName(unzipLocation));
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
        IOService.ClearTmpData(tmpCollectionPath);

        //unzip the file to tmp folder
        using ZipArchive archive = ZipArchive.Open(zipFile);

        //report progress
        var progressVM = ProgressViewModel.GetInstance();
        progressVM.Text = $"Reading {zipFile}";
        progressVM.ResetCounter();
        progressVM.TotalAmount = 1;

        //extract
        await Task.Run(() =>
            archive.ExtractToDirectory(tmpCollectionPath, progressReport: progressVM.UpdateFromPercentage));

        progressVM.IncrementCounter();
        return tmpCollectionPath;
    }

    #endregion

    #region Export

    public async Task ExportTags(CodexCollection collection)
    {
        var savedFile = await _filesService.SaveFileAsync(new()
        {
            FileTypeChoices = [_filesService.SatchelExtensionFilter],
            SuggestedFileName = $"{collection.Name}_Tags",
            DefaultExtension = Constants.SatchelExtension,
        });

        if (savedFile == null) return;

        //make sure to save first
        SaveTags(collection);

        string targetPath = savedFile.Path.AbsolutePath;
        savedFile.Dispose();
        using var archive = ZipArchive.Create();
        archive.AddEntry(TagsFileName, TagsDataFilePath(collection.Name));

        //Export
        archive.SaveTo(targetPath, CompressionType.None);
        Logger.Info($"Exported Tags from {collection.Name} to {targetPath}");
    }

    public void AddCollectionToArchive(ZipArchive archive, CodexCollection collection)
    {
        archive.AddAllFromDirectory(CollectionDataPath(collection.Name));
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
    
    #endregion
    
    #region Delete

    public void OnCollectionDeleted(CodexCollection toDelete)
    {
        //if Dir name of toDelete is empty, it will delete the entire collections folder
        if (string.IsNullOrEmpty(toDelete.Name)) return;
        if (Directory.Exists(CollectionDataPath(toDelete.Name))) //does not exist if collection was never saved
        {
            Directory.Delete(CollectionDataPath(toDelete.Name), true);
        }
    }

    #endregion
}