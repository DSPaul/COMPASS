using System;
using Microsoft.Win32;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using COMPASS.ViewModels;
using COMPASS.Models;
using System.Drawing;

namespace COMPASS
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
            MainViewModel = new MainViewModel("DnD");
            DataContext = MainViewModel;

            //MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            //MaxHeight = SystemParameters.VirtualScreenHeight;
            MaximizeWindow(this);
        }

        private MainViewModel MainViewModel;

        // is true if we hold left mouse button on windows tilebar
        private bool DragWindow = false;

        //Deselects when you click away
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainGrid.Focus();
        }

        //removes tag from filter list when clicked
        private void ActiveTag_Click(object sender, RoutedEventArgs e)
        {
            if ((Tag)CurrentTagList.SelectedItem != null)
            {
                Tag t = (Tag)CurrentTagList.SelectedItem;
                if (!t.GetType().IsSubclassOf(typeof(Tag))) MainViewModel.FilterHandler.RemoveTagFilter(t);
                else MainViewModel.FilterHandler.RemoveFieldFilter((FilterTag)t);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MainViewModel.CurrentData.SaveFilesToFile();
            MainViewModel.CurrentData.SaveTagsToFile();
            Properties.Settings.Default.Save();
        }

        #region Window management
        public static System.Windows.Forms.Screen CurrentScreen(Window window)
        {
            return System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)window.Left + (int)window.ActualWidth/2, (int)window.Top + (int)window.ActualHeight/2));
        }

        //get size of a virtual window to limit size when maximizing main window
        private (double height, double width) GetVirtualWindowSize()
        {
            Window virtualWindow = new Window();
            System.Windows.Forms.Screen targetScreen = CurrentScreen(this);
            Rectangle viewport = targetScreen.WorkingArea;
            virtualWindow.Top = viewport.Top;
            virtualWindow.Left = viewport.Left;
            virtualWindow.Show();
            virtualWindow.Opacity = 0;
            virtualWindow.WindowState = WindowState.Maximized;
            double returnHeight = virtualWindow.Height;
            double returnWidth = virtualWindow.Width;
            virtualWindow.Close();
            return (returnHeight, returnWidth);
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        private void MaximizeWindow(Window window)
        {
            var sizingParams = GetVirtualWindowSize();
            MaxHeight = sizingParams.height;
            MaxWidth = sizingParams.width;
            WindowState = WindowState.Maximized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            switch (WindowState)
            {
                case (WindowState.Maximized):
                    WindowState = WindowState.Normal;
                    break;
                case (WindowState.Normal):
                    MaximizeWindow(this);
                    break;
            }
        }

        private void WindowsBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeWindow(this);
                DragWindow = false;
            }

            else
            {
                DragMove();
                if (WindowState == WindowState.Maximized) DragWindow = WindowState == WindowState.Maximized;
            }
        }
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (DragWindow)
            {
                DragWindow = false;

                System.Windows.Forms.Screen targetScreen = CurrentScreen(this);
                Rectangle viewport = targetScreen.WorkingArea;

                var point = e.MouseDevice.GetPosition(this);

                Left = viewport.Left + point.X - (RestoreBounds.Width * 0.5);
                Top = viewport.Top + point.Y - 20;

                WindowState = WindowState.Normal;

                try
                {
                    DragMove();
                }

                catch (InvalidOperationException)
                {
                    MaximizeWindow(this);
                }
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //this.Close();
            Application.Current.Shutdown();
        }
        #endregion

        private void ViewConfig_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.PlacementTarget = (sender as Button);
            (sender as Button).ContextMenu.IsOpen = true;
        }
    }
}


