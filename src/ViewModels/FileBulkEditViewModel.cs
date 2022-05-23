using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class FileBulkEditViewModel : BaseEditViewModel
    {
        public FileBulkEditViewModel(MainViewModel vm, List<Codex> ToEdit) : base(vm)
        {
            EditedFiles = ToEdit;
            TempFile = new Codex(CurrentCollection);

            //set common metadata
            if (EditedFiles.All(f => f.Author == EditedFiles[0].Author)) TempFile.Author = EditedFiles[0].Author;
            if (EditedFiles.All(f => f.Publisher == EditedFiles[0].Publisher)) TempFile.Publisher = EditedFiles[0].Publisher;
            if (EditedFiles.All(f => f.Rating == EditedFiles[0].Rating)) TempFile.Rating = EditedFiles[0].Rating;
            if (EditedFiles.All(f => f.Physically_Owned == EditedFiles[0].Physically_Owned)) TempFile.Physically_Owned = EditedFiles[0].Physically_Owned;
            if (EditedFiles.All(f => f.ReleaseDate == EditedFiles[0].ReleaseDate)) TempFile.ReleaseDate = EditedFiles[0].ReleaseDate;


            //commands
            SetTagModeCommand = new RelayCommand<object>(SetTagMode);
            TagCheckCommand = new BasicCommand(Update_Taglist);
        }

        #region Properties

        readonly List<Codex> EditedFiles;

        //True if adding tags, false if removing
        private bool _TagMode = true;
        public bool TagMode
        {
            get { return _TagMode; }
            set { SetProperty(ref _TagMode, value); }
        }


        private ObservableCollection<Tag> _TagsToAdd = new ObservableCollection<Tag>();
        private ObservableCollection<Tag> _TagsToRemove= new ObservableCollection<Tag>();

        public ObservableCollection<Tag> TagsToAdd
        {
            get { return _TagsToAdd; }
            set { SetProperty(ref _TagsToAdd, value); }
        }
        
        public ObservableCollection<Tag> TagsToRemove
        {
            get { return _TagsToRemove; }
            set { SetProperty(ref _TagsToRemove, value); }
        }

        private Codex _tempFile;
        public Codex TempFile
        {
            get { return _tempFile; }
            set { SetProperty(ref _tempFile, value); }
        }

        #endregion

        #region Funtions and Commands

        public RelayCommand<object> SetTagModeCommand { get; private set; }
        public bool SetTagMode(object o)
        {
            TagMode = (bool)o;

            // Apply right checkboxes in Alltags
            if (TagMode)
            {
                foreach (TreeViewNode t in AllTreeViewNodes)
                {
                    t.Expanded = false;
                    t.Selected = TagsToAdd.Contains(t.Tag) ? true : false;
                    if (t.Children.Any(node => TagsToAdd.Contains(node.Tag))) t.Expanded = true;
                }
            }

            else
            {
                foreach (TreeViewNode t in AllTreeViewNodes)
                {
                    t.Expanded = false;
                    t.Selected = TagsToRemove.Contains(t.Tag) ? true : false;
                    if (t.Children.Any(node => TagsToRemove.Contains(node.Tag))) t.Expanded = true;
                }
            }
            return true;
        }

        public BasicCommand TagCheckCommand { get; private set; }
        public void Update_Taglist()
        {
            if (TagMode) TagsToAdd.Clear();
            else TagsToRemove.Clear();
            foreach (TreeViewNode t in AllTreeViewNodes)
            {
                if (t.Selected)
                {
                    if(TagMode) TagsToAdd.Add(t.Tag);
                    else TagsToRemove.Add(t.Tag);
                }
            }
        }

        public override void OKBtn()
        {
            //Copy changes into each Codex
            if(TempFile.Author != "" && TempFile.Author!= null)
            {
                foreach(Codex f in EditedFiles)
                {
                    f.Author = TempFile.Author;
                }
            }

            if (TempFile.Publisher != "" && TempFile.Publisher != null)
            {
                foreach (Codex f in EditedFiles)
                {
                    f.Publisher = TempFile.Publisher;
                }
            }

            if(TempFile.Physically_Owned == true)
            {
                foreach (Codex f in EditedFiles)
                {
                    f.Physically_Owned = true;
                }
            }

            if(TempFile.Rating > 0)
            {
                foreach (Codex f in EditedFiles)
                {
                    f.Rating = TempFile.Rating;
                }
            }

            if (TempFile.Version != null)
            {
                foreach (Codex f in EditedFiles)
                {
                    f.Version = TempFile.Version;
                }
            }

            if(TempFile.ReleaseDate != null)
            {
                    foreach (Codex f in EditedFiles)
                    {
                        f.ReleaseDate = TempFile.ReleaseDate;
                    }
            }

            //Add new Author and Publishers to lists
            if (TempFile.Author != "" && !CurrentCollection.AuthorList.Contains(TempFile.Author)) CurrentCollection.AuthorList.Add(TempFile.Author);
            if(TempFile.Publisher != "" && !CurrentCollection.PublisherList.Contains(TempFile.Publisher)) CurrentCollection.PublisherList.Add(TempFile.Publisher);

            //Add and remove Tags
            foreach(Codex f in EditedFiles)
            {
                if (TagsToAdd.Count > 0)
                {
                    //add all tags from TagsToAdd
                    foreach(Tag t in TagsToAdd) f.Tags.Add(t);
                    //remove duplacates
                    f.Tags = new ObservableCollection<Tag>(f.Tags.Distinct());
                }
                if (TagsToRemove.Count > 0)
                {
                    //remove Tags from TagsToRemove
                    foreach (Tag t in TagsToRemove) f.Tags.Remove(t);
                }
                
            }
            CloseAction();
        }

        public override void Cancel()
        {
            CloseAction();
        }

        #endregion
    }
}

