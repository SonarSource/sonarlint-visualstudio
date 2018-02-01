/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Microsoft.TeamFoundation.Controls;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="ITeamExplorer"/>.
    /// </summary>
    internal class ConfigurableTeamExplorer : ITeamExplorer
    {
        internal Guid CurrentPageId { get; private set; }

        public IDictionary<Guid, ITeamExplorerPage> AvailablePages { get; } = new Dictionary<Guid, ITeamExplorerPage>();

        public ConfigurableTeamExplorer()
            : this(new Guid(TeamExplorerPageIds.Home))
        {
        }

        public ConfigurableTeamExplorer(Guid startPage)
        {
            this.CurrentPageId = startPage;
            this.AddStandardPages();
        }

        private void AddStandardPages()
        {
            const BindingFlags constantsBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            IEnumerable<string> standardPageIdStrings = typeof(TeamExplorerPageIds)
                                                            .GetFields(constantsBindingFlags)
                                                            .Where(x => x.IsLiteral && !x.IsInitOnly)
                                                            .Select(x => x.GetRawConstantValue())
                                                            .OfType<string>();
            foreach (var idStr in standardPageIdStrings)
            {
                var guid = new Guid(idStr);
                var pageInstance = new ConfigurableTeamExplorerPage(guid);
                this.AvailablePages.Add(guid, pageInstance);
            }
        }

        #region ITeamExplorer

        ITeamExplorerPage ITeamExplorer.CurrentPage
        {
            get
            {
                return this.AvailablePages[this.CurrentPageId];
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

        void ITeamExplorer.ClearNotifications()
        {
            throw new NotImplementedException();
        }

        void ITeamExplorer.ClosePage(ITeamExplorerPage page)
        {
            throw new NotImplementedException();
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        bool ITeamExplorer.HideNotification(Guid id)
        {
            throw new NotImplementedException();
        }

        bool ITeamExplorer.IsNotificationVisible(Guid id)
        {
            throw new NotImplementedException();
        }

        ITeamExplorerPage ITeamExplorer.NavigateToPage(Guid pageId, object context)
        {
            this.CurrentPageId = pageId;
            return ((ITeamExplorer)this).CurrentPage;
        }

        void ITeamExplorer.ShowNotification(string message, NotificationType type, NotificationFlags flags, ICommand command, Guid id)
        {
            throw new NotImplementedException();
        }

        #endregion ITeamExplorer
    }
}