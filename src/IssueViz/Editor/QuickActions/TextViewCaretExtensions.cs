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

using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions
{
    internal static class TextViewCaretExtensions
    {
        /// <summary>
        /// An open light bulb session tracks a span anchored to the caret, but ILightBulbBroker.DismissSession
        /// closes the whole view's session unconditionally. Callers should use this to check whether a reported
        /// tag change is actually near the caret before dismissing - otherwise any issue changing anywhere in
        /// the file, however far from the caret, would dismiss a session the user is actively interacting with.
        /// </summary>
        public static bool IsChangeNearCaret(this ITextView textView, IMappingSpan changedSpan)
        {
            var caretSpan = new SnapshotSpan(textView.Caret.Position.BufferPosition, 0);
            var mappedSpans = changedSpan.GetSpans(textView.TextSnapshot);

            return mappedSpans.Any(x => x.IntersectsWith(caretSpan));
        }
    }
}
