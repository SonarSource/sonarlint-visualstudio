/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using SonarLint.VisualStudio.Integration.Vsix.Resources;
using System;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public abstract class RoslynRuntimeOptions
    {
        public static readonly RoslynRuntimeOptions VS2015 = new Vs2015RoslynRuntimeOptions();
        public static readonly RoslynRuntimeOptions VS2017 = new Vs2017RoslynRuntimeOptions();

        public abstract string RuntimeOptionsFeatureName { get; }
        public abstract string FullSolutionAnalysisOptionName { get; }

        public static RoslynRuntimeOptions Resolve(IServiceProvider serviceProvider)
        {
            var visualStudioVersion = serviceProvider.GetService<EnvDTE.DTE>()?.Version;
            switch (visualStudioVersion)
            {
                case VisualStudioConstants.VS2015VersionNumber:
                    return VS2015;
                case VisualStudioConstants.VS2017VersionNumber:
                    return VS2017;
                default:
                    VsShellUtils.WriteToSonarLintOutputPane(serviceProvider, Strings.InvalidVisualStudioVersion, visualStudioVersion);
                    return null;
            }
        }

        private class Vs2015RoslynRuntimeOptions : RoslynRuntimeOptions
        {
            public override string RuntimeOptionsFeatureName { get; } = "Runtime";
            public override string FullSolutionAnalysisOptionName { get; } = "Full Solution Analysis";
        }

        private class Vs2017RoslynRuntimeOptions : RoslynRuntimeOptions
        {
            public override string RuntimeOptionsFeatureName { get; } = "RuntimeOptions";
            public override string FullSolutionAnalysisOptionName { get; } = "FullSolutionAnalysis";
        }
    }
}
