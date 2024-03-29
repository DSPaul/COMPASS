﻿using Autofac;
using COMPASS.Interfaces;
using COMPASS.Models;

namespace COMPASS.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        //So every viewmodel has access to all the others
        public static MainViewModel? MVM { get; set; }

        private static IMessageBox? _messageBox;
        protected static IMessageBox messageDialog => _messageBox ??= App.Container.Resolve<IMessageBox>();
    }
}