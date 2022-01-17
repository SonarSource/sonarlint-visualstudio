/*
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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;

namespace SonarLint.VisualStudio.IssueVisualization.Editor
{
    public interface ITaggableBufferIndicator
    {
        bool IsTaggable(ITextBuffer buffer);
    }

    [Export(typeof(ITaggableBufferIndicator))]
    internal class TaggableBufferIndicator : ITaggableBufferIndicator
    {
        public bool IsTaggable(ITextBuffer buffer)
        {
            var filePath = buffer.GetFilePath();

            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            var isOutputBuffer = buffer.ContentType.IsOfType("Output");

            if (isOutputBuffer)
            {
                return false;
            }

            return true;
        }
    }
}
