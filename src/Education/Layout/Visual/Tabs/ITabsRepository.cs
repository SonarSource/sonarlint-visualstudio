/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Windows.Documents;

namespace SonarLint.VisualStudio.Education.Layout.Visual.Tabs
{
    /// <summary>
    /// Repository for all created tabs
    /// </summary>
    internal interface ITabsRepository
    {
        /// <summary>
        /// Saves the tab in the repository. Uses tab.Name as the key
        /// </summary>
        /// <param name="tab">Tab block</param>
        void RegisterTab(Block tab);
        /// <summary>
        /// Retrieves the tab by it's key
        /// </summary>
        /// <param name="name">Name of the tab</param>
        /// <param name="tab">The resulting tab</param>
        /// <returns>true if tab is present, false if not found</returns>
        bool TryGetTab(string name, out Block tab);
        /// <summary>
        /// Removes all of the saved tabs
        /// </summary>
        void Clear();
    }
}
