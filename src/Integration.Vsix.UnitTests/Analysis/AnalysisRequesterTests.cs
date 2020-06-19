/*
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis.UnitTests
{
    [TestClass]
    public class AnalysisRequesterTests
    {
        [TestMethod]
        public void NoListeners_NoError()
        {
            var logger = new TestLogger();
            var testSubject = new AnalysisRequester(logger);

            testSubject.RequestAnalysis();

            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void OneListener_EventIsRaised()
        {
            var logger = new TestLogger();
            var testSubject = new AnalysisRequester(logger);

            bool eventRaised = false;
            object actualSender = null;
            AnalysisRequestEventArgs actualEventArgs = null;

            testSubject.AnalysisRequested += (s, args) =>
            {
                // Don't assert here - one a background thread,
                // and the exception will be caught and suppressed.

                actualSender = s;
                actualEventArgs = args;
                eventRaised = true;
            };


            var inputOptions = new Mock<IAnalyzerOptions>().Object;

            // Act
            testSubject.RequestAnalysis(inputOptions, "file1", "c:\\aaa\\bbb.cs");

            // Assert
            eventRaised.Should().BeTrue();
            actualSender.Should().Be(testSubject);
            actualEventArgs.Should().NotBeNull();
            actualEventArgs.Options.Should().BeSameAs(inputOptions);
            actualEventArgs.FilePaths.Should().BeEquivalentTo("file1", "c:\\aaa\\bbb.cs");
        }

        [TestMethod]
        public void NonCriticalErrorInListener_IsSuppressedAndLogged()
        {
            var logger = new TestLogger();
            var testSubject = new AnalysisRequester(logger);

            testSubject.AnalysisRequested += (s, e) => throw new ArgumentException("XXX yyy");

            // Act
            testSubject.RequestAnalysis();

            // Assert
            logger.AssertPartialOutputStringExists("XXX yyy");
        }

        [TestMethod]
        public void CriticalErrorInListener_IsNotSuppressedORLogged()
        {
            var logger = new TestLogger();
            var testSubject = new AnalysisRequester(logger);

            testSubject.AnalysisRequested += (s, e) => throw new StackOverflowException("XXX overflow");

            Action act = () => testSubject.RequestAnalysis();

            // Act
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("XXX overflow");
            logger.AssertNoOutputMessages();
        }
    }
}
