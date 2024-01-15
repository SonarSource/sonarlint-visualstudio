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

using Microsoft.VisualStudio.Text;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Infrastructure.VS.Editor
{
    public interface ILineHashCalculator
    {
        /// <summary>
        /// Returns a hash of the given line number inside the documentText
        /// </summary>
        /// <param name="textSnapshot">The ITextSnapshot from which to extract the line</param>
        /// <param name="oneBasedLineNumber">1-based line to hash</param>
        /// <returns>hash of line</returns>
        string Calculate(ITextSnapshot textSnapshot, int oneBasedLineNumber);
    }

    public class LineHashCalculator : ILineHashCalculator
    {
        private readonly IChecksumCalculator checksumCalculator;

        public LineHashCalculator()
            : this(new ChecksumCalculator())
        {
        }

        internal LineHashCalculator(IChecksumCalculator checksumCalculator)
        {
            this.checksumCalculator = checksumCalculator;
        }

        public string Calculate(ITextSnapshot textSnapshot, int oneBasedLineNumber)
        {
            if (oneBasedLineNumber < 1 || textSnapshot == null)
            {
                return null;
            }

            // SonarLint issues line numbers are 1-based, span lines are 0-based
            // We are using ITextSnapshot.GetLineFromLineNumber, as that method is aware of the different types of line break.
            var lineToHash = textSnapshot.GetLineFromLineNumber(oneBasedLineNumber - 1);
            var lineText = lineToHash?.GetText();

            if (lineText == null)
            {
                return null;
            }

            var hash = checksumCalculator.Calculate(lineText);

            return hash;
        }
    }
}
