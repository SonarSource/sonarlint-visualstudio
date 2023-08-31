/*
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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl
{
    [TestClass]
    public class SeverityToMonikerConverterTests
    {
        [TestMethod]
        public void ConvertBack_NotImplementedException()
        {
            Action act = () => new SeverityToMonikerConverter().ConvertBack(null, null, null, null);

            act.Should().Throw<NotImplementedException>("ConvertBack is not required");
        }

        [TestMethod]
        public void Convert_CurrentIssueNull_InformationMoniker()
        {
            var issueVisMock = new Mock<IAnalysisIssueVisualization>();
            issueVisMock.SetupGet(x => x.Issue).Returns((IAnalysisIssue)null);
            
            var result = (ImageMoniker)new SeverityToMonikerConverter(Mock.Of<IAnalysisSeverityToVsSeverityConverter>()).Convert(issueVisMock.Object, null, null, null);

            result.Should().Be(KnownMonikers.StatusInformation);
        }
        
        [TestMethod]
        public void Convert_CurrentNotIAnalysisIssue_InformationMoniker()
        {
            var issueVisMock = new Mock<IAnalysisIssueVisualization>();
            issueVisMock.SetupGet(x => x.Issue).Returns(Mock.Of<IAnalysisIssueBase>());
            
            var result = (ImageMoniker)new SeverityToMonikerConverter(Mock.Of<IAnalysisSeverityToVsSeverityConverter>()).Convert(issueVisMock.Object, null, null, null);

            result.Should().Be(KnownMonikers.StatusInformation);
        }

        [TestMethod]
        public void Convert_NullParameter_InformationMoniker()
        {

            var result = (ImageMoniker)new SeverityToMonikerConverter(Mock.Of<IAnalysisSeverityToVsSeverityConverter>()).Convert(null, null, null, null);

            result.Should().Be(KnownMonikers.StatusInformation);
        }

        [TestMethod]
        public void Convert_VsSeverityIsError_ErrorMoniker()
        {
            VerifyConversionWhenIssueIsIAnalysisIssue(__VSERRORCATEGORY.EC_ERROR, KnownMonikers.StatusError);
        }

        [TestMethod]
        public void Convert_VsSeverityIsMessage_InformationMoniker()
        {
            VerifyConversionWhenIssueIsIAnalysisIssue(__VSERRORCATEGORY.EC_MESSAGE, KnownMonikers.StatusInformation);   
        }

        [TestMethod]
        public void Convert_VsSeverityIsWarning_WarningMoniker()
        {
            VerifyConversionWhenIssueIsIAnalysisIssue(__VSERRORCATEGORY.EC_WARNING, KnownMonikers.StatusWarning);
        }

        private void VerifyConversionWhenIssueIsIAnalysisIssue(__VSERRORCATEGORY vsSeverity, object expectedMoniker)
        {
            var toVsSeverityConverterMock = new Mock<IAnalysisSeverityToVsSeverityConverter>();
            toVsSeverityConverterMock
                .Setup(x => x.Convert(AnalysisIssueSeverity.Info))
                .Returns(vsSeverity);

            var issueVisMock = new Mock<IAnalysisIssueVisualization>();
            issueVisMock.SetupGet(x => x.Issue).Returns(new DummyAnalysisIssue { Severity = AnalysisIssueSeverity.Info });

            var result = (ImageMoniker)new SeverityToMonikerConverter(toVsSeverityConverterMock.Object).Convert(issueVisMock.Object, null, null, null);
            var expected = (ImageMoniker) expectedMoniker;

            result.Should().Be(expected);
        }
    }
}
