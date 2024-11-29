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
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

public interface IFileInSolutionIndicator
{
    bool IsFileInSolution(ProjectItem projectItem);
}

[Export(typeof(IFileInSolutionIndicator))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class FileInSolutionIndicator : IFileInSolutionIndicator
{
    public bool IsFileInSolution(ProjectItem projectItem)
    {
        try
        {
            // Issue 667:  https://github.com/SonarSource/sonarlint-visualstudio/issues/667
            // If you open a C++ file that is not part of the current solution then
            // VS will cruft up a temporary vcxproj so that it can provide language
            // services for the file (e.g. syntax highlighting). This means that
            // even though we have what looks like a valid project item, it might
            // not actually belong to a real project.
            var indexOfSingleFileString = projectItem?.ContainingProject?.FullName.IndexOf("SingleFileISense", StringComparison.OrdinalIgnoreCase);
            return indexOfSingleFileString.HasValue &&
                   indexOfSingleFileString.Value <= 0 &&
                   projectItem.ConfigurationManager != null &&
                   // the next line will throw if the file is not part of a solution
                   projectItem.ConfigurationManager.ActiveConfiguration != null;
        }
        catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
        {
            // Suppress non-critical exceptions
        }
        return false;
    }
}
