/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix.Settings;

internal static class OpenDocumentService
{
    [ExcludeFromCodeCoverage]
    internal static void OpenDocumentInVs(IServiceProvider serviceProvider, string filePath)
    {
        // TryOpenDocument calls several other VS services. From a testing point of view, it's simpler to
        // create a subclass and override this method.
        var viewType = Guid.Empty;
        VsShellUtilities.TryOpenDocument(serviceProvider, filePath, viewType, out _, out _, out _);
    }
}
