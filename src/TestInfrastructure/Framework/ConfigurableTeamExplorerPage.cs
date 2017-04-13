/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.ComponentModel;
using Microsoft.TeamFoundation.Controls;

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

        #endregion ITeamExplorerPage
    }
}