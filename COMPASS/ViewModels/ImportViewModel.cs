using COMPASS.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class ImportViewModel : BaseViewModel
    {
        public ImportViewModel(MainViewModel vm, ImportMode mode)
        {
            //set data so we know where to import to
            _data = vm.CurrentData;

            //Only needed for Reset method
            MVM = vm;

            //Start new threat (so program doesn't freeze while importing)
            BackgroundWorker worker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };

            //Call Relevant funcion
            switch (mode)
            {
                case ImportMode.Pdf:
                    worker.DoWork += ImportFromPdf;
                    break;
                case ImportMode.Manual:
                    ProgressWindow pgw = new ProgressWindow(this);
                    pgw.Show();
                    worker.DoWork += ImportManual;
                    break;
            }

            
            worker.ProgressChanged += ProgressChanged;

            worker.RunWorkerAsync();
        }


        #region Properties
        readonly MainViewModel MVM;

        private Data _data;

        private float _progress;
        public float Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }

        private readonly string _importText = "Import in Progress: \n {0} out of {1}";
        private int _importcounter;
        private int _importamount;
        public string ImportText
        {
            get { return string.Format(_importText,_importcounter,_importamount); }
        }

        #endregion

        #region Functions and Events

        void ImportFromPdf(object sender, DoWorkEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AddExtension = false,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProgressWindow pgw;

                //needed to avoid error "The calling Thread must be STA" when creating progress window
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        pgw = new ProgressWindow(this);
                        pgw.Show();
                    });
                int progcounter = 1;
                foreach (string path in openFileDialog.FileNames)
                {
                    //Update Progress Bar
                    float prog = (float)progcounter / openFileDialog.FileNames.Length  * 100;               
                    (sender as BackgroundWorker).ReportProgress((int)prog, new Tuple<int, int>(progcounter, openFileDialog.FileNames.Length));
                    progcounter++;

                    //Import File
                    if (_data.AllFiles.All(p => p.Path != path))
                    {
                        MyFile pdf = new MyFile(_data) { Path = path, Title = System.IO.Path.GetFileNameWithoutExtension(path) };
                        _data.AllFiles.Add(pdf);
                        CoverArtGenerator.ConvertPDF(pdf, _data.Folder);
                    }
                }
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MVM.Reset();
            });  
        }

        void ImportManual(object sender, DoWorkEventArgs e)
        {
            
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Progress = e.ProgressPercentage;
            var args = (Tuple<int, int>)e.UserState;
            _importcounter = args.Item1;
            _importamount = args.Item2;
            RaisePropertyChanged("ImportText");
        }

        #endregion
    }
}
