/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Integration.LocalServices;

[Export(typeof(ICanonicalFilePathProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class CanonicalFilePathProvider(ICanonicalFilePathsCache canonicalFilePathsCache, IVsUIServiceOperation uiServiceOperation)
    : ICanonicalFilePathProvider
{
    private static readonly StringComparer PathsComparer = StringComparer.OrdinalIgnoreCase;

    public string GetCanonicalPath(string originalFilePath)
    {
        if (string.IsNullOrWhiteSpace(originalFilePath))
        {
            return null;
        }

        if (canonicalFilePathsCache.TryGet(originalFilePath, out var canonicalName))
        {
            return canonicalName;
        }

        if (GetFromRdt(originalFilePath) is { } rdtCanonicalPath)
        {
            canonicalFilePathsCache.Add(rdtCanonicalPath);
            return rdtCanonicalPath;
        }

        if (GetFromSolution(originalFilePath) is { } solutionCanonicalPath)
        {
            canonicalFilePathsCache.Add(solutionCanonicalPath);
            return solutionCanonicalPath;
        }

        // if we can't find it anywhere, means it's not part of the project so its file path is the canonical path
        return originalFilePath;
    }

    private string GetFromRdt(string originalFilePath) =>
        uiServiceOperation.Execute<SVsRunningDocumentTable, IVsRunningDocumentTable, string>(rdt =>
        {
            if (rdt.FindAndLockDocument(
                    (uint)_VSRDTFLAGS.RDT_NoLock, // only get the hierarchy info, no file locking
                    originalFilePath,
                    out var hierarchy,
                    out var itemId,
                    out _,
                    out _) is not VSConstants.S_OK)
            {
                return null;
            }
            if (itemId is VSConstants.VSITEMID_NIL)
            {
                return null;
            }
            if (hierarchy is not IVsProject project)
            {
                return null;
            }
            if (project.GetMkDocument(itemId, out var canonicalFilePath) is not VSConstants.S_OK)
            {
                return null;
            }
            if (!PathsComparer.Equals(originalFilePath, canonicalFilePath))
            {
                return null;
            }
            return canonicalFilePath;
        });

    private string GetFromSolution(string originalFilePath) =>
        uiServiceOperation.Execute<SDTE, DTE2, string>(dte =>
        {
            if (dte?.Solution?.FindProjectItem(originalFilePath)?.FileNames[0] is not { } canonicalFilePath)
            {
                return null;
            }
            if (!PathsComparer.Equals(originalFilePath, canonicalFilePath))
            {
                return null;
            }
            return canonicalFilePath;
        });
}
