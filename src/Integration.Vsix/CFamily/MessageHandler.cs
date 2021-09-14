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

using System.Diagnostics;
using System.Linq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    /// <summary>
    /// Handles messages returned by the CFamily subprocess.exe
    /// </summary>
    internal interface IMessageHandler
    {
        int IssueCount { get;  }

        void HandleMessage(Message message);
    }

    /// <summary>
    /// No-op implementation - used when there is not a valid issue consumer
    /// </summary>
    internal class NoOpMessageHandler : IMessageHandler
    {
        /// <summary>
        /// Singleton no-op message handler
        /// </summary>
        public static readonly IMessageHandler Instance = new NoOpMessageHandler();

        public int IssueCount { get; } = 0;
        public void HandleMessage(Message message) { /* no-op */ }
    }

    internal class MessageHandler : IMessageHandler
    {
        private readonly IRequest request;
        private readonly IIssueConsumer issueConsumer;
        private readonly ICFamilyIssueToAnalysisIssueConverter issueConverter;

        public int IssueCount { get; private set; }

        public MessageHandler(IRequest request, IIssueConsumer issueConsumer, ICFamilyIssueToAnalysisIssueConverter issueConverter)
        {
            this.request = request;
            this.issueConsumer = issueConsumer;
            this.issueConverter = issueConverter;
        }

        public void HandleMessage(Message message)
        {
            if (!PathHelper.IsMatchingPath(message.Filename, request.Context.File))
            {
                // Ignore issues for other files (e.g. issues reported against header files
                // when analysing a source file)
                return;
            }

            if (!IsIssueForActiveRule(message, request.Context.RulesConfiguration))
            {
                return;
            }

            IssueCount++;
            var issue = issueConverter.Convert(message, request.Context.CFamilyLanguage, request.Context.RulesConfiguration);

            // Note: the file being analyzed might have been closed by the time the analysis results are 
            // returned. This doesn't cause a crash; all active taggers will have been detached from the
            // TextBufferIssueTracker when the file was closed, but the TextBufferIssueTracker will
            // still exist and handle the call.
            issueConsumer.Accept(request.Context.File, new[] { issue });
        }

        internal /* for testing */ static bool IsIssueForActiveRule(Message message, ICFamilyRulesConfig rulesConfiguration)
        {
            // Currently (v6.3) the subprocess.exe will always run the native CLang rules, so those issues
            // could be returned even if they were not activated in the profile.

            // In addition, in v6.4+ there are internal rules that are always enabled and will always return
            // issues. Filtering for active rules will also remove those internal issues since the corresponding
            // rules will never be active in a quality profile.
            return rulesConfiguration.ActivePartialRuleKeys.Contains(message.RuleKey, CFamilyShared.RuleKeyComparer);
        }
    }
}
