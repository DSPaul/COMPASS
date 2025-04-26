using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Controls;

[TemplatePart("PART_Input", typeof(TextBox))]
[TemplatePart("PART_SuggestionPopup", typeof(Popup))]
[TemplatePart("PART_Suggestions", typeof(SelectingItemsControl))]
public class MultiSelectAutoCompleteBox : ListBox
{
    private TextBox? _inputTextBox;
    private Popup? _suggestionPopup;
    private SelectingItemsControl? _suggestionsControl;
    
    public MultiSelectAutoCompleteBox()
    {
        SuggestedItems = [];
    }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // if we had a control template before, we need to unsubscribe any event listeners
        if(_inputTextBox is not null)
        {
            _inputTextBox.KeyDown -= InputTextBoxOnKeyDown;
        }

        if (_suggestionPopup is not null)
        {
            _suggestionPopup.PointerReleased -= SuggestionPopupOnPointerReleased;
        }
        
        // try to find the control with the given name
        _inputTextBox = e.NameScope.Find("PART_Input") as TextBox;
        _suggestionPopup = e.NameScope.Find("PART_SuggestionPopup") as Popup;
        _suggestionsControl = e.NameScope.Find("PART_Suggestions") as SelectingItemsControl;
        
        if(_inputTextBox != null)
        {
            _inputTextBox.KeyDown += InputTextBoxOnKeyDown;
        }


        if (_suggestionPopup != null)
        {
            _suggestionPopup.PointerReleased += SuggestionPopupOnPointerReleased;
        }
    }

    #region Events

    private void InputTextBoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
            case Key.Tab:
                AcceptSuggestion();
                e.Handled = true;
                break;
            case Key.Down:
                if (_suggestionPopup is {IsOpen: false})
                {
                    _suggestionPopup.Open();
                }
                else if (_suggestionsControl != null && _suggestionsControl.SelectedIndex < _suggestionsControl.ItemCount )
                {
                    _suggestionsControl.SelectedIndex++;
                }
                break;
            case Key.Up:
                if (_suggestionPopup is {IsOpen: false})
                {
                    _suggestionPopup.Open();
                }
                else if (_suggestionsControl != null && _suggestionsControl.SelectedIndex > 0)
                {
                    _suggestionsControl.SelectedIndex--;
                }
                break;
            default:
                _suggestionPopup?.Open();
                break;
        }
    }
    
    private void SuggestionPopupOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        AcceptSuggestion();
    }
    
    #endregion
    
    #region Properties
    
    private string _text = "";

    public static readonly DirectProperty<MultiSelectAutoCompleteBox, string> TextProperty = AvaloniaProperty.RegisterDirect<MultiSelectAutoCompleteBox, string>(
        nameof(Text), o => o.Text, (o, v) => o.Text = v);

    public string Text
    {
        get => _text;
        set
        {
            SetAndRaise(TextProperty, ref _text, value);
            UpdateSuggestions();
        }
    }

    public static readonly StyledProperty<string?> WatermarkProperty = TextBox.WatermarkProperty.AddOwner<MultiSelectAutoCompleteBox>();

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public static readonly StyledProperty<bool> CanCreateProperty = AvaloniaProperty.Register<MultiSelectAutoCompleteBox, bool>(
        nameof(CanCreate));

    public bool CanCreate
    {
        get => GetValue(CanCreateProperty);
        set => SetValue(CanCreateProperty, value);
    }
    
    public static readonly DirectProperty<MultiSelectAutoCompleteBox, ObservableCollection<object>> SuggestedItemsProperty =
        AvaloniaProperty.RegisterDirect<MultiSelectAutoCompleteBox, ObservableCollection<object>>(
            nameof(SuggestedItems), o => o.SuggestedItems);

    public ObservableCollection<object> SuggestedItems { get; }
    
    #endregion

    public static readonly DirectProperty<MultiSelectAutoCompleteBox, ICommand> RemoveItemCommandProperty =
        AvaloniaProperty.RegisterDirect<MultiSelectAutoCompleteBox, ICommand>(
            nameof(RemoveItemCommand), o => o.RemoveItemCommand);

    private RelayCommand<object>? _removeItemCommand;
    public ICommand RemoveItemCommand => _removeItemCommand ??= new(RemoveItem);
    
    private void RemoveItem(object? item)
    {
        if (item == null) return;
        SelectedItems?.Remove(item);
    }
    
    private void UpdateSuggestions()
    {
        SuggestedItems.Clear();

        if (ItemsSource == null)
        {
            return;
        }
        
        //Filter items based on text
        var filtered = ItemsSource
            .Cast<object>()
            .Except(SelectedItems?.Cast<object>() ?? []) //Already selected items should not be suggested again
            .Where(x => x != null && x.ToString().MatchesFuzzy(Text))
            .OrderBy(x => x.ToString());

        //Add them to suggestions
        foreach (var item in filtered)
        {
            SuggestedItems.Add(item);
        }

        //Add new item if CanCreate is set
        if (CanCreate && !string.IsNullOrWhiteSpace(Text) &&
            SuggestedItems.All(item => item.ToString() != Text))
        {
            SuggestedItems.Add(new NewItem(Text));
        }

        //Reset selection index to top
        if (_suggestionsControl is { Items.Count: > 0 })
        {
            _suggestionsControl.SelectedIndex = 0;
        }
    }

    private void AcceptSuggestion()
    {
        if (_suggestionsControl?.SelectedItem != null &&
            SelectedItems != null &&
            _suggestionPopup?.IsOpen == true)
        {
            if (_suggestionsControl.SelectedItem is NewItem newItem)
            {
                //TODO make this support items that aren't text
                SelectedItems.Add(newItem.Text);
            }
            else
            {
                SelectedItems.Add(_suggestionsControl.SelectedItem);
            }
        }
        _suggestionPopup?.Close();
        Text = string.Empty;
    }

    class NewItem
    {
        public string Text { get; set; }

        public NewItem(string text)
        {
            Text = text;
        }
        
        public override string ToString()
        {
            return $"New: '{Text}'";
        }
    }
}