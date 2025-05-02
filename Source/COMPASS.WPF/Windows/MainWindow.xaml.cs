﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.ViewModels;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //set Itemsources for databinding
            MainViewModel = new MainViewModel();
            DataContext = MainViewModel;
        }

        private readonly MainViewModel MainViewModel;

        //Deselects when you click away
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) => MainGrid.Focus();

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ProgressViewModel.GetInstance().CancelBackgroundTask();
            MainViewModel.CollectionVM?.CurrentCollection.Save();
            PreferencesService.GetInstance().SavePreferences();
        }

        #region Window management

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);
        }

        public static IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                // We need to tell the system what our size should be when maximized. Otherwise it will cover the whole screen,
                // including the task bar.
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO))!;

                // Adjust the maximized size and position to fit the work area of the correct monitor
                IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                if (monitor != IntPtr.Zero)
                {
                    MONITORINFO monitorInfo = new()
                    {
                        cbSize = Marshal.SizeOf(typeof(MONITORINFO))
                    };
                    GetMonitorInfo(monitor, ref monitorInfo);
                    RECT rcWorkArea = monitorInfo.rcWork;
                    RECT rcMonitorArea = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                    mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                    mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
                }

                Marshal.StructureToPtr(mmi, lParam, true);
            }

            return IntPtr.Zero;
        }

        private const int WM_GETMINMAXINFO = 0x0024;

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
        private void MinimizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Maximized:
                    WindowState = WindowState.Normal;
                    break;
                case WindowState.Normal:
                    WindowState = WindowState.Maximized;
                    break;
            }
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var progressVM = ProgressViewModel.GetInstance();
            if (progressVM.WorkInProgress)
            {
                progressVM.CancelBackgroundTask();
                int totalDelay = 0;
                while (progressVM.WorkInProgress && totalDelay < 5000)
                {
                    int delay = 100;
                    await Task.Delay(delay);
                    totalDelay += delay;
                }
            }
            Application.Current.Shutdown();
        }
        #endregion

        private void Toggle_ContextMenu(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = (Button)sender;
                btn.ContextMenu!.PlacementTarget = btn;
                btn.ContextMenu!.IsOpen = !btn.ContextMenu!.IsOpen;
            }
            catch (Exception ex)
            {
                Logger.Error("Could not open collection Context menu", ex);
                return;
            }
        }

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.S:
                    // Ctrl + S to search
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        Searchbar.Focus();
                        e.Handled = true;
                    }
                    break;

                case Key.I:
                    // Ctrl + I toggle info
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        ((MainViewModel)DataContext).CodexInfoVM.ShowCodexInfo = !((MainViewModel)DataContext).CodexInfoVM.ShowCodexInfo;
                        e.Handled = true;
                    }
                    break;

                case Key.F5:
                    await MainViewModel.CollectionVM.Refresh();
                    e.Handled = true;
                    break;
            }
        }

        private async void CollectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //only refresh if a new item is set
            if (e.AddedItems.Count > 0)
            {
                await MainViewModel.CollectionVM.Refresh();
            }
        }
    }
}


