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

namespace SonarLint.VisualStudio.Core
{
    public class EnvironmentSettings : IEnvironmentSettings
    {
        internal const string TreatBlockerAsErrorEnvVar = "SONAR_INTERNAL_TREAT_BLOCKER_AS_ERROR";
        internal const string AnalysisTimeoutEnvVar = "SONARLINT_INTERNAL_ANALYSIS_TIMEOUT_MS";
        internal const string PchGenerationTimeoutEnvVar = "SONARLINT_INTERNAL_PCH_GENERATION_TIMEOUT_MS";
        internal const string LogDebugMessagesEnvVar = "SONARLINT_INTERNAL_LOG_DEBUG";

        public const string SonarLintDownloadUrlEnvVar = "SONARLINT_DAEMON_DOWNLOAD_URL";

        public bool TreatBlockerSeverityAsError()
            => ParseBool(TreatBlockerAsErrorEnvVar);

        public int AnalysisTimeoutInMs()
            => ParseInt(Environment.GetEnvironmentVariable(AnalysisTimeoutEnvVar));

        public int PCHGenerationTimeoutInMs(int defaultValue)
        {
            var userValue = ParseInt(Environment.GetEnvironmentVariable(PchGenerationTimeoutEnvVar));

            return userValue > 0 ? userValue : defaultValue;
        }

        public bool ShouldLogDebugMessages() 
            => ParseBool(LogDebugMessagesEnvVar);

        // The URL validation and logging is being done by the daemon installer, so
        // this is just a passthrough       
        public string SonarLintDaemonDownloadUrl()
            => Environment.GetEnvironmentVariable(SonarLintDownloadUrlEnvVar);

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
