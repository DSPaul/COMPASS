using System;
using System.Windows.Input;

namespace COMPASS.Common.Commands
{
    //Relay command that works with functions that return bool
    //to for example indicate success of execution
    /// <summary>
    /// Command for methods that take one argument and return bool that indicates success
    /// </summary>
    /// <typeparam name="T"> Type of function argument </typeparam>
    /// <typeparam name="R"> Type of return value </typeparam>
    public class ReturningRelayCommand<T, R> : ICommand
    {
        private readonly Func<T?, R> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public ReturningRelayCommand(Func<T?, R> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute((T?)parameter);

        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}
