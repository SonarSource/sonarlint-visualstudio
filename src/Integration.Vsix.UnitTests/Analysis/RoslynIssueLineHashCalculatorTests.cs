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
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Infrastructure.VS.Editor;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis;

[TestClass]
public class RoslynIssueLineHashCalculatorTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<RoslynIssueLineHashCalculator, IRoslynIssueLineHashCalculator>(
            MefTestHelpers.CreateExport<ITextDocumentFactoryService>(),
            MefTestHelpers.CreateExport<IContentTypeRegistryService>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<RoslynIssueLineHashCalculator>();
    }
    
    [TestMethod]
    public void UpdateRoslynIssueWithLineHash_FileLevelIssue_DoesNothing()
    {
        var issue = new Mock<IFilterableRoslynIssue>(MockBehavior.Strict);
        issue.SetupGet(x => x.StartLine).Returns((int?)null);
        var testSubject = CreateTestSubject(out _, out _, out _);
        
        testSubject.UpdateRoslynIssueWithLineHash(issue.Object);
        
        issue.Verify(x => x.SetLineHash(It.IsAny<string>()), Times.Never);
    }
    
    [TestMethod]
    public void UpdateRoslynIssueWithLineHash_UpdatesLineHash()
    {
        const int lineNumber = 111;
        const string filePath = "filepath";
        const string lineHash = "linehash";

        var issue = new Mock<IFilterableRoslynIssue>(MockBehavior.Strict);
        issue.SetupGet(x => x.FilePath).Returns(filePath);
        issue.SetupGet(x => x.StartLine).Returns(lineNumber);

        var testSubject = CreateTestSubject(out var textDocumentFactoryServiceMock, out var contentTypeRegistryServiceMock, out var lineHashCalculatorMock);
        var contentType = Mock.Of<IContentType>();
        var document = new Mock<ITextDocument>();
        var buffer = new Mock<ITextBuffer>();
        var snapshot = new Mock<ITextSnapshot>();
        contentTypeRegistryServiceMock.SetupGet(x => x.UnknownContentType).Returns(contentType);
        textDocumentFactoryServiceMock.Setup(x => x.CreateAndLoadTextDocument(filePath, contentType)).Returns(document.Object);
        document.SetupGet(x => x.TextBuffer).Returns(buffer.Object);
        buffer.SetupGet(x => x.CurrentSnapshot).Returns(snapshot.Object);
        lineHashCalculatorMock.Setup(x => x.Calculate(snapshot.Object, lineNumber)).Returns(lineHash);
        issue.Setup(x => x.SetLineHash(lineHash));
        
        testSubject.UpdateRoslynIssueWithLineHash(issue.Object);
        
        issue.Verify(x => x.SetLineHash(lineHash), Times.Once);
    }
    
    private RoslynIssueLineHashCalculator CreateTestSubject(out Mock<ITextDocumentFactoryService> textDocumentFactoryServiceMock, 
        out Mock<IContentTypeRegistryService> contentTypeRegistryServiceMock,
        out Mock<ILineHashCalculator> lineHashCalculatorMock)
    {
        return new RoslynIssueLineHashCalculator(
            (textDocumentFactoryServiceMock = new Mock<ITextDocumentFactoryService>(MockBehavior.Strict)).Object,
            (contentTypeRegistryServiceMock = new Mock<IContentTypeRegistryService>(MockBehavior.Strict)).Object,
            (lineHashCalculatorMock = new Mock<ILineHashCalculator>(MockBehavior.Strict)).Object);
    }
}
