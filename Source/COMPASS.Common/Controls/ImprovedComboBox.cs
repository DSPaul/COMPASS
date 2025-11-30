using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.Controls;

[TemplatePart("PART_Input", typeof(TextBox))]
[TemplatePart("PART_SuggestionPopup", typeof(Popup))]
[TemplatePart("PART_Suggestions", typeof(SelectingItemsControl))]
[TemplatePart("PART_Chevron", typeof(IconButton))]
public class ImprovedComboBox : ListBox
{
    private TextBox? _inputTextBox;
    private Popup? _suggestionPopup;
    private SelectingItemsControl? _suggestionsControl;
    private IconButton? _chevronBtn;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        // if we had a control template before, we need to unsubscribe any event listeners
        SelectionChanged -= OnSelectionChanged;
        LostFocus -= OnLostFocus;
        
        if(_inputTextBox != null)
        {
            _inputTextBox.KeyDown -= InputTextBoxOnKeyDown;
            _inputTextBox.KeyUp -= InputTextBoxOnKeyUp;
            _inputTextBox.GotFocus -= InputTextBoxOnGotFocus;
            _inputTextBox.PointerReleased -= InputTextBoxOnPointerReleased;
        }
        if (_suggestionPopup != null)
        {
            _suggestionPopup.PointerReleased -= SuggestionPopupOnPointerReleased;
        }
        if (_chevronBtn != null)
        {
            _chevronBtn.Click -= ChevronBtnOnClick;
        }
        
        // try to find the control with the given name
        _inputTextBox = e.NameScope.Find("PART_Input") as TextBox;
        _suggestionPopup = e.NameScope.Find("PART_SuggestionPopup") as Popup;
        _suggestionsControl = e.NameScope.Find("PART_Suggestions") as SelectingItemsControl;
        _chevronBtn = e.NameScope.Find("PART_Chevron") as IconButton;
        
        SelectionChanged += OnSelectionChanged;
        LostFocus += OnLostFocus;
        if(_inputTextBox != null)
        {
            _inputTextBox.KeyDown += InputTextBoxOnKeyDown;
            _inputTextBox.KeyUp += InputTextBoxOnKeyUp;
            _inputTextBox.GotFocus += InputTextBoxOnGotFocus;
            _inputTextBox.PointerReleased += InputTextBoxOnPointerReleased;
        }
        if (_suggestionPopup != null)
        {
            _suggestionPopup.PointerReleased += SuggestionPopupOnPointerReleased;
        }
        if (_chevronBtn != null)
        {
            _chevronBtn.Click += ChevronBtnOnClick;
        }
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        //TODO this causes crash
        // if (_inputTextBox?.IsFocused != true && !CanCreate)
        // {
        //     Text = string.Empty;
        // }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateSuggestions();
    }

    #region Events

    private void InputTextBoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_suggestionPopup is {IsOpen: false} && SuggestedItems.Count > 0)
        {
            _suggestionPopup.Open();
        }
        
        switch (e.Key)
        {
            case Key.Enter:
            case Key.Tab:
                if (_suggestionPopup is { IsOpen: true })
                {
                    AcceptSuggestion();
                    if (SelectionMode == SelectionMode.Single)
                    {
                        _suggestionPopup.Close();
                    }
                    e.Handled = true;
                }
                break;
            case Key.Down:
                if (_suggestionsControl != null && _suggestionsControl.SelectedIndex < _suggestionsControl.ItemCount )
                {
                    _suggestionsControl.SelectedIndex++;
                }
                break;
            case Key.Up:
                if (_suggestionsControl is { SelectedIndex: > 0 })
                {
                    _suggestionsControl.SelectedIndex--;
                }
                break;
        }
    }
    
    private void InputTextBoxOnKeyUp(object? sender, KeyEventArgs e)
    {
        //some key such as backspace are not captured by keyup
        
        if (e.Key == Key.Back && _suggestionPopup is {IsOpen: false} && SuggestedItems.Count > 0)
        {
            _suggestionPopup.Open();
        }
    }
    
    private void InputTextBoxOnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (_suggestionPopup is {IsOpen: false} && SuggestedItems.Count > 0)
        {
            _suggestionPopup.Open();
        }
    }
    
    private void InputTextBoxOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_suggestionPopup is {IsOpen: false} && SuggestedItems.Count > 0)
        {
            _suggestionPopup.Open();
        }
    }
    
    private void SuggestionPopupOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        AcceptSuggestion();
    }
    
    private void ChevronBtnOnClick(object? sender, RoutedEventArgs e)
    {
        if (_suggestionPopup == null) return;
        
        if(_suggestionPopup.IsOpen)
        {
            _suggestionPopup.Close();
        }
        else
        {
            _suggestionPopup.Open();
            _inputTextBox?.Focus();
            _inputTextBox?.CaretIndex = _inputTextBox?.Text?.Length ?? 0;
        }
    }
    
    #endregion
    
    #region Properties

    public static readonly DirectProperty<ImprovedComboBox, string> TextProperty = AvaloniaProperty.RegisterDirect<ImprovedComboBox, string>(
        nameof(Text), o => o.Text, (o, v) => o.Text = v);

    public string Text
    {
        get;
        set
        {
            SetAndRaise(TextProperty, ref field, value);
            UpdateSuggestions();
        }
    } = "";

    public static readonly StyledProperty<string?> WatermarkProperty = TextBox.WatermarkProperty.AddOwner<ImprovedComboBox>();

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public static readonly StyledProperty<bool> CanCreateProperty = AvaloniaProperty.Register<ImprovedComboBox, bool>(
        nameof(CanCreate));

    public bool CanCreate
    {
        get => GetValue(CanCreateProperty);
        set => SetValue(CanCreateProperty, value);
    }
    
    public static readonly DirectProperty<ImprovedComboBox, ObservableCollection<object>> SuggestedItemsProperty =
        AvaloniaProperty.RegisterDirect<ImprovedComboBox, ObservableCollection<object>>(
            nameof(SuggestedItems), o => o.SuggestedItems);

    public ObservableCollection<object> SuggestedItems { get; } = [];

    #endregion

    public static readonly DirectProperty<ImprovedComboBox, ICommand> RemoveItemCommandProperty =
        AvaloniaProperty.RegisterDirect<ImprovedComboBox, ICommand>(
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
            .Except(SelectionMode == SelectionMode.Multiple ? SelectedItems?.Cast<object>() ?? [] : []) //In multi select, already selected items should not be suggested again
            .Where(x => x != null && x.ToString().MatchesFuzzy(Text))
            .OrderBy(x => x.ToString());

        //Add them to suggestions
        foreach (var item in filtered)
        {
            SuggestedItems.Add(item);
        }

        //Add new item if CanCreate is set
        if (CanCreate && !string.IsNullOrWhiteSpace(Text) &&
            ItemsSource.Cast<object>().All(item => item.ToString() != Text))
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
                if (SelectionMode == SelectionMode.Single)
                {
                    SelectedItem = _suggestionsControl.SelectedItem;
                    Text = SelectedItem?.ToString() ?? "";
                    _inputTextBox?.CaretIndex = Text.Length;
                }
                else
                {
                    SelectedItems.Add(_suggestionsControl.SelectedItem);
                }
            }
        }
        _suggestionPopup?.Close();

        //clear textbox if multiple entry is allowed
        if (SelectionMode != SelectionMode.Single)
        {
            Text = string.Empty;
        }
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