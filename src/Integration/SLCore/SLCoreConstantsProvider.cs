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
using SonarLint.VisualStudio.Core.VsInfo;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;

namespace SonarLint.VisualStudio.Integration.SLCore
{
    [Export(typeof(ISLCoreConstantsProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SLCoreConstantsProvider : ISLCoreConstantsProvider
    {
        private readonly IVsInfoProvider vsInfoProvider;

        [ImportingConstructor]
        public SLCoreConstantsProvider(IVsInfoProvider vsInfoProvider)
        {
            this.vsInfoProvider = vsInfoProvider;
        }

        public ClientConstantsDto ClientConstants => new(vsInfoProvider.Name, $"SonarLint Visual Studio/{VersionHelper.SonarLintVersion}", Process.GetCurrentProcess().Id);

        public FeatureFlagsDto FeatureFlags => new(true, true, true, true, false, false, true, false);

        public TelemetryClientConstantAttributesDto TelemetryConstants => new("visualstudio", "SonarLint Visual Studio", VersionHelper.SonarLintVersion, VisualStudioHelpers.VisualStudioVersion, new Dictionary<string, object>
        {
            { "slvs_ide_info", GetVsVersion(vsInfoProvider.Version) }
        });

        public IReadOnlyList<Language> LanguagesInStandaloneMode =>
        [
            Language.JS,
            Language.TS,
            Language.CSS,
            Language.C,
            Language.CPP,
            Language.CS,
            Language.VBNET,
            Language.SECRETS,
        ];

        public IReadOnlyList<Language> SLCoreAnalyzableLanguages =>
        [
            Language.SECRETS
        ];
        
        private static IdeVersionInformation GetVsVersion(IVsVersion vsVersion)
        {
            if (vsVersion == null)
            {
                return null;
            }

            return new IdeVersionInformation
            {
                DisplayName = vsVersion.DisplayName,
                InstallationVersion = vsVersion.InstallationVersion,
                DisplayVersion = vsVersion.DisplayVersion
            };
        }
    }
}
