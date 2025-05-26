using Avalonia.Input;
using COMPASS.Common.Models;

namespace COMPASS.Common.Controls
{
    public class CollapsableTabItem : IconTabItem
    {
        public CollapsableTabItem()
        {
            AddHandler(Gestures.TappedEvent, TabItemClicked);
            AddHandler(Gestures.DoubleTappedEvent, TabItemClicked);
        }

        protected override void TabItemClicked(object? sender, TappedEventArgs e)
        {
            base.TabItemClicked(sender, e);
            
            if (DataContext is not IDealsWithTabControl vm) return;
            
            //Click open tab -> collapse
            if (vm.PrevSelectedTab == TabIndex)
            {
                vm.SelectedTab = 0;
            }
            
            vm.PrevSelectedTab = TabIndex;
        }
    }
}
