using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Infra.Avalonia.ExtensionMethods;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Models;

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
        
        UpdateSuggestions();
        if (SelectionMode == SelectionMode.Single)
        {
            Text = SelectedItem?.ToString() ?? "";
        }
    }

    private void OnLostFocus(object? sender, FocusChangedEventArgs e)
    {
        var focusedElement = e.NewFocusedElement as Visual;
        var popupChild = _suggestionPopup?.Child;

        bool focusStillInside = focusedElement.IsDescendantOf(this)
            || (popupChild is not null && focusedElement.IsDescendantOf(popupChild));

        if (!focusStillInside)
        {
            HandleFocusLost();
        }
    }

    private void HandleFocusLost()
    {
        if (CanCreate && SelectionMode == SelectionMode.Single)
        {
            //if exactmatch use that, otherwise create new item
            var selectItem = 
                SuggestedItems?.FirstOrDefault(item => item.ToString() == Text) ??
                SuggestedItems?.OfType<NewItem>().FirstOrDefault();
            AcceptSuggestion(selectItem);
        }
        else if (!CanCreate)
        {
            Text = string.Empty;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        //This event should handle selection changed of the ImprovedCombobox itself, 
        //bubbling events from the suggestions can also trigger this, we don't want taht
        if (e.Source == this)
        {
            if (SelectionMode != SelectionMode.Single)
            {
                UpdateSuggestions();
            }
            else
            {
                _suggestionsControl?.SelectedItem = e.AddedItems.Count > 0 ? e.AddedItems[0] : null;
            }
        }
        
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
    
    private void InputTextBoxOnGotFocus(object? sender, FocusChangedEventArgs e)
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
            if (SetAndRaise(TextProperty, ref field, value))
            {
                UpdateSuggestions();
            }
        }
    } = "";

    public static readonly StyledProperty<string?> PlaceholderTextProperty = TextBox.PlaceholderTextProperty.AddOwner<ImprovedComboBox>();

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public static readonly StyledProperty<bool> CanCreateProperty = AvaloniaProperty.Register<ImprovedComboBox, bool>(
        nameof(CanCreate));

    public bool CanCreate
    {
        get => GetValue(CanCreateProperty);
        set => SetValue(CanCreateProperty, value);
    }

    public static readonly StyledProperty<Func<string, object>> CreateItemProperty =
        AvaloniaProperty.Register<ImprovedComboBox, Func<string, object>>(nameof(CreateItem), defaultValue: s => s);

    public Func<string, object> CreateItem
    {
        get => GetValue(CreateItemProperty);
        set => SetValue(CreateItemProperty, value);
    }

    public static readonly DirectProperty<ImprovedComboBox, ObservableCollection<object>> SuggestedItemsProperty =
        AvaloniaProperty.RegisterDirect<ImprovedComboBox, ObservableCollection<object>>(
            nameof(SuggestedItems), o => o.SuggestedItems);

    public RangeObservableCollection<object> SuggestedItems { get; } = [];

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
        object? prevSelected = _suggestionsControl?.SelectedItem;
        
        if (ItemsSource == null)
        {
            SuggestedItems.Clear();
            return;
        }
        
        //Filter items based on text
        var newSuggestions = ItemsSource
            .Cast<object>()
            .Except(SelectionMode == SelectionMode.Multiple ? SelectedItems?.Cast<object>() ?? [] : []) //In multi select, already selected items should not be suggested again
            .Where(x => x != null && x.ToString().MatchesFuzzy(Text))
            .OrderBy(x => x.ToString())
            .ToList();

        //Add new item if CanCreate is set
        if (CanCreate && !string.IsNullOrWhiteSpace(Text) &&
            ItemsSource.Cast<object>().All(item => item.ToString() != Text))
        {
            newSuggestions.Add(new NewItem(Text));
        }
        
        //Add them to suggestions
        SuggestedItems.ReplaceRange(newSuggestions);

         //Reset selection index to top if previously selected item is no longer selectable
         if (_suggestionsControl is { Items.Count: > 0 } && 
             Items.All(item => item != prevSelected))
         {
             _suggestionsControl.SelectedIndex = 0;
         }
    }

    private void AcceptSuggestion()
    {
        if (_suggestionsControl?.SelectedItem == null || _suggestionPopup?.IsOpen != true) return;
        
        AcceptSuggestion(_suggestionsControl.SelectedItem);
    }

    private void AcceptSuggestion(object? item)
    {
        if (item == null) return;

        if (item is NewItem newItem)
        {
            var createdItem = CreateItem(newItem.Text);
            (ItemsSource as System.Collections.IList)?.Add(createdItem);
            if (SelectionMode == SelectionMode.Single)
            {
                SelectedItem = createdItem;
                Text = createdItem.ToString() ?? "";
                _inputTextBox?.CaretIndex = Text.Length;
            }
            else
            {
                SelectedItems?.Add(createdItem);
            }
        }
        else
        {
            if (SelectionMode == SelectionMode.Single)
            {
                SelectedItem = item;
                Text = SelectedItem?.ToString() ?? "";
                _inputTextBox?.CaretIndex = Text.Length;
            }
            else
            {
                SelectedItems?.Add(item);
            }
        }

        _suggestionPopup?.Close();

        if (SelectionMode == SelectionMode.Single)
        {
            TopLevel.GetTopLevel(this)?.Focus();
        }
        else
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