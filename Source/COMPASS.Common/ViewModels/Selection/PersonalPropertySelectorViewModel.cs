using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Common.ViewModels.Selection
{
    public class PersonalPropertySelectorViewModel : ObservableObject
    {
        public PersonalPropertySelectorViewModel(string propertyName, string displayName)
        {
            PropertyName = propertyName;
            DisplayName = displayName;
        }

        public string PropertyName { get; }
        public string DisplayName { get; }

        public bool Selected { get; set => SetProperty(ref field, value); } = true;
    }
}
