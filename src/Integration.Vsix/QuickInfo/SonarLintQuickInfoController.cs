/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Controls when to display a tooltip containing SonarLint Daemon issues.
    /// </summary>
    public class SonarLintQuickInfoController : IIntellisenseController
    {
        private readonly SonarLintQuickInfoControllerProvider provider;
        private readonly IList<ITextBuffer> subjectBuffers;
        private ITextView textView;

        internal SonarLintQuickInfoController(ITextView textView, IList<ITextBuffer> subjectBuffers,
            SonarLintQuickInfoControllerProvider provider)
        {
            this.textView = textView;
            this.subjectBuffers = subjectBuffers;
            this.provider = provider;

            textView.MouseHover += OnTextViewMouseHover;
        }

        private void OnTextViewMouseHover(object sender, MouseHoverEventArgs e)
        {
            // Find the mouse position by mapping down to the subject buffer
            var point = textView.BufferGraph.MapDownToFirstMatch(
                    new SnapshotPoint(textView.TextSnapshot, e.Position),
                    PointTrackingMode.Positive,
                    snapshot => subjectBuffers.Contains(snapshot.TextBuffer),
                    PositionAffinity.Predecessor);

            if (point != null)
            {
                var trackingPoint = point.Value.Snapshot
                    .CreateTrackingPoint(point.Value.Position, PointTrackingMode.Positive);

                if (!provider.QuickInfoBroker.IsQuickInfoActive(textView))
                {
                    provider.QuickInfoBroker.TriggerQuickInfo(textView, trackingPoint, true);
                }
            }
        }

        public void Detach(ITextView textView)
        {
            if (this.textView == textView)
            {
                this.textView.MouseHover -= OnTextViewMouseHover;
                this.textView = null;
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
            // Nothing to do here
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
            // Nothing to do here
        }
    }
}
