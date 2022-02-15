﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System.Threading;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes
{
    internal class QuickFixSuggestedAction : BaseSuggestedAction
    {
        internal const string sonarLintPrefix = "SonarLint: ";
        private readonly IQuickFixVisualization quickFixVisualization;
        private readonly ITextBuffer textBuffer;
        private readonly ISpanTranslator spanTranslator;
        private readonly IAnalysisIssueVisualization issueViz;
        private readonly ILogger logger;

        public QuickFixSuggestedAction(IQuickFixVisualization quickFixVisualization,
            ITextBuffer textBuffer, 
            IAnalysisIssueVisualization issueViz, 
            ILogger logger)
            : this(quickFixVisualization, textBuffer, issueViz, logger, new SpanTranslator()){

        }
        internal QuickFixSuggestedAction(IQuickFixVisualization quickFixVisualization,
            ITextBuffer textBuffer,
            IAnalysisIssueVisualization issueViz,
            ILogger logger,
            ISpanTranslator spanTranslator)
        {
            this.quickFixVisualization = quickFixVisualization;
            this.textBuffer = textBuffer;
            this.issueViz = issueViz;
            this.logger = logger;
            this.spanTranslator = spanTranslator;
        }

        public override string DisplayText => sonarLintPrefix + quickFixVisualization.Fix.Message;

        public override void Invoke(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!quickFixVisualization.CanBeApplied(textBuffer.CurrentSnapshot))
            {
                logger.LogDebug("[Quick Fixes] Quick fix cannot be applied as the text has changed. Issue: " + issueViz.RuleId);
                return;
            }

            var textEdit = textBuffer.CreateEdit();

            foreach (var edit in quickFixVisualization.EditVisualizations)
            {
                var updatedSpan = spanTranslator.TranslateTo(edit.Span, textBuffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive);
                
                textEdit.Replace(updatedSpan, edit.Edit.Text);
            }

            issueViz.InvalidateSpan();
            textEdit.Apply();
        }
    }
}
