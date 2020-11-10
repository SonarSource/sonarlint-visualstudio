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
using System.ComponentModel.Composition;
using System.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api
{
    internal interface IOpenInIDEFailureInfoBar
    {
        void Show(Guid toolWindowId);

        void Clear();
    }

    [Export(typeof(IOpenInIDEFailureInfoBar))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class OpenInIDEFailureInfoBar : IOpenInIDEFailureInfoBar, IDisposable
    {
        private readonly IInfoBarManager infoBarManager;
        private readonly IOutputWindowService outputWindowService;
        private IInfoBar currentInfoBar;

        [ImportingConstructor]
        public OpenInIDEFailureInfoBar(IInfoBarManager infoBarManager,
            IOutputWindowService outputWindowService)
        {
            this.infoBarManager = infoBarManager;
            this.outputWindowService = outputWindowService;
        }

        public void Show(Guid toolWindowId)
        {
            RemoveExistingInfoBar();
            AddInfoBar(toolWindowId);
        }

        public void Clear()
        {
            RemoveExistingInfoBar();
        }

        private void AddInfoBar(Guid toolWindowId)
        {
            currentInfoBar = infoBarManager.AttachInfoBarWithButton(toolWindowId, OpenInIDEResources.RequestValidator_InfoBarMessage, "Show Output Window", default);
            Debug.Assert(currentInfoBar != null, "currentInfoBar != null");

            currentInfoBar.ButtonClick += ShowOutputWindow;
            currentInfoBar.Closed += CurrentInfoBar_Closed;
        }

        private void ShowOutputWindow(object sender, EventArgs e)
        {
            outputWindowService.Show();
        }

        private void RemoveExistingInfoBar()
        {
            if (currentInfoBar != null)
            {
                currentInfoBar.ButtonClick -= ShowOutputWindow;
                currentInfoBar.Closed -= CurrentInfoBar_Closed;
                infoBarManager.DetachInfoBar(currentInfoBar);
                currentInfoBar = null;
            }
        }

        private void CurrentInfoBar_Closed(object sender, EventArgs e)
        {
            RemoveExistingInfoBar();
        }

        public void Dispose()
        {
            RemoveExistingInfoBar();
        }
    }
}
