using System;
using System.Windows.Input;

namespace COMPASS.Commands
{
    /// <summary>
    /// Command for methods without arguments
    /// </summary>
    public class ActionCommand : ICommand
    {
        readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public ActionCommand(Action Execute, Func<bool> CanExecute = null)
        {
            _execute = Execute;
            _canExecute = CanExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute.Invoke();
    }
}
