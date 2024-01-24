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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.JsTs;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.VsVersion;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration
{
    public interface ITelemetryPayloadCreator
    {
        TelemetryPayload Create(TelemetryData telemetryData);
    }

    [Export(typeof(ITelemetryPayloadCreator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class TelemetryPayloadCreator : ITelemetryPayloadCreator
    {
        private readonly ICurrentTimeProvider currentTimeProvider;
        private readonly IActiveSolutionBoundTracker solutionBindingTracker;
        private readonly INodeVersionInfoProvider nodeVersionInfoProvider;
        private readonly ICompatibleNodeLocator compatibleNodeLocator;
        private readonly IVsVersionProvider vsVersionProvider;

        [ImportingConstructor]
        public TelemetryPayloadCreator(IActiveSolutionBoundTracker solutionBindingTracker,
            IVsVersionProvider vsVersionProvider,
            INodeVersionInfoProvider nodeVersionInfoProvider,
            ICompatibleNodeLocator compatibleNodeLocator)
            : this(solutionBindingTracker,
                vsVersionProvider,
                nodeVersionInfoProvider,
                compatibleNodeLocator,
                DefaultCurrentTimeProvider.Instance)
        {
        }

        internal TelemetryPayloadCreator(IActiveSolutionBoundTracker solutionBindingTracker,
            IVsVersionProvider vsVersionProvider,
            INodeVersionInfoProvider nodeVersionInfoProvider,
            ICompatibleNodeLocator compatibleNodeLocator,
            ICurrentTimeProvider currentTimeProvider)
        {
            this.solutionBindingTracker = solutionBindingTracker;
            this.vsVersionProvider = vsVersionProvider;
            this.nodeVersionInfoProvider = nodeVersionInfoProvider;
            this.compatibleNodeLocator = compatibleNodeLocator;
            this.currentTimeProvider = currentTimeProvider;
        }

        private static readonly string SonarLintVersion = GetSonarLintVersion();

        private static string GetSonarLintVersion()
        {
            return FileVersionInfo.GetVersionInfo(typeof(TelemetryTimer).Assembly.Location).FileVersion;
        }

        internal static bool IsSonarCloud(Uri sonarqubeUri)
        {
            if (sonarqubeUri == null)
            {
                return false;
            }

            return sonarqubeUri.Equals("https://sonarcloud.io/") ||
                   sonarqubeUri.Equals("https://www.sonarcloud.io/");
        }

        public TelemetryPayload Create(TelemetryData telemetryData)
        {
            if (telemetryData == null)
            {
                throw new ArgumentNullException(nameof(telemetryData));
            }

            // Note: we are capturing the data about the connected mode at the point
            // the data is about to be sent. This seems weird, as it depends entirely
            // on the solution the user happens to have open at the time, if any.
            // However, this is what was spec-ed in the MMF.
            var bindingConfiguration = solutionBindingTracker.CurrentConfiguration;
            var isConnected = bindingConfiguration?.Mode != SonarLintMode.Standalone;
            var isLegacyConnected = bindingConfiguration?.Mode == SonarLintMode.LegacyConnected;
            var isSonarCloud = IsSonarCloud(bindingConfiguration?.Project?.ServerUri);

            return new TelemetryPayload
            {
                SonarLintProduct = "SonarLint Visual Studio",
                SonarLintVersion = SonarLintVersion,
                VisualStudioVersion = VisualStudioHelpers.VisualStudioVersion,
                VisualStudioVersionInformation = GetVsVersion(vsVersionProvider.Version),
                NumberOfDaysSinceInstallation = currentTimeProvider.Now.DaysPassedSince(telemetryData.InstallationDate),
                NumberOfDaysOfUse = telemetryData.NumberOfDaysOfUse,
                IsUsingConnectedMode = isConnected,
                IsUsingLegacyConnectedMode = isLegacyConnected,
                IsUsingSonarCloud = isSonarCloud,
                SystemDate = currentTimeProvider.Now,
                InstallDate = telemetryData.InstallationDate,
                Analyses = telemetryData.Analyses,
                ShowHotspot = telemetryData.ShowHotspot,
                TaintVulnerabilities = telemetryData.TaintVulnerabilities,
                ServerNotifications = isConnected ? telemetryData.ServerNotifications : null,
                CFamilyProjectTypes = telemetryData.CFamilyProjectTypes,
                RulesUsage = telemetryData.RulesUsage,
                CompatibleNodeJsVersion = compatibleNodeLocator.Locate()?.Version.ToString(),
                MaxNodeJsVersion = nodeVersionInfoProvider.GetAllNodeVersions().Max(x => x.Version)?.ToString()
            };
        }

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
