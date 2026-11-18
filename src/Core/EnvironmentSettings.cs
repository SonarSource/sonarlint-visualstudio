/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

namespace SonarLint.VisualStudio.Core
{
    public class EnvironmentSettings : IEnvironmentSettings
    {
        internal const string TreatBlockerAsErrorEnvVar = "SONAR_INTERNAL_TREAT_BLOCKER_AS_ERROR";
        internal const string AnalysisTimeoutEnvVar = "SONARLINT_INTERNAL_ANALYSIS_TIMEOUT_MS";
        internal const string PchGenerationTimeoutEnvVar = "SONARLINT_INTERNAL_PCH_GENERATION_TIMEOUT_MS";
        internal const string ConnectedModeAutomationCommandsEnvVar = "SONARLINT_INTERNAL_E2E_COMMANDS";
        internal const string TelemetryDisabledEnvVar = "SONARLINT_INTERNAL_TELEMETRY_DISABLED";

        /// <summary>
        /// The end-to-end tests start each Visual Studio instance with <c>/RootSuffix ExtensionE2E&lt;id&gt;</c>, an isolated
        /// experimental instance. The automation commands are only enabled in such an instance.
        /// </summary>
        internal const string ConnectedModeAutomationRootSuffixPrefix = "ExtensionE2E";

        private readonly Func<string[]> getCommandLineArgs;

        public EnvironmentSettings() : this(Environment.GetCommandLineArgs)
        {
        }

        internal /* for testing */ EnvironmentSettings(Func<string[]> getCommandLineArgs)
        {
            this.getCommandLineArgs = getCommandLineArgs;
        }

        public bool TreatBlockerSeverityAsError()
            => ParseBool(TreatBlockerAsErrorEnvVar);

        public int AnalysisTimeoutInMs()
            => ParseInt(Environment.GetEnvironmentVariable(AnalysisTimeoutEnvVar));

        public int PCHGenerationTimeoutInMs(int defaultValue)
        {
            var userValue = ParseInt(Environment.GetEnvironmentVariable(PchGenerationTimeoutEnvVar));

            return userValue > 0 ? userValue : defaultValue;
        }

        public bool AreConnectedModeAutomationCommandsEnabled()
            => ParseBool(ConnectedModeAutomationCommandsEnvVar) && IsRootSuffix(ConnectedModeAutomationRootSuffixPrefix);

        public bool IsTelemetryDisabled()
            => ParseBool(TelemetryDisabledEnvVar);

        /// <summary>
        /// True if the process was started with <c>/RootSuffix</c> (or <c>-RootSuffix</c>) followed by a value starting with <paramref name="prefix"/>.
        /// </summary>
        private bool IsRootSuffix(string prefix)
        {
            var args = getCommandLineArgs() ?? Array.Empty<string>();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "/RootSuffix", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(args[i], "-RootSuffix", StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1].StartsWith(prefix, StringComparison.Ordinal);
                }
            }
            return false;
        }

        private static int ParseInt(string setting)
        {
            if (int.TryParse(setting, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out int userSuppliedValue)
                && userSuppliedValue > 0)
            {
                return userSuppliedValue;
            }

            return 0;
        }

        private static bool ParseBool(string setting)
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable(setting), out var result))
            {
                return result;
            }
            return false;
        }
    }
}
