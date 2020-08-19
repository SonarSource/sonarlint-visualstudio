﻿/*
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.BufferTagger
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(IssueLocationTag))]
    internal class IssueLocationTaggerProvider : ITaggerProvider
    {
        private readonly IAnalysisIssueSelectionService issueSelectionService;

        [ImportingConstructor]
        public IssueLocationTaggerProvider(IAnalysisIssueSelectionService issueSelectionService)
        {
            this.issueSelectionService = issueSelectionService;
        }

        ITagger<T> ITaggerProvider.CreateTagger<T>(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            // Tip when debugging/developing: a buffer tagger won't be created until there is something that can consume the tags. In our case,
            // it means until one of our view tagger providers is created, because they create a tag aggregator that consumes the those
            // buffer tags.
            return buffer.Properties.GetOrCreateSingletonProperty(() => new IssueLocationTagger(buffer, issueSelectionService)) as ITagger<T>;
        }
    }
}
