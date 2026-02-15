using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace COMPASS.Common.Views
{
    public partial class ItemsSelectorView : UserControl
    {
        public ItemsSelectorView()
        {
            InitializeComponent();
        }
        
        public static readonly StyledProperty<object> TitleProperty =
            AvaloniaProperty.Register<ItemsSelectorView, object>(nameof(Title));

        public object Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<ItemsSelectorView, bool>(nameof(IsExpanded));

        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public static readonly StyledProperty<bool> ShowSelectAllProperty =
            AvaloniaProperty.Register<ItemsSelectorView, bool>(nameof(ShowSelectAll), defaultValue: true);

        public bool ShowSelectAll
        {
            get => GetValue(ShowSelectAllProperty);
            set => SetValue(ShowSelectAllProperty, value);
        }

        public static readonly StyledProperty<ITemplate<Panel>> ItemsPanelProperty =
            AvaloniaProperty.Register<ItemsSelectorView, ITemplate<Panel>>(nameof(ItemsPanel));

        public ITemplate<Panel> ItemsPanel
        {
            get => GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }

        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<ItemsSelectorView, IDataTemplate>(nameof(ItemTemplate));

        public IDataTemplate ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }
    }
}

