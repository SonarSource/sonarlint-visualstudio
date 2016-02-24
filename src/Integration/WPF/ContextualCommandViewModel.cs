//-----------------------------------------------------------------------
// <copyright file="ContextualCommandViewModel.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.WPF
{
    /// <summary>
    /// View model for a command with fixed context that is required during command execution
    /// </summary>
    /// <typeparam name="T">Command argument. <seealso cref="ICommand"/></typeparam>
    public class ContextualCommandViewModel : ViewModelBase
    {
        private readonly object fixedContext;
        private readonly ICommand command;
        private readonly RelayCommand proxyCommand;

        private Func<object, string> displayTextFunc;
        private string tooltip;
        private Func<object, IconViewModel> iconFunc;

        public ContextualCommandViewModel(object fixedContext, ICommand command)
        {
            if (fixedContext == null)
            {
                throw new ArgumentNullException(nameof(fixedContext));
            }

            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            this.command = command;
            this.fixedContext = fixedContext;
            this.proxyCommand = new RelayCommand(this.Execute, this.CanExecute);
        }

        public void SetDynamicDisplayText(Func<object, string> displayTextFunc)
        {
            if (displayTextFunc == null)
            {
                throw new ArgumentNullException(nameof(displayTextFunc));
            }

            this.displayTextFunc = displayTextFunc;
            this.RaisePropertyChanged(nameof(this.DisplayText));
        }

        public void SetDynamicIcon(Func<object, IconViewModel> iconFunc)
        {
            if (iconFunc == null)
            {
                throw new ArgumentNullException(nameof(iconFunc));
            }

            this.iconFunc = iconFunc;
            this.RaisePropertyChanged(nameof(this.Icon));
        }

        public string DisplayText
        {
            get
            {
                return this.displayTextFunc?.Invoke(this.fixedContext);
            }
            set
            {
                this.displayTextFunc = x => value;
                this.RaisePropertyChanged();
            }
        }

        public string Tooltip
        {
            get { return this.tooltip; }
            set { this.SetAndRaisePropertyChanged(ref this.tooltip, value); }
        }

        public IconViewModel Icon
        {
            get
            {
                return this.iconFunc?.Invoke(this.fixedContext);
            }
            set
            {
                this.iconFunc = x => value;
                this.RaisePropertyChanged();
            }
        }

        public ICommand Command
        {
            get { return this.proxyCommand; }
        }

        internal /*for testing purposes*/ ICommand InternalRealCommand
        {
            get { return this.command; }
        }

        internal /*for testing purposes*/ object InternalFixedContext
        {
            get { return this.fixedContext; }
        }

        private void Execute()
        {
            this.command.Execute(this.fixedContext);
        }

        private bool CanExecute()
        {
            return this.command.CanExecute(this.fixedContext);
        }
    }
}
