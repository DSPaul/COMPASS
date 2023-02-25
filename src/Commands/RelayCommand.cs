using System;
using System.Windows.Input;

namespace COMPASS.Commands
{
    /// <summary>
    /// Command for methods that take one argument
    /// </summary>
    /// <typeparam name="T"> Type of function argument </typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action<T> Execute, Func<T, bool> CanExecute = null)
        {
            _execute = Execute;
            _canExecute = CanExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);

        public void Execute(object parameter) => _execute.Invoke((T)parameter);
    }
}
