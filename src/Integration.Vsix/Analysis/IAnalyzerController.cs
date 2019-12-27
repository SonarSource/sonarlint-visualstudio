/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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

using System.Collections.Generic;
using EnvDTE;

// TODO: decide whether both of these interfaces are required

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public interface IAnalyzer
    {
        bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages);

        void ExecuteAnalysis(string path, string charset, IEnumerable<AnalysisLanguage> detectedLanguages, IIssueConsumer consumer, ProjectItem projectItem);
    }

    // Marker interface used by for MEF exporting/importing
    // (there are multiple implementations of IAnalyzer but only one implentation
    // of this interface).
    internal interface IAnalyzerController : IAnalyzer
    {
    }
}
