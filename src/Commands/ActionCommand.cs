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

        public event EventHandler CanExecuteChanged;

        public ActionCommand(Action Execute)
        {
            _execute = Execute;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => _execute.Invoke();
    }
}
