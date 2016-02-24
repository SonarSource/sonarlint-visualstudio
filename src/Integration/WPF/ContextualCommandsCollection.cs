//-----------------------------------------------------------------------
// <copyright file="ContextualCommandsCollection.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SonarLint.VisualStudio.Integration.WPF
{
    public class ContextualCommandsCollection : ObservableCollection<ContextualCommandViewModel>
    {
        public bool HasCommands
        {
            get
            {
                return this.Count > 0;
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasCommands)));
        }
    }
}
