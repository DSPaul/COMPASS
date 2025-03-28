using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Controls;

public partial class StringListControl : UserControl
    {
        public StringListControl()
        {
            InitializeComponent();
            
            // Initialize with sample data for design mode
            if (Design.IsDesignMode)
            {
                ReadOnlyToolTip = "This explains why I am locked";
                ReadOnlyItems = Enumerable.Range(0,3).Select(n => $"Readonly String {n}").ToList();
                Items = Enumerable.Range(0,10).Select(n => $"String {n}").ToList();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        #region Properties

        // Items Collection
        public static readonly StyledProperty<IList<string>> ItemsProperty =
            AvaloniaProperty.Register<StringListControl, IList<string>>(
                nameof(Items), new List<string>(), defaultBindingMode:BindingMode.TwoWay);

        public IList<string> Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }
        
        // ReadOnly Items Collection
        public static readonly StyledProperty<IList<string>> ReadOnlyItemsProperty =
            AvaloniaProperty.Register<StringListControl, IList<string>>(
                nameof(ReadOnlyItems), new List<string>(), defaultBindingMode:BindingMode.OneWay);

        public IList<string> ReadOnlyItems
        {
            get => GetValue(ReadOnlyItemsProperty);
            set => SetValue(ReadOnlyItemsProperty, value);
        }
        
        public static readonly StyledProperty<string> ReadOnlyToolTipProperty = AvaloniaProperty.Register<StringListControl, string>(
            nameof(ReadOnlyToolTip), defaultBindingMode: BindingMode.OneTime);

        public string ReadOnlyToolTip
        {
            get => GetValue(ReadOnlyToolTipProperty);
            set => SetValue(ReadOnlyToolTipProperty, value);
        }

        private string _newItemText = "";

        public static readonly DirectProperty<StringListControl, string> NewItemTextProperty = AvaloniaProperty.RegisterDirect<StringListControl, string>(
            nameof(NewItemText), o => o.NewItemText, (o, v) => o.NewItemText = v);

        public string NewItemText
        {
            get => _newItemText;
            set
            {
                SetAndRaise(NewItemTextProperty, ref _newItemText, value);
                AddItemCommand.NotifyCanExecuteChanged();
            }
        }

        #endregion

        #region Commands

        private RelayCommand<string>? _deleteItemCommand;
        public RelayCommand<string> DeleteItemCommand => _deleteItemCommand ??= new (DeleteItem);
        private void DeleteItem(string? item)
        {
            if(item == null) return;
            if (Items.Contains(item))
            {
                Items.Remove(item);
            }
        }
        
        private RelayCommand? _addItemCommand;
        public RelayCommand AddItemCommand => _addItemCommand ??= new(AddItem, CanAddItem);
        private void AddItem()
        {
            if (!string.IsNullOrWhiteSpace(NewItemText))
            {
                Items.AddIfMissing(NewItemText);
                NewItemText = string.Empty;
            }
        }
        private bool CanAddItem() => 
            !string.IsNullOrWhiteSpace(NewItemText) && 
            !Items.Contains(NewItemText) &&
            !ReadOnlyItems.Contains(NewItemText);

        #endregion
    }