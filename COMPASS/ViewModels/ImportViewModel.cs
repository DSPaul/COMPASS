using COMPASS.Models;
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
using iText;
using iText.Kernel.Pdf;

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
            BackgroundWorker worker;

            //Call Relevant funcion
            switch (mode)
            {
                case ImportMode.Pdf:
                    worker= new BackgroundWorker { WorkerReportsProgress = true };
                    worker.DoWork += ImportFromPdf;
                    worker.ProgressChanged += ProgressChanged;
                    worker.RunWorkerAsync();
                    break;
                case ImportMode.Manual:
                    ImportManual();
                    break;
            }  
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

            MyFile SelectWhenDone = null;

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
                        PdfDocument pdfdoc = new PdfDocument(new PdfReader(path));
                        var info = pdfdoc.GetDocumentInfo();
                        MyFile pdf = new MyFile(_data)
                        {
                            Path = path,
                            Title = info.GetTitle() ?? System.IO.Path.GetFileNameWithoutExtension(path),
                            Author = info.GetAuthor(),
                            PageCount = pdfdoc.GetNumberOfPages()
                        };
                        pdfdoc.Close();
                        _data.AllFiles.Add(pdf);
                        CoverArtGenerator.ConvertPDF(pdf, _data.Folder);
                        SelectWhenDone = pdf;
                    }
                }
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MVM.Reset();
            });
            if(SelectWhenDone!=null) MVM.CurrentFileViewModel.SelectedFile = SelectWhenDone;
        }

        private void ImportManual()
        {
            MVM.CurrentEditViewModel = new FileEditViewModel(MVM,null);
            FilePropWindow fpw = new FilePropWindow((FileEditViewModel)MVM.CurrentEditViewModel);
            fpw.ShowDialog();
            fpw.Topmost = true;
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
