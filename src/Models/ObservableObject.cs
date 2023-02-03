using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace COMPASS.Models
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;
            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

    }
}
