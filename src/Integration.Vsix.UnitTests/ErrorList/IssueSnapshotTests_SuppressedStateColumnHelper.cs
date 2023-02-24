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

using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.ErrorList
{
    [TestClass]
    public class IssueSnapshotTests_SuppressedStateColumnHelper
    {

#if VS2022

        [TestMethod]
        public void GetValue_VS2022_IsSuppressed_ReturnsSuppressed()
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.IsSuppressed).Returns(true);

            IssuesSnapshot.SuppressedStateColumnHelper.GetValue(issueViz.Object)
                .Should().Be(SuppressionState.Suppressed);
        }

        [TestMethod]
        public void GetValue_VS2022_IsNotSuppressed_ReturnsActive()
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.IsSuppressed).Returns(false);

            IssuesSnapshot.SuppressedStateColumnHelper.GetValue(issueViz.Object)
                .Should().Be(SuppressionState.Active);
        }

#else

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetValue_VS2019BuildTime_NoExceptionAndReturnsNull(bool isSuppressed)
        {
            // NOTE: this test is checking that there are no exceptions if the enum
            // is not available.

            // The enum won't be available at SLVS build-time for VS2019 because it is
            // not in the SDK version we are referencing.

            // The enum should be available at runtime in VS2019.3 or later.

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.IsSuppressed).Returns(isSuppressed);

            IssuesSnapshot.SuppressedStateColumnHelper.GetValue(issueViz.Object)
                .Should().BeNull();
        }
#endif

    }
}
