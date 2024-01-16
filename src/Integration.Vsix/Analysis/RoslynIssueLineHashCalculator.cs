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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Infrastructure.VS.Editor;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    internal interface IRoslynIssueLineHashCalculator
    {
        /// <summary>
        /// Calculates line hash based on <see cref="IFilterableRoslynIssue.StartLine"/> , if it's not a file level issue, and <see cref="IFilterableRoslynIssue.FilePath"/>
        /// </summary>
        void UpdateRoslynIssueWithLineHash(IFilterableRoslynIssue filterableRoslynIssue);
    }

    [Export(typeof(IRoslynIssueLineHashCalculator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class RoslynIssueLineHashCalculator : IRoslynIssueLineHashCalculator
    {
        private readonly ITextDocumentFactoryService textDocumentFactoryService;
        private readonly IContentTypeRegistryService contentTypeRegistryService;
        private readonly ILineHashCalculator lineHashCalculator;

        [ImportingConstructor]
        public RoslynIssueLineHashCalculator(ITextDocumentFactoryService textDocumentFactoryService, IContentTypeRegistryService contentTypeRegistryService)
            : this(textDocumentFactoryService, contentTypeRegistryService, new LineHashCalculator())
        {
        }

        internal RoslynIssueLineHashCalculator(ITextDocumentFactoryService textDocumentFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            ILineHashCalculator lineHashCalculator)
        {
            this.textDocumentFactoryService = textDocumentFactoryService;
            this.contentTypeRegistryService = contentTypeRegistryService;
            this.lineHashCalculator = lineHashCalculator;
        }

        public void UpdateRoslynIssueWithLineHash(IFilterableRoslynIssue filterableRoslynIssue)
        {
            if (!filterableRoslynIssue.StartLine.HasValue)
            {
                return; 
            }

            var document = textDocumentFactoryService.CreateAndLoadTextDocument(filterableRoslynIssue.FilePath, contentTypeRegistryService.UnknownContentType);
            filterableRoslynIssue.SetLineHash(lineHashCalculator.Calculate(document.TextBuffer.CurrentSnapshot, filterableRoslynIssue.StartLine.Value));
        }
    }
}
