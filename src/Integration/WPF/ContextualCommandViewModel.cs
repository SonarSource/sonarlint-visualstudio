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
        private readonly RelayCommand proxyCommand;

        private ICommand command;
        private Func<object, string> displayTextFunc;
        private string tooltip;
        private Func<object, IconViewModel> iconFunc;

        /// <summary>
        /// Creates an instance of contextual command view model
        /// </summary>
        /// <param name="fixedContext">Required context</param>
        /// <param name="command">Optional real command to trigger and pass the fixed context to</param>
        public ContextualCommandViewModel(object fixedContext, ICommand command)
        {
            if (fixedContext == null)
            {
                throw new ArgumentNullException(nameof(fixedContext));
            }

            this.fixedContext = fixedContext;
            this.proxyCommand = new RelayCommand(this.Execute, this.CanExecute);
            this.SetCommand(command);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
            "S3236:Methods with caller info attributes should not be invoked with explicit arguments",
            Justification = "We want to change a different property than the 'caller' which is a method",
            Scope = "member",
            Target = "~M:SonarLint.VisualStudio.Integration.WPF.ContextualCommandViewModel.SetDynamicDisplayText(System.Func{System.Object,System.String})")]
        public void SetDynamicDisplayText(Func<object, string> getDisplayText)
        {
            if (getDisplayText == null)
            {
                throw new ArgumentNullException(nameof(getDisplayText));
            }

            this.displayTextFunc = getDisplayText;
            this.RaisePropertyChanged(nameof(this.DisplayText));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
            "S3236:Methods with caller info attributes should not be invoked with explicit arguments",
            Justification = "We want to change a different property than the 'caller' which is a method",
            Scope = "member",
            Target = "~M:SonarLint.VisualStudio.Integration.WPF.ContextualCommandViewModel.SetDynamicIcon(System.Func{System.Object,SonarLint.VisualStudio.Integration.WPF.IconViewModel})")]
        public void SetDynamicIcon(Func<object, IconViewModel> getIconFunc)
        {
            if (getIconFunc == null)
            {
                throw new ArgumentNullException(nameof(getIconFunc));
            }

            this.iconFunc = getIconFunc;
            this.RaisePropertyChanged(nameof(this.Icon));
        }

        public void SetCommand(ICommand realCommand)
        {
            this.command = realCommand;
            this.proxyCommand.RequeryCanExecute();
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
            return this.command != null && this.command.CanExecute(this.fixedContext);
        }
    }
}
