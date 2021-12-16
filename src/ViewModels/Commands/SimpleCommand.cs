﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace COMPASS.ViewModels.Commands
{
    //Command with parameter but without canExecute
    public class SimpleCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        readonly Action<object> _execute;

        public SimpleCommand(Action<object> Execute)
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