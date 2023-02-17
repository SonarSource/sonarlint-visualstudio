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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment;
using SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.SelectedIssueTagging.Adornment
{
    [TestClass]
    public class IssueLocationAdornmentTaggerProviderTests : CommonViewTaggerProviderTestsBase
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<IssueLocationAdornmentTaggerProvider, IViewTaggerProvider>(
                MefTestHelpers.CreateExport<ITaggableBufferIndicator>(),
                MefTestHelpers.CreateExport<IBufferTagAggregatorFactoryService>());
        }

        internal override IViewTaggerProvider CreateTestSubject(ITaggableBufferIndicator taggableBufferIndicator)
        {
            var aggregatorMock = new Mock<IBufferTagAggregatorFactoryService>();
            aggregatorMock.Setup(x => x.CreateTagAggregator<ISelectedIssueLocationTag>(It.IsAny<ITextBuffer>()))
                .Returns(Mock.Of<ITagAggregator<ISelectedIssueLocationTag>>());

            return new IssueLocationAdornmentTaggerProvider(aggregatorMock.Object, taggableBufferIndicator);
        }
    }
}
