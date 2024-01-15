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

namespace SonarLint.VisualStudio.Core.Analysis
{
    public class QuickFix : IQuickFix
    {
        public QuickFix(string message, IReadOnlyList<IEdit> edits)
        {
            if (edits == null || edits.Count == 0)
            {
                throw new ArgumentException("A fix should have at least one edit.", nameof(edits));
            }
            Message = message;
            Edits = edits;
        }

        public string Message { get; }

        public IReadOnlyList<IEdit> Edits { get; }
    }

    public class Edit : IEdit
    {
        public Edit(string text, ITextRange textRange)
        {
            NewText = text;
            RangeToReplace = textRange;
        }

        public string NewText { get; }
        public ITextRange RangeToReplace { get; }
    }
}
