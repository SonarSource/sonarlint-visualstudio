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

using System.Linq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Integration.ETW;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    /// <summary>
    /// Handles messages returned by the CFamily subprocess.exe
    /// </summary>
    internal interface IMessageHandler
    {
        /// <summary>
        /// The number of analysis issues processed by the message handler
        /// </summary>
        /// <remarks>Messages with internal rule keys and message for files other than the
        /// file being analyzed are ignored</remarks>
        int IssueCount { get;  }

        /// <summary>
        /// True if the analysis completed successfully, otherwise false
        /// </summary>
        /// <remarks>The analysis will be treated as having failed if any "error" internal messages are received</remarks>
        bool AnalysisSucceeded { get; }

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

        public bool AnalysisSucceeded => true;

        public void HandleMessage(Message message) { /* no-op */ }
    }

    internal class MessageHandler : IMessageHandler
    {
        private readonly IRequest request;
        private readonly IIssueConsumer issueConsumer;
        private readonly ICFamilyIssueToAnalysisIssueConverter issueConverter;
        private readonly ILogger logger;

        public int IssueCount { get; private set; }

        public bool AnalysisSucceeded { get; private set; } = true;

        public MessageHandler(IRequest request, IIssueConsumer issueConsumer, ICFamilyIssueToAnalysisIssueConverter issueConverter, ILogger logger)
        {
            this.request = request;
            this.issueConsumer = issueConsumer;
            this.issueConverter = issueConverter;
            this.logger = logger;
        }

        public void HandleMessage(Message message)
        {
            CodeMarkers.Instance.HandleMessageStart(request.Context.File);

            // Handle known internal rule keys - used to return info/warnings
            switch (message.RuleKey)
            {
                case "internal.UnsupportedConfig": // the user has specified an unsupported configuration option - log it
                    AnalysisSucceeded = false;
                    logger.WriteLine(CFamilyStrings.MsgHandler_ReportUnsupportedConfiguration, message.Text);
                    break;

                case "internal.InvalidInput": // subprocess has been called incorrectly by SonarLint
                    AnalysisSucceeded = false;
                    logger.WriteLine(CFamilyStrings.MsgHandler_ReportInvalidInput, message.Text);
                    break;

                case "internal.UnexpectedFailure": // unexpected failure in the subprocess
                    AnalysisSucceeded = false;
                    logger.WriteLine(CFamilyStrings.MsgHandler_ReportUnexpectedFailure, message.Text);
                    break;

                case "internal.fileDependency": // not currently handled. See https://github.com/SonarSource/sonarlint-visualstudio/issues/2611
                    break;

                default: // assume anything else is an analysis issue
                    HandleAnalysisIssue(message);
                    break;
            }

            CodeMarkers.Instance.HandleMessageStop();
        }

        private void HandleAnalysisIssue(Message message)
        {
            if (string.IsNullOrEmpty(message.Filename) // info/error messages might not have a file name
                || !PathHelper.IsMatchingPath(message.Filename, request.Context.File)) // Ignore issues for other files (e.g. issues reported against header when analysing a source file)
            {
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
