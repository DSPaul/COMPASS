using COMPASS.Models;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace COMPASS.Resources.Controls
{
    /// <summary>
    /// Interaction logic for PriorityControl.xaml
    /// </summary>
    public partial class PriorityControl
    {
        public PriorityControl()
        {
            InitializeComponent();
            Key++;
        }

        private void MoveUp(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            ITag toMove = btn.CommandParameter as ITag;

            int i = 0;
            while (Root.Items[i] != toMove)
            {
                i++;
            }

            if (i > 0)
            {
                //move the items
                ((IList)Root.ItemsSource).RemoveAt(i);
                ((IList)Root.ItemsSource).Insert(i - 1, toMove);
            }
        }

        private void MoveDown(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            ITag toMove = btn.CommandParameter as ITag;

            int i = 0;
            while (Root.Items[i] != toMove)
            {
                i++;
            }

            if (i + 1 < ((IList)Root.ItemsSource).Count)
            {
                //move the items
                ((IList)Root.ItemsSource).RemoveAt(i);
                ((IList)Root.ItemsSource).Insert(i + 1, toMove);
            }
        }

        //key to uniquely identify control so drag drop only works within the same control
        public static int Key { get; set; } = 0;
    }
}
