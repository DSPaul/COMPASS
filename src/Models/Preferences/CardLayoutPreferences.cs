﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Models.Preferences
{
    public class CardLayoutPreferences : ObservableObject
    {
        private bool _showTitle = true;
        public bool ShowTitle
        {
            get => _showTitle;
            set => SetProperty(ref _showTitle, value);
        }

        public bool ShowAuthor
        {
            get => Properties.Settings.Default.CardShowAuthor;
            set
            {
                Properties.Settings.Default.CardShowAuthor = value;
                OnPropertyChanged();
            }
        }

        public bool ShowPublisher
        {
            get => Properties.Settings.Default.CardShowPublisher;
            set
            {
                Properties.Settings.Default.CardShowPublisher = value;
                OnPropertyChanged();
            }
        }

        public bool ShowReleaseDate
        {
            get => Properties.Settings.Default.CardShowRelease;
            set
            {
                Properties.Settings.Default.CardShowRelease = value;
                OnPropertyChanged();
            }
        }

        public bool ShowVersion
        {
            get => Properties.Settings.Default.CardShowVersion;
            set
            {
                Properties.Settings.Default.CardShowVersion = value;
                OnPropertyChanged();
            }
        }

        public bool ShowRating
        {
            get => Properties.Settings.Default.CardShowRating;
            set
            {
                Properties.Settings.Default.CardShowRating = value;
                OnPropertyChanged();
            }
        }

        public bool ShowTags
        {
            get => Properties.Settings.Default.CardShowTags;
            set
            {
                Properties.Settings.Default.CardShowTags = value;
                OnPropertyChanged();
            }
        }

        public bool ShowFileIcons
        {
            get => Properties.Settings.Default.CardShowFileIcons;
            set
            {
                Properties.Settings.Default.CardShowFileIcons = value;
                OnPropertyChanged();
            }
        }
    }
}
