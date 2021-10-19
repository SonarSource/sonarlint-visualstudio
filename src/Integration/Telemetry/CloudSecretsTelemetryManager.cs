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

using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.CloudSecrets;

namespace SonarLint.VisualStudio.Integration.Telemetry
{
    [Export(typeof(ICloudSecretsTelemetryManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CloudSecretsTelemetryManager : ICloudSecretsTelemetryManager
    {
        private readonly ITelemetryDataRepository telemetryDataRepository;
        private static readonly object Lock = new object();

        [ImportingConstructor]
        public CloudSecretsTelemetryManager(ITelemetryDataRepository telemetryDataRepository)
        {
            this.telemetryDataRepository = telemetryDataRepository;
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
                        rulesUsage.RulesThatRaisedIssues = rulesUsage.RulesThatRaisedIssues.Distinct().ToList();

                        telemetryDataRepository.Save();
                    }
                }
            }
        }
    }
}
