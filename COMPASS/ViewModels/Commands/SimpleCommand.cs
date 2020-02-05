using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace COMPASS.ViewModels.Commands
{
    public class SimpleCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        readonly Action<Object> _execute;

        public SimpleCommand(Action<Object> Execute)
        {
            _execute = Execute;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _execute.Invoke(parameter);
        }
    }
}
