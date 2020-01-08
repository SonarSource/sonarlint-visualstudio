/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

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
        private readonly object commandArgs;
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
            : this(fixedContext, command, commandArgs: null)
        {
        }

        /// <summary>
        /// Creates an instance of contextual command view model
        /// </summary>
        /// <param name="fixedContext">Required context</param>
        /// <param name="command">Optional real command to trigger and pass the fixed context to</param>
        /// <param name="commandArgs">Optional arguments to pass to the command. If null then <paramref name="fixedContent"/> will be passed.</param>
        public ContextualCommandViewModel(object fixedContext, ICommand command, object commandArgs)
        {
            if (fixedContext == null)
            {
                throw new ArgumentNullException(nameof(fixedContext));
            }

            this.fixedContext = fixedContext;
            this.commandArgs = commandArgs ?? fixedContext;
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
            this.command.Execute(this.commandArgs);
        }

        private bool CanExecute()
        {
            return this.command != null && this.command.CanExecute(this.commandArgs);
        }
    }
}
