//-----------------------------------------------------------------------
// <copyright file="ConfigurableTeamExplorerPage.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Controls;
using System;
using System.ComponentModel;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableTeamExplorerPage : ITeamExplorerPage
    {
        public Guid PageId { get; }

        public ConfigurableTeamExplorerPage(Guid guid)
        {
            this.PageId = guid;
        }

        #region ITeamExplorerPage

        string ITeamExplorerPage.Title
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object ITeamExplorerPage.PageContent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool ITeamExplorerPage.IsBusy
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        void ITeamExplorerPage.Initialize(object sender, PageInitializeEventArgs e)
        {
            throw new NotImplementedException();
        }

        void ITeamExplorerPage.Loaded(object sender, PageLoadedEventArgs e)
        {
            throw new NotImplementedException();
        }

        void ITeamExplorerPage.SaveContext(object sender, PageSaveContextEventArgs e)
        {
            throw new NotImplementedException();
        }

        void ITeamExplorerPage.Refresh()
        {
            throw new NotImplementedException();
        }

        void ITeamExplorerPage.Cancel()
        {
            throw new NotImplementedException();
        }

        object ITeamExplorerPage.GetExtensibilityService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}