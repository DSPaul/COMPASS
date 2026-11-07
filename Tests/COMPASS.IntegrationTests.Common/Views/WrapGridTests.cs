using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.NUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using COMPASS.Infra.Avalonia.Controls;

namespace COMPASS.IntegrationTests.Common.Views
{
    [TestFixture]
    public class WrapGridTests
    {
        /// <summary>
        /// Items can change size while the grid is shown, for example when the tile size is bound to a
        /// preference slider. The grid must resize its cells too, otherwise the items outgrow their cells
        /// and overlap instead of pushing each other along.
        /// </summary>
        [AvaloniaTest]
        public void MeasureOverride_WhenItemWidthChanges_ResizesCellsToFitTheItems()
        {
            List<ItemSizeViewModel> itemViewModels =
            [
                new(), new(), new(), new()
            ];
            WrapGrid wrapGrid = new() { HorizontalContentAlignment = HorizontalAlignment.Left };
            ItemsControl itemsControl = new()
            {
                ItemsSource = itemViewModels,
                ItemTemplate = new FuncDataTemplate<ItemSizeViewModel>((_, _) => CreateItemView()),
                ItemsPanel = new FuncTemplate<Panel?>(() => wrapGrid)
            };
            Window window = new() { Content = itemsControl, Width = 800, Height = 600 };
            window.Show();
            window.UpdateLayout();

            foreach (ItemSizeViewModel itemViewModel in itemViewModels)
                itemViewModel.Width = 300;
            window.UpdateLayout();

            ContentPresenter firstCell = GetCell(wrapGrid, itemViewModels[0]);
            ContentPresenter secondCell = GetCell(wrapGrid, itemViewModels[1]);
            Assert.Multiple(() =>
            {
                Assert.That(firstCell.Bounds.Width, Is.EqualTo(300).Within(0.5), "the cell should grow with its item");
                Assert.That(secondCell.Bounds.X, Is.GreaterThanOrEqualTo(firstCell.Bounds.Right - 0.5),
                    "the next item should be pushed along instead of overlapping");
            });
        }

        private static Control CreateItemView()
        {
            Border border = new() { Background = Brushes.Gray };
            border.Bind(Border.WidthProperty, new Binding(nameof(ItemSizeViewModel.Width)));
            border.Bind(Border.HeightProperty, new Binding(nameof(ItemSizeViewModel.Height)));
            return border;
        }

        private static ContentPresenter GetCell(WrapGrid wrapGrid, ItemSizeViewModel itemViewModel)
        {
            return wrapGrid.GetVisualChildren()
                .OfType<ContentPresenter>()
                .Single(container => ReferenceEquals(container.DataContext, itemViewModel));
        }

        private sealed class ItemSizeViewModel : INotifyPropertyChanged
        {
            private double _width = 100;

            public double Width
            {
                get => _width;
                set
                {
                    _width = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Width)));
                }
            }

            public double Height { get; } = 100;

            public event PropertyChangedEventHandler? PropertyChanged;
        }
    }
}
