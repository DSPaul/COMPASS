using System;
using System.Windows.Input;

namespace COMPASS.Commands
{
    //Relaycommand that works with functions that return bool
    //to for example indicate succes of excecution
    public class ReturningRelayCommand<T> : ICommand
    {
        private readonly Func<T, bool> _execute;
        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public ReturningRelayCommand(Func<T, bool> Execute, Func<T, bool> CanExecute = null)
        {
            _execute = Execute;
            _canExecute = CanExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);

        public void Execute(object parameter) => _execute((T)parameter);
    }
}
