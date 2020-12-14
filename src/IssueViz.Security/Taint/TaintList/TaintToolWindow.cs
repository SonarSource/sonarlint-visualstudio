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
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList.ViewModels;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList
{
    [Guid(ToolWindowIdAsString)]
    public class TaintToolWindow : ToolWindowPane
    {
        private const string ToolWindowIdAsString = "537833A5-E0F1-4405-821D-D83D89370B78";
        public static readonly Guid ToolWindowId = new Guid(ToolWindowIdAsString);

        public TaintToolWindow(IServiceProvider serviceProvider)
        {
            Caption = Resources.TaintToolWindowCaption;

            var componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            var store = componentModel.GetService<ITaintStore>();
            var locationNavigator = componentModel.GetService<ILocationNavigator>();

            var viewModel = new TaintIssuesControlViewModel(store, locationNavigator);

            Content = new TaintIssuesControl(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var vsWindowFrame = Frame as IVsWindowFrame;
                vsWindowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
            }

            base.Dispose(disposing);
        }
    }
}
