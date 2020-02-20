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
                int prog_counter = 1;
                foreach (string path in openFileDialog.FileNames)
                {
                    //Update Progress Bar
                    float prog = (float)prog_counter / openFileDialog.FileNames.Length *100;
                    prog_counter++;
                    (sender as BackgroundWorker).ReportProgress((int)prog);

                    //Import File
                    if (_data.AllFiles.All(p => p.Path != path))
                    {
                        MyFile pdf = new MyFile(_data) { Path = path, Title = System.IO.Path.GetFileNameWithoutExtension(path) };
                        _data.AllFiles.Add(pdf);
                        CoverArtGenerator.ConvertPDF(pdf, _data.Folder);
                    }
                    Thread.Sleep(1000);
                }
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MVM.Reset();
            });  
        }

        void ImportManual(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                (sender as BackgroundWorker).ReportProgress(i);
                Thread.Sleep(100);
            }
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Progress = e.ProgressPercentage;
        }

        #endregion
    }
}
