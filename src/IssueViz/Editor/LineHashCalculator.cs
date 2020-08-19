/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.IssueVisualization.Editor
{
    public interface ILineHashCalculator
    {
        string Calculate(string wholeText, int line);
    }

    [Export(typeof(ILineHashCalculator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class LineHashCalculator : ILineHashCalculator
    {
        private readonly IChecksumCalculator checksumCalculator;

        [ImportingConstructor]
        public LineHashCalculator()
            : this(new ChecksumCalculator())
        {
        }

        internal LineHashCalculator(IChecksumCalculator checksumCalculator)
        {
            this.checksumCalculator = checksumCalculator;
        }

        public string Calculate(string wholeText, int line)
        {
            if (string.IsNullOrEmpty(wholeText) || line < 1)
            {
                return null;
            }

            var lines = GetLines(wholeText);

            if (line > lines.Length)
            {
                return null;
            }

            var lineToHash = lines[line - 1];

            var hash = checksumCalculator.Calculate(lineToHash);

            return hash;
        }

        private static string[] GetLines(string text) => text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
    }
}
