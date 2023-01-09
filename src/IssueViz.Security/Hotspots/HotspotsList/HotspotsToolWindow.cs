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
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList
{
    [Guid(ToolWindowIdAsString)]
    public class HotspotsToolWindow : ToolWindowPane
    {
        private const string ToolWindowIdAsString = "4BCD4392-DBCF-4AA2-9852-01129D229CD8";
        public static readonly Guid ToolWindowId = new Guid(ToolWindowIdAsString);

        public HotspotsToolWindow(IServiceProvider serviceProvider)
        {
            Caption = Resources.HotspotsToolWindowCaption;

            var componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;

            var store = componentModel.GetService<IHotspotsStore>();
            var locationNavigator = componentModel.GetService<ILocationNavigator>();
            var selectionService = componentModel.GetService<IIssueSelectionService>();

            var viewModel = new HotspotsControlViewModel(store, locationNavigator, selectionService);
            var hotspotsControl = new HotspotsControl(viewModel);

            Content = hotspotsControl;
        }
    }
}
