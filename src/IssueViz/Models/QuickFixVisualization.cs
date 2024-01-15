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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.IssueVisualization.Models
{
    public interface IQuickFixVisualization
    {
        IQuickFix Fix { get; }

        IReadOnlyList<IQuickFixEditVisualization> EditVisualizations { get; }

        /// <summary>
        /// Returns false if the snapshot has been edited so the fix can no longer be applied, otherwise true.
        /// </summary>
        bool CanBeApplied(ITextSnapshot currentSnapshot);
    }

    internal class QuickFixVisualization : IQuickFixVisualization
    {
        private readonly ISpanTranslator spanTranslator;

        public QuickFixVisualization(IQuickFix fix, IReadOnlyList<IQuickFixEditVisualization> editVisualizations)
            : this(fix, editVisualizations, new SpanTranslator())
        {
            Fix = fix;
            EditVisualizations = editVisualizations;
        }

        internal QuickFixVisualization(IQuickFix fix,
            IReadOnlyList<IQuickFixEditVisualization> editVisualizations,
            ISpanTranslator spanTranslator)
        {
            this.spanTranslator = spanTranslator;
            Fix = fix;
            EditVisualizations = editVisualizations;
        }

        public IQuickFix Fix { get; }

        public IReadOnlyList<IQuickFixEditVisualization> EditVisualizations { get; }

        public bool CanBeApplied(ITextSnapshot currentSnapshot) =>
            EditVisualizations.All(x => IsTextUnchanged(x, currentSnapshot));

        private bool IsTextUnchanged(IQuickFixEditVisualization editViz, ITextSnapshot currentSnapshot)
        {
            var originalText = editViz.Span.GetText();
            var updatedSpan = spanTranslator.TranslateTo(editViz.Span, currentSnapshot, SpanTrackingMode.EdgeExclusive);
            var currentText = currentSnapshot.GetText(updatedSpan);

            return string.Equals(currentText, originalText, StringComparison.InvariantCulture);
        }
    }

    public interface IQuickFixEditVisualization
    {
        IEdit Edit { get; }

        SnapshotSpan Span { get; }
    }

    internal class QuickFixEditVisualization : IQuickFixEditVisualization
    {
        public QuickFixEditVisualization(IEdit edit, SnapshotSpan span)
        {
            Edit = edit;
            Span = span;
        }

        public IEdit Edit { get; }

        public SnapshotSpan Span { get; }
    }
}
