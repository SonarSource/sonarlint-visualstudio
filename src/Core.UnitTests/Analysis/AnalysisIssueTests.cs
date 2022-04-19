using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Core.UnitTests.Analysis
{
    [TestClass]
    public class AnalysisIssueTests
    {
        [TestMethod]
        public void IsFileLevel_PrimaryLocationHasTextRange_True()
        {
            var analysisIssue = CreateTestSubject(true);

            analysisIssue.IsFileLevel().Should().BeFalse();
        }

        [TestMethod]
        public void IsFileLevel_PrimaryLocationHasNoTextRange_False()
        {
            var analysisIssue = CreateTestSubject(false);

            analysisIssue.IsFileLevel().Should().BeTrue();
        }

        private IAnalysisIssue CreateTestSubject(bool primaryLocationHasTextRange)
        {
            var analysisIssue = new Mock<IAnalysisIssue>();
            var primaryLocation = new Mock<IAnalysisIssueLocation>();

            if(primaryLocationHasTextRange)
            {
                primaryLocation.SetupGet(p => p.TextRange).Returns(new DummyTextRange());
            }
            analysisIssue.SetupGet(a => a.PrimaryLocation).Returns(primaryLocation.Object);
            return analysisIssue.Object;
        }
    }
}
