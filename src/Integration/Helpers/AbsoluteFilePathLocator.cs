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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    [Export(typeof(IAbsoluteFilePathLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AbsoluteFilePathLocator : IAbsoluteFilePathLocator
    {
        private readonly IVsUIServiceOperation vSServiceOperation;

        [ImportingConstructor]
        public AbsoluteFilePathLocator(IVsUIServiceOperation vSServiceOperation)
            => this.vSServiceOperation = vSServiceOperation;

        public string Locate(string relativeFilePath)
        {
            if (relativeFilePath == null)
            {
                throw new ArgumentNullException(nameof(relativeFilePath));
            }

            relativeFilePath = relativeFilePath.Replace("/", "\\");
            relativeFilePath = relativeFilePath.TrimStart('\\');

            var result = vSServiceOperation.Execute<SVsUIShellOpenDocument, IVsUIShellOpenDocument, string>(vsUiShellOpenDocument =>
            {
                var absoluteFilePaths = new string[1];

                var hr = vsUiShellOpenDocument.SearchProjectsForRelativePath(
                    (uint)__VSRELPATHSEARCHFLAGS.RPS_UseAllSearchStrategies,
                    relativeFilePath,
                    absoluteFilePaths);

                return hr == VSConstants.S_OK ? absoluteFilePaths[0] : null;
            });

            return result;
        }
    }
}
