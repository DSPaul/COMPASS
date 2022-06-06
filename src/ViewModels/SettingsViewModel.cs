using COMPASS.Models;
using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml;

namespace COMPASS.ViewModels
{
    public class SettingsViewModel:BaseViewModel
    {
        public SettingsViewModel()
        { 
            //will need to download this when updating because it is one folder too high in the file hierarchy
            Changelog = File.ReadAllText(@"C:\Users\pauld\Documents\COMPASS\Changelog.md");

            LoadPreferences();
        }

        #region static fields
        public static XmlWriterSettings xmlWriterSettings = new XmlWriterSettings() { Indent = true };
        public static string CompassDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass";
        private static string PreferencesFilePath = CompassDataPath + @"\Preferences.xml";
        private static SerializablePreferences AllPreferences = new SerializablePreferences();
        #endregion

        #region Save and Load
        public void SavePreferences()
        {
            //Save OpenFilePriority
            AllPreferences.OpenFilePriorityIDs = OpenFilePriority.Select(pf => pf.ID).ToList();

            using (var writer = XmlWriter.Create(PreferencesFilePath, xmlWriterSettings))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(SerializablePreferences));
                serializer.Serialize(writer, AllPreferences);
            }
        }

        public void LoadPreferences()
        {
            using (var Reader = new StreamReader(PreferencesFilePath))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(SerializablePreferences));
                AllPreferences = serializer.Deserialize(Reader) as SerializablePreferences;
                Reader.Close();
            }
            //put openFilePriority in right order
            OpenFilePriority = new ObservableCollection<PreferableFunction>(OpenFileFunctions.OrderBy(pf => AllPreferences.OpenFilePriorityIDs.IndexOf(pf.ID)));
        }
        #endregion

        #region General
        //list with possible functions to open a file
        private List<PreferableFunction> OpenFileFunctions = new List<PreferableFunction>()
            {
                new PreferableFunction("Local File", FileBaseViewModel.OpenFileLocally,0),
                new PreferableFunction("Web Version", FileBaseViewModel.OpenFileOnline,1)
            };
        //same ordered version of the list
        private ObservableCollection<PreferableFunction> _OpenFilePriority;
        public ObservableCollection<PreferableFunction> OpenFilePriority
        {
            get { return _OpenFilePriority; }
            set { SetProperty(ref _OpenFilePriority, value); }
        }

        public void RegenAllThumbnails() { 
            foreach(Codex codex in MVM.CurrentCollection.AllFiles)
            {
                //codex.Thumbnail = codex.CoverArt.Replace("CoverArt", "Thumbnails");
                CoverArtGenerator.CreateThumbnail(codex);
            }
        }
        #endregion

        #region Changelog
        private string _Changelog;
        public string Changelog
        {
            get { return _Changelog; }
            set { SetProperty(ref _Changelog, value); }
        }
        #endregion
    }
}
