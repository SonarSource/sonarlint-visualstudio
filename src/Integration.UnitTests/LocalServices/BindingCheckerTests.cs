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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class BindingCheckerTests
    {
        [TestMethod]
        public async Task IsBindingUpdateRequired_SolutionIsUnbound_True()
        {
            var unboundSolutionChecker = CreateUnboundSolutionChecker(isSolutionBound: false);
            var testSubject = CreateTestSubject(unboundSolutionChecker.Object);

            var result = await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            result.Should().BeTrue();
            unboundSolutionChecker.VerifyAll();
        }

        [TestMethod]
        public async Task IsBindingUpdateRequired_SolutionIsBound_False()
        {
            var testSubject = CreateTestSubject();

            var result = await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task IsBindingUpdateRequired_SolutionIsBound_NoLogs()
        {
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(logger);

            await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            logger.OutputStrings.Should().BeEmpty();
        }

        private Mock<IUnboundSolutionChecker> CreateUnboundSolutionChecker(bool isSolutionBound)
        {
            var unboundSolutionChecker = new Mock<IUnboundSolutionChecker>();
            unboundSolutionChecker.Setup(x => x.IsBindingUpdateRequired(CancellationToken.None)).ReturnsAsync(!isSolutionBound);

            return unboundSolutionChecker;
        }

        private BindingChecker CreateTestSubject(IUnboundSolutionChecker unboundSolutionChecker, ILogger logger = null)
        {
            logger ??= new TestLogger();

            return new BindingChecker(unboundSolutionChecker, logger);
        }

        private BindingChecker CreateTestSubject(ILogger logger = null)
        {
            var unboundSolutionChecker = CreateUnboundSolutionChecker(isSolutionBound:true);

            return CreateTestSubject(unboundSolutionChecker.Object, logger);
        }
    }
}
