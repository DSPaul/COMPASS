using COMPASS.Models;
using System.Collections.ObjectModel;

namespace COMPASS.ViewModels
{
    public class ProgressViewModel : ObservableObject
    {
        private ObservableCollection<LogEntry> _log = new();
        public ObservableCollection<LogEntry> Log
        {
            get => _log;
            set => SetProperty(ref _log, value);
        }

        private float _percentage;
        public float Percentage
        {
            get => _percentage;
            set => SetProperty(ref _percentage, value);
        }


        private string _text;
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public float SetPercentage(int counter, int total) => Percentage = (int)((float)counter / total * 100);
    }
}
