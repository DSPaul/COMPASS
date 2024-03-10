using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace COMPASS.Resources.Controls.MultiSelectCombobox
{
    [TemplatePart(Name = "placeholderText", Type = typeof(TextBlock))]
    [TemplatePart(Name = "rtxt", Type = typeof(RichTextBox))]
    [TemplatePart(Name = "popup", Type = typeof(Popup))]
    [TemplatePart(Name = "lstSuggestion", Type = typeof(ListBox))]
    public sealed partial class MultiSelectCombobox : Control
    {
        #region Constructor
        static MultiSelectCombobox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiSelectCombobox), new FrameworkPropertyMetadata(typeof(MultiSelectCombobox)));
        }
        public MultiSelectCombobox()
        {
            PreviewKeyDown += MultiSelectCombobox_PreviewKeyDown;
            LostFocus += MultiSelectCombobox_LostFocus;
        }

        #endregion

        #region Template Parts
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            PlaceholderElement = GetTemplateChild("placeholderText") as TextBlock;
            RichTextBoxElement = GetTemplateChild("rtxt") as RichTextBox;
            PopupElement = GetTemplateChild("popup") as Popup;
            SuggestionElement = GetTemplateChild("lstSuggestion") as ListBox;
        }

        private TextBlock? PlaceholderElement { get; set; }

        private RichTextBox? _richTextBoxElement;
        private RichTextBox? RichTextBoxElement
        {
            get => _richTextBoxElement;
            set
            {
                if (_richTextBoxElement != null)
                {
                    _richTextBoxElement.TextChanged -= RichTextBoxElement_TextChanged;
                    _richTextBoxElement.SizeChanged -= RichTextBoxElement_SizeChanged;
                    DataObject.RemovePastingHandler(_richTextBoxElement, PasteHandler);
                    DataObject.RemoveCopyingHandler(_richTextBoxElement, OnSelectionStartDrag);
                    _richTextBoxElement.RemoveHandler(CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(SetClipboardTextWithCommandCancelled));
                    _richTextBoxElement.DragEnter -= OnDragEnter;
                    _richTextBoxElement.Drop -= OnDragDrop;
                }

                _richTextBoxElement = value;

                if (_richTextBoxElement != null)
                {
                    _richTextBoxElement.SetParagraphAsFirstBlock();

                    if (SelectedItems != null)
                    {
                        //Add all selected items
                        foreach (object item in SelectedItems)
                        {
                            _richTextBoxElement.AddToParagraph(item, CreateInlineUIElement);
                        }
                    }

                    _richTextBoxElement.TextChanged += RichTextBoxElement_TextChanged;
                    _richTextBoxElement.SizeChanged += RichTextBoxElement_SizeChanged;
                    DataObject.AddPastingHandler(_richTextBoxElement, PasteHandler);
                    DataObject.AddCopyingHandler(_richTextBoxElement, OnSelectionStartDrag);
                    _richTextBoxElement.AddHandler(CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(SetClipboardTextWithCommandCancelled));
                    _richTextBoxElement.DragEnter += OnDragEnter;
                    _richTextBoxElement.Drop += OnDragDrop;
                    _richTextBoxElement.AllowDrop = true;
                }
            }
        }

        private Popup? PopupElement { get; set; }

        private ListBox? _suggestionElement;
        private ListBox? SuggestionElement
        {
            get => _suggestionElement;
            set
            {
                if (_suggestionElement != null)
                {
                    _suggestionElement.PreviewMouseUp -= SuggestionDropdown_PreviewMouseUp;
                    _suggestionElement.PreviewKeyUp -= SuggestionElement_PreviewKeyUp;
                    _suggestionElement.PreviewMouseDown -= SuggestionDropdown_PreviewMouseDown;
                }

                _suggestionElement = value;

                if (_suggestionElement != null)
                {
                    _suggestionElement.DisplayMemberPath = DisplayMemberPath;
                    _suggestionElement.ItemsSource = ItemSource;
                    _suggestionElement.PreviewMouseUp += SuggestionDropdown_PreviewMouseUp;
                    _suggestionElement.PreviewKeyUp += SuggestionElement_PreviewKeyUp;
                    _suggestionElement.PreviewMouseDown += SuggestionDropdown_PreviewMouseDown;
                }
            }
        }

        #endregion

        #region Properties
        /// <summary>
        /// Item source
        /// </summary>
        public static readonly DependencyProperty ItemSourceProperty =
            DependencyProperty.Register(nameof(ItemSource), typeof(IEnumerable), typeof(MultiSelectCombobox), new PropertyMetadata(ItemSourcePropertyChanged));
        public IEnumerable ItemSource
        {
            get => (IEnumerable)GetValue(ItemSourceProperty);
            set => SetValue(ItemSourceProperty, value);
        }

        /// <summary>
        /// List of selected items
        /// </summary>
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(MultiSelectCombobox), new PropertyMetadata(SelectedItemsChanged));
        public IList SelectedItems
        {
            get => (IList)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        /// <summary>
        /// Char value that separates two selected items. Default value is ';'
        /// </summary>
        public static readonly DependencyProperty ItemSeparatorProperty =
            DependencyProperty.Register(nameof(ItemSeparator), typeof(char), typeof(MultiSelectCombobox), new PropertyMetadata(';'));
        public char ItemSeparator
        {
            get => (char)GetValue(ItemSeparatorProperty);
            set => SetValue(ItemSeparatorProperty, value);
        }

        /// <summary>
        /// Array of additional char value that separates two selected items. Default value is null
        /// </summary>
        public static readonly DependencyProperty AdditionalItemSeparatorsProperty =
            DependencyProperty.Register(nameof(AdditionalItemSeparators), typeof(char[]), typeof(MultiSelectCombobox), new PropertyMetadata(System.Array.Empty<char>()));
        public char[] AdditionalItemSeparators
        {
            get => (char[])GetValue(AdditionalItemSeparatorsProperty);
            set => SetValue(AdditionalItemSeparatorsProperty, value);
        }

        /// <summary>
        /// Display member path - for complex object, we can set this to show value on given path
        /// </summary>
        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(MultiSelectCombobox), new PropertyMetadata(DisplayMemberPathChanged));
        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        /// <summary>
        /// ILookUpContract - implementation for custom behavior of Look-up and create.
        /// If not set, default behavior will be set.
        /// </summary>
        public static readonly DependencyProperty LookUpContractProperty =
            DependencyProperty.Register(nameof(LookUpContract), typeof(ILookUpContract), typeof(MultiSelectCombobox), new PropertyMetadata(null));
        public ILookUpContract LookUpContract
        {
            get => (ILookUpContract)GetValue(LookUpContractProperty);
            set => SetValue(LookUpContractProperty, value);
        }
        #endregion  

        #region Property changed callback
        /// <summary>
        /// When selected item property is changed
        /// </summary>
        /// <param name="d">control</param>
        /// <param name="e">arg</param>
        private static void SelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is MultiSelectCombobox multiChoiceControl
                && e.NewValue is IList selectedItems))
            {
                return;
            }

            try
            {
                //Unsubscribe handlers first
                if (!multiChoiceControl.UnsubscribeHandler()
                    || multiChoiceControl.RichTextBoxElement == null)
                {
                    //Failed to unsubscribe, return
                    return;
                }

                foreach (TextBlock? textBlock in multiChoiceControl.RichTextBoxElement?.GetParagraph()?.Inlines?.Select(i => i.GetTextBlock()).Where(i => i is not null) ?? Enumerable.Empty<TextBlock>())
                {
                    textBlock!.Unloaded -= multiChoiceControl.Tb_Unloaded;
                }

                //Clear everything in RichTextBox
                multiChoiceControl.RichTextBoxElement?.ClearParagraph();

                //Add all selected items
                foreach (object item in selectedItems)
                {
                    multiChoiceControl.RichTextBoxElement?.AddToParagraph(item, multiChoiceControl.CreateInlineUIElement);
                }

                multiChoiceControl.RaiseSelectionChangedEvent(e.OldValue as IList ?? new ArrayList(0), e.NewValue as IList ?? new ArrayList(0));
            }
            finally
            {
                multiChoiceControl.SubscribeHandler();
            }
        }
        /// <summary>
        /// When ItemSource property is changed
        /// </summary>
        /// <param name="d">control</param>
        /// <param name="e">arguments</param>
        private static void ItemSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MultiSelectCombobox multiChoiceControl)
            {
                return;
            }

            if (multiChoiceControl.SuggestionElement == null)
            {
                return;
            }

            multiChoiceControl.SuggestionElement.ItemsSource = (e.NewValue as IEnumerable)?.Cast<object>();
        }

        /// <summary>
        /// Display member path change handler
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void DisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MultiSelectCombobox msc
                || msc.SuggestionElement == null)
            {
                return;
            }

            msc.SuggestionElement.DisplayMemberPath = e.NewValue?.ToString();
        }
        #endregion

        #region Routed Event
        public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(
            name: nameof(SelectionChanged),
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(SelectionChangedEventHandler), ownerType:
            typeof(MultiSelectCombobox));
        public event SelectionChangedEventHandler SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value);
            remove => RemoveHandler(SelectionChangedEvent, value);
        }

        /// <summary>
        /// Raise SelectionChanged event
        /// </summary>
        /// <param name="removedItems">removed items</param>
        /// <param name="addedItems">added items</param>
        private void RaiseSelectionChangedEvent(IList removedItems, IList addedItems) => RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, removedItems, addedItems));
        #endregion
    }
}