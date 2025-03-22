using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace COMPASS.Common.Views;

public partial class TagEditBasicView : UserControl
{
    public TagEditBasicView()
    {
        InitializeComponent();
    }

    private void TagNameTextBox_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Focus();
            textBox.CaretIndex = textBox.Text?.Length ?? 0;
        }
    }
}