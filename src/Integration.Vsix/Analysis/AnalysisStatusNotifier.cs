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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    [Export(typeof(IAnalysisStatusNotifier))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class AnalysisStatusNotifier : IAnalysisStatusNotifier
    {
        private readonly IVsStatusbar vsStatusBar;

        [ImportingConstructor]
        public AnalysisStatusNotifier([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            vsStatusBar = serviceProvider.GetService(typeof(IVsStatusbar)) as IVsStatusbar;
        }

        public void AnalysisStarted(string filePath)
        {
            ShowSpinner();
            ShowStatus(AnalysisStrings.Notifier_AnalysisStarted, filePath);
        }

        public void AnalysisFinished(string filePath)
        {
            HideSpinner();
            ShowStatus(AnalysisStrings.Notifier_AnalysisEnded, filePath);
        }

        public void AnalysisCancelled(string filePath)
        {
            HideSpinner();
            ShowStatus(AnalysisStrings.Notifier_AnalysisCancelled, filePath);
        }

        public void AnalysisFailed(string filePath)
        {
            HideSpinner();
            ShowStatus(AnalysisStrings.Notifier_AnalysisFailed, filePath);
        }

        private void ShowStatus(string text, params object[] args)
        {
            vsStatusBar.SetText(string.Format(text, args));
        }

        private void ShowSpinner()
        {
            vsStatusBar.Animation(1, Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General);
        }

        private void HideSpinner()
        {
            vsStatusBar.Animation(0, Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General);
        }
    }
}
