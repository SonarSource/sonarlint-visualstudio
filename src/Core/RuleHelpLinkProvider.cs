/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using System.Web;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Core
{
    public interface IRuleHelpLinkProvider
    {
        string GetHelpLink(SonarCompositeRuleId ruleId);
    }

    [Export(typeof(IRuleHelpLinkProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    public class RuleHelpLinkProvider(IActiveSolutionBoundTracker activeSolutionBoundTracker) : IRuleHelpLinkProvider
    {
        public string GetHelpLink(SonarCompositeRuleId ruleId) =>
            // ruleKey is in format "javascript:S1234" (or javascript:SOMETHING for legacy keys)
            // NB: there are some "common" rules that are implemented on the server-side. We do
            // need to handle these case as they will never be raised in the IDE (and don't seem
            // to be documented on the rule site anyway).
            //   e.g. common-c:DuplicatedBlocks, common-cpp:FailedUnitTests
            activeSolutionBoundTracker.CurrentConfiguration?.Project?.ServerConnection switch
            {
                ServerConnection.SonarCloud sc => BuildRuleHelpUri(sc.Id, "rules", ruleId),
                ServerConnection.SonarQube sq => BuildRuleHelpUri(sq.Id, "coding_rules", ruleId),
                _ => null
            };

        private static string BuildRuleHelpUri(string baseUri, string path, SonarCompositeRuleId ruleId) =>
            $"{baseUri.TrimEnd('/')}/{path}?languages={Uri.EscapeDataString(ruleId.Language.ServerLanguageKey)}&open={Uri.EscapeDataString(ruleId.Id)}&q={Uri.EscapeDataString(ruleId.RuleKey)}";
    }
}
