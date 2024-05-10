using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Common.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        //So every viewmodel has access to all the others
        public static MainViewModel? MVM { get; set; }
    }
}