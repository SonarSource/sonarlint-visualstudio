/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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
using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests
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

            testSubject.AnalysisRequested += (s, e) =>
            {
                s.Should().Be(testSubject);
                e.Should().Be(EventArgs.Empty);

                eventRaised = true;
            };

            // Act
            testSubject.RequestAnalysis();

            // Assert
            eventRaised.Should().BeTrue();
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
