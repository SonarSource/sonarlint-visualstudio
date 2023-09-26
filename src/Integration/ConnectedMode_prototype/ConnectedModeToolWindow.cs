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

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.ConnectedMode_prototype
{
    [Guid(ToolWindowIdAsString)]
    internal class ConnectedModeToolWindow : ToolWindowPane
    {
        private const string ToolWindowIdAsString = "0eddcf48-0951-4546-b5c9-c5c2b583a6d4";
        public static readonly Guid ToolWindowId = new Guid(ToolWindowIdAsString);

        public ConnectedModeToolWindow(ISectionController sectionController)
        {
            Content = sectionController.View;
            Caption = "Sonar Connected Mode"; // TODO - add to resource file
        }
    }
}
