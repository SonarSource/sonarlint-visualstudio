/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.ErrorTagging
{
    [TestClass]
    public class ErrorTaggerProviderTests : CommonTaggerProviderTestsBase
    {
        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<ErrorTaggerProvider, ITaggerProvider>(
                MefTestHelpers.CreateExport<ITaggableBufferIndicator>(),
                MefTestHelpers.CreateExport<IBufferTagAggregatorFactoryService>(),
                MefTestHelpers.CreateExport<IErrorTagTooltipProvider>(),
                MefTestHelpers.CreateExport<IFocusOnNewCodeService>());

        internal override ITaggerProvider CreateTestSubject(ITaggableBufferIndicator taggableBufferIndicator)
        {
            var bufferTagAggregatorFactoryService = Substitute.For<IBufferTagAggregatorFactoryService>();
            bufferTagAggregatorFactoryService.CreateTagAggregator<IIssueLocationTag>(Arg.Any<ITextBuffer>())
                .Returns(Substitute.For<ITagAggregator<IIssueLocationTag>>());

            return new ErrorTaggerProvider(bufferTagAggregatorFactoryService, taggableBufferIndicator, Substitute.For<IErrorTagTooltipProvider>(), Substitute.For<IFocusOnNewCodeService>());
        }
    }
}
