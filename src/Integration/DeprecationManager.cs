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
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core.InfoBar;

namespace SonarLint.VisualStudio.Integration
{
    public sealed class DeprecationManager : IDisposable
    {
        internal /* for testing purpose */ static readonly Guid DeprecationBarGuid = new Guid(ToolWindowGuids80.ErrorList);

        private readonly IInfoBarManager infoBarManager;
        private readonly ILogger sonarLintOutput;
        private IInfoBar deprecationBar;

        public DeprecationManager(IInfoBarManager infoBarManager, ILogger sonarLintOutput)
        {
            if (infoBarManager == null)
            {
                throw new ArgumentNullException(nameof(infoBarManager));
            }
            if (sonarLintOutput == null)
            {
                throw new ArgumentNullException(nameof(sonarLintOutput));
            }

            this.infoBarManager = infoBarManager;
            this.sonarLintOutput = sonarLintOutput;

            if (VisualStudioHelpers.IsVisualStudioBeforeUpdate3())
            {
                WriteMessageToOutput();
                ShowDeprecationBar();
            }
        }

        private void WriteMessageToOutput()
        {
            const string message =
                "*****************************************************************************************\r\n" +
                "***   SonarLint for Visual Studio versions 4.0+ will no longer support this version   ***\r\n" +
                "***         of Visual Studio. Please update to Visual Studio 2015 Update 3 or         ***\r\n" +
                "***               Visual Studio 2017 to benefit from new features.                    ***\r\n" +
                "*****************************************************************************************";

            sonarLintOutput.WriteLine(message);
        }

        private void ShowDeprecationBar()
        {
            const string message = "SonarLint for Visual Studio versions 4.0+ will no longer support this version of Visual " +
                "Studio. Please update to Visual Studio 2015 Update 3 or Visual Studio 2017 to benefit from new features.";
            deprecationBar = infoBarManager.AttachInfoBar(DeprecationBarGuid, message, default);
        }

        public void Dispose()
        {
            deprecationBar?.Close();
            deprecationBar = null;
        }
    }
}
