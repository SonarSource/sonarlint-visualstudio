/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.TypeScript.TsConfig
{
    internal interface ITsConfigsLocator
    {
        /// <summary>
        /// Returns all the tsconfig files in the current solution.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> Locate();
    }

    [Export(typeof(ITsConfigsLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class TsConfigsLocator : ITsConfigsLocator
    {
        internal const int MaxNumberOfFiles = 1000;

        private readonly IVsUIShellOpenDocument vsUiShellOpenDocument;

        [ImportingConstructor]
        public TsConfigsLocator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            vsUiShellOpenDocument = serviceProvider.GetService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
        }

        public IEnumerable<string> Locate()
        {
            var foundFiles = new string[MaxNumberOfFiles];

            var hr = vsUiShellOpenDocument.SearchProjectsForRelativePath(
                (uint) __VSRELPATHSEARCHFLAGS.RPS_UseAllSearchStrategies,
                "tsconfig.json",
                foundFiles);

            return hr != VSConstants.S_OK
                ? Enumerable.Empty<string>()
                : foundFiles.Where(x => !string.IsNullOrEmpty(x));
        }
    }
}
