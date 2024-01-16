/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Integration.Vsix.Helpers
{
    internal interface IStatusBarNotifier
    {
        void Notify(string message, bool showSpinner);
    }

    [Export(typeof(IStatusBarNotifier))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class StatusBarNotifier : IStatusBarNotifier
    {
        private readonly IVsUIServiceOperation vSServiceOperation;

        [ImportingConstructor]
        public StatusBarNotifier(IVsUIServiceOperation vSServiceOperation)
        {
            this.vSServiceOperation = vSServiceOperation;
        }

        public void Notify(string message, bool showSpinner)
        {
            vSServiceOperation.Execute<IVsStatusbar, IVsStatusbar>(vsStatusBar => DoNotify(vsStatusBar, message, showSpinner));
        }

        private void DoNotify(IVsStatusbar vsStatusBar, string message, bool showSpinner)
        {
            object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General;
            vsStatusBar.Animation(showSpinner ? 1 : 0, ref icon);

            vsStatusBar.SetText(message);
        }
    }
}
