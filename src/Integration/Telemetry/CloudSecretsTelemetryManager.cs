/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Linq;
using SonarLint.VisualStudio.CloudSecrets;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Telemetry
{
    [Export(typeof(ICloudSecretsTelemetryManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class CloudSecretsTelemetryManager : ICloudSecretsTelemetryManager, IDisposable
    {
        private const string SecretsRepositoryKey = "secrets:";

        private readonly ITelemetryDataRepository telemetryDataRepository;
        private readonly IUserSettingsProvider userSettingsProvider;
        private readonly ITelemetryEvents telemetryEvents;
        private static readonly object Lock = new object();

        [ImportingConstructor]
        public CloudSecretsTelemetryManager(ITelemetryDataRepository telemetryDataRepository, 
            IUserSettingsProvider userSettingsProvider,
            ITelemetryEvents telemetryEvents)
        {
            this.telemetryDataRepository = telemetryDataRepository;
            this.userSettingsProvider = userSettingsProvider;
            this.telemetryEvents = telemetryEvents;

            telemetryEvents.BeforeTelemetrySent += TelemetryEvents_BeforeTelemetrySent;
        }

        public void SecretDetected(string ruleId)
        {
            var rulesUsage = telemetryDataRepository.Data.RulesUsage;

            if (!rulesUsage.RulesThatRaisedIssues.Contains(ruleId))
            {
                lock (Lock)
                {
                    if (!rulesUsage.RulesThatRaisedIssues.Contains(ruleId))
                    {
                        rulesUsage.RulesThatRaisedIssues.Add(ruleId);
                        rulesUsage.RulesThatRaisedIssues = rulesUsage.RulesThatRaisedIssues.Distinct().OrderBy(x => x).ToList();
                        telemetryDataRepository.Save();
                    }
                }
            }
        }

        private void TelemetryEvents_BeforeTelemetrySent(object sender, EventArgs e)
        {
            var disabledSecretRules = userSettingsProvider.UserSettings.RulesSettings.Rules
                .Where(x => x.Key.StartsWith(SecretsRepositoryKey) && x.Value.Level == RuleLevel.Off)
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToList();

            telemetryDataRepository.Data.RulesUsage.EnabledByDefaultThatWereDisabled = disabledSecretRules;
            telemetryDataRepository.Save();
        }

        public void Dispose()
        {
            telemetryEvents.BeforeTelemetrySent -= TelemetryEvents_BeforeTelemetrySent;
        }
    }
}
