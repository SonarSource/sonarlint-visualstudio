﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

namespace SonarLint.VisualStudio.Core.Analysis
{
    public interface IQuickFix
    {
        string Message { get; }
        IReadOnlyList<IEdit> Edits { get; }
    }

    public interface IEdit
    {
        /// <summary>
        /// The new text to insert. Can be empty if the edit is a deletion.
        /// </summary>
        string NewText { get; }

        /// <summary>
        /// The range of existing text to be replaced.
        /// The range can have a zero-length if no existing text is being removed i.e. the range will indicate the insertion point.
        /// </summary>
        ITextRange RangeToReplace { get; }
    }
}
