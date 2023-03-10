﻿/*
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

using System;
using System.Windows.Documents;
using SonarLint.VisualStudio.Education.Layout.Visual;

namespace SonarLint.VisualStudio.Education.Layout.Visual.Tabs
{
    /// <summary>
    /// Represents individual tab of a TabGroup
    /// </summary>
    internal class TabItem // NOTE: this does not implement IAbstractVisualizationTreeNode by design, as it cannot be used without TabGroup
    {
        internal /* for testing */ readonly IAbstractVisualizationTreeNode content;

        public TabItem(string displayName, IAbstractVisualizationTreeNode content)
        {
            // Name = name;
            DisplayName = displayName;
            this.content = content;
        }

        // public string Name { get; }
        public string DisplayName { get; }

        public Block CreateVisualization(string tabGroupName)
        {
            throw new NotImplementedException();
            // return new Section(content.CreateVisualization()) { Name = TabNameProvider.GetTabSectionName(tabGroupName, Name) };
        }
    }
}
