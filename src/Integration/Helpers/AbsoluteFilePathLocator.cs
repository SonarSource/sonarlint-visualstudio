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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    [Export(typeof(IAbsoluteFilePathLocator))]
    internal class AbsoluteFilePathLocator : IAbsoluteFilePathLocator
    {
        private readonly IVsUIShellOpenDocument vsUiShellOpenDocument;

        [ImportingConstructor]
        public AbsoluteFilePathLocator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            vsUiShellOpenDocument = serviceProvider.GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>();
        }

        public string Locate(string relativeFilePath)
        {
            if (relativeFilePath == null)
            {
                throw new ArgumentNullException(nameof(relativeFilePath));
            }

            relativeFilePath = relativeFilePath.TrimStart('\\');

            var absoluteFilePaths = new string[1];

            var hr = vsUiShellOpenDocument.SearchProjectsForRelativePath(
                (uint) __VSRELPATHSEARCHFLAGS.RPS_UseAllSearchStrategies,
                relativeFilePath,
                absoluteFilePaths);

            return hr == VSConstants.S_OK ? absoluteFilePaths[0] : null;
        }
    }
}
