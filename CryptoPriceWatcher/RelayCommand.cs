﻿using System;
using System.Windows.Input;

namespace CryptoPriceWatcher {
    public class RelayCommand : ICommand {

        private Action _action;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action action) {
            this._action = action;
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public void Execute(object parameter) {
            _action();
        }
    }
}
