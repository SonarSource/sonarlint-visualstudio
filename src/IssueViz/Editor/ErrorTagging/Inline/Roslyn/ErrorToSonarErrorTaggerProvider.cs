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

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging.Inline.Roslyn
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(ISonarErrorTag))]
    internal class ErrorToSonarErrorTaggerProvider : ITaggerProvider
    {
        private readonly ISonarAndRoslynErrorsProvider sonarAndRoslynErrorsProvider;
        private readonly ITaggableBufferIndicator taggableBufferIndicator;
        private ILogger logger;

        [ImportingConstructor]
        public ErrorToSonarErrorTaggerProvider(ISonarAndRoslynErrorsProvider sonarAndRoslynErrorsProvider,
            ITaggableBufferIndicator taggableBufferIndicator, 
            ILogger logger)
        {
            this.sonarAndRoslynErrorsProvider = sonarAndRoslynErrorsProvider;
            this.taggableBufferIndicator = taggableBufferIndicator;
            this.logger = logger;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (!taggableBufferIndicator.IsTaggable(buffer))
            {
                return null;
            }

            var tagger = Create(buffer);
            return tagger as ITagger<T>;
        }

        private ErrorToSonarErrorTagger Create(ITextBuffer textBuffer)
        {
            return new ErrorToSonarErrorTagger(textBuffer, sonarAndRoslynErrorsProvider, logger);
        }
    }
}
