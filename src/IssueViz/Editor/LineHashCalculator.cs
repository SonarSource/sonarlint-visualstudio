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
        /// <summary>
        /// Returns a hash of the given line number inside the documentText
        /// </summary>
        /// <param name="documentText">The text from which to extract the line</param>
        /// <param name="oneBasedLineNumber">1-based line to hash</param>
        /// <returns>hash of line</returns>
        string Calculate(string documentText, int oneBasedLineNumber);
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

        public string Calculate(string documentText, int oneBasedLineNumber)
        {
            if (string.IsNullOrEmpty(documentText) || oneBasedLineNumber < 1)
            {
                return null;
            }

            var lines = GetLines(documentText);

            if (oneBasedLineNumber > lines.Length)
            {
                return null;
            }

            var lineToHash = lines[oneBasedLineNumber - 1];

            var hash = checksumCalculator.Calculate(lineToHash);

            return hash;
        }

        private static string[] GetLines(string text) => text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
    }
}
