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
using System.IO;
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
            Notify(AnalysisStrings.Notifier_AnalysisStarted, filePath, true);
        }

        public void AnalysisFinished(string filePath)
        {
            Notify(AnalysisStrings.Notifier_AnalysisFinished, filePath, false);
        }

        public void AnalysisTimedOut(string filePath)
        {
            Notify(AnalysisStrings.Notifier_AnalysisTimedOut, filePath, false);
        }

        public void AnalysisFailed(string filePath)
        {
            Notify(AnalysisStrings.Notifier_AnalysisFailed, filePath, false);
        }

        private void Notify(string messageFormat, string filePath, bool showSpinner)
        {
            RunOnUIThread(() =>
            {
                object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General;
                vsStatusBar.Animation(showSpinner ? 1 : 0, ref icon);

                var fileName = Path.GetFileName(filePath);
                var message = string.Format(messageFormat, fileName);
                vsStatusBar.SetText(message);
            });
        }

        private static void RunOnUIThread(Action op)
        {
            if (ThreadHelper.CheckAccess())
            {
                op();
                return;
            }
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                op();
            });
        }
    }
}
