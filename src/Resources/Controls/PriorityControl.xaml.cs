using COMPASS.Models;
using System.Collections;
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
        }

        private void MoveUp(object sender, System.Windows.RoutedEventArgs e)
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

        private void MoveDown(object sender, System.Windows.RoutedEventArgs e)
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
    }
}
