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
using System.Diagnostics;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration
{
    public static class TelemetryHelper
    {
        private static readonly string SonarLintVersion = GetSonarLintVersion();

        private static string GetSonarLintVersion()
        {
            return FileVersionInfo.GetVersionInfo(typeof(TelemetryTimer).Assembly.Location).FileVersion;
        }

        public static bool IsSonarCloud(Uri sonarqubeUri)
        {
            if (sonarqubeUri == null)
            {
                return false;
            }

            return sonarqubeUri.Equals("https://sonarcloud.io/") ||
                sonarqubeUri.Equals("https://www.sonarcloud.io/");
        }

        public static TelemetryPayload CreatePayload(TelemetryData telemetryData, DateTimeOffset now,
            BindingConfiguration bindingConfiguration)
        {
            if (telemetryData == null)
            {
                throw new ArgumentNullException(nameof(telemetryData));
            }

            if (bindingConfiguration == null)
            {
                throw new ArgumentNullException(nameof(bindingConfiguration));
            }

            // Note: we are capturing the data about the connected mode at the point
            // the data is about to be sent. This seems weird, as it depends entirely
            // on the solution the user happens to have open at the time, if any.
            // However, this is what was spec-ed in the MMF.
            var isConnected = bindingConfiguration?.Mode != SonarLintMode.Standalone;
            var isLegacyConnected = bindingConfiguration?.Mode == SonarLintMode.LegacyConnected;
            var isSonarCloud = IsSonarCloud(bindingConfiguration?.Project?.ServerUri);

            return new TelemetryPayload
            {
                SonarLintProduct = "SonarLint Visual Studio",
                SonarLintVersion = SonarLintVersion,
                VisualStudioVersion = VisualStudioHelpers.VisualStudioVersion,
                NumberOfDaysSinceInstallation = now.DaysPassedSince(telemetryData.InstallationDate),
                NumberOfDaysOfUse = telemetryData.NumberOfDaysOfUse,
                IsUsingConnectedMode = isConnected,
                IsUsingLegacyConnectedMode = isLegacyConnected,
                IsUsingSonarCloud = isSonarCloud,
                SystemDate = now,
                InstallDate = telemetryData.InstallationDate,
                Analyses = telemetryData.Analyses
            };
        }

        public static string Serialize(TelemetryPayload payload)
        {
            return JsonHelper.Serialize(payload);
        }
    }
}
