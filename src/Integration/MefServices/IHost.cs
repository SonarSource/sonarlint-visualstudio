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
using System.Collections.Generic;
using System.Windows.Threading;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration
{
    internal interface IHost : IServiceProvider
    {
        /// <summary>
        /// The UI thread dispatcher. Not null.
        /// </summary>
        Dispatcher UIDispatcher { get; }

        /// <summary>
        /// <see cref="ISonarQubeService"/>. Not null.
        /// </summary>
        ISonarQubeService SonarQubeService { get; }

        /// <summary>
        /// The visual state manager. Not null.
        /// </summary>
        IStateManager VisualStateManager { get; }

        /// <summary>
        /// The currently active section. Null when no active section.
        /// </summary>
        ISectionController ActiveSection { get; }

        /// <summary>
        /// Sets the <see cref="ActiveSection"/> with the specified <paramref name="section"/>
        /// </summary>
        /// <param name="section">Required</param>
        void SetActiveSection(ISectionController section);

        /// <summary>
        /// Change event when the <see cref="ActiveSection"/> changed
        /// </summary>
        event EventHandler ActiveSectionChanged;

        /// <summary>
        /// Clears the <see cref="ActiveSection"/>
        /// </summary>
        void ClearActiveSection();

        ISet<Language> SupportedPluginLanguages { get; }
    }
}
