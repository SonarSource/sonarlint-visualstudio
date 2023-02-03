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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Commands;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.UnitTests;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Education.UnitTests.Commands
{
    [TestClass]
    public class ShowHelpFromErrorListTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var serviceProvider = CreateServiceProvider();

                MefTestHelpers.CheckTypeCanBeImported<ShowHelpFromErrorList, IShowHelpFromErrorList>(
                    MefTestHelpers.CreateExport<SVsServiceProvider>(serviceProvider),
                    MefTestHelpers.CreateExport<IEducation>(),
                    MefTestHelpers.CreateExport<IErrorListHelper>(),
                    MefTestHelpers.CreateExport<IThreadHandling>(new NoOpThreadHandler()));
        }

        [TestMethod]
        public void Ctor_InitializeInterceptionIsCalled()
        {
            var priority = new Mock<IVsRegisterPriorityCommandTarget>();
            var serviceProvider = new Mock<IServiceProvider>();

            uint cookie = 5;
            priority.Setup(x => x.RegisterPriorityCommandTarget(0, It.IsAny<CommandInterceptor>(), out cookie));

            serviceProvider.Setup(x => x.GetService(typeof(SVsRegisterPriorityCommandTarget))).Returns(priority.Object);

            _ = CreateTestSubject(serviceProvider: serviceProvider.Object);

            serviceProvider.Verify(x => x.GetService(typeof(SVsRegisterPriorityCommandTarget)), Times.Once);
            priority.Verify(x => x.RegisterPriorityCommandTarget(0, It.IsAny<CommandInterceptor>(), out cookie), Times.Once);
        }

        [TestMethod]
        [DataRow("csharpsquid:S122", "csharpsquid", "S122")]
        [DataRow("vbnet:S123", "vbnet", "S123")]
        [DataRow("cpp:S111", "cpp", "S111")]
        [DataRow("c:S222", "c", "S222")]
        [DataRow("javascript:S333", "javascript", "S333")]
        [DataRow("typescript:S444", "typescript", "S444")]
        public void HandleInterception_RuleIsSonar_ReturnsStop(string errorCode, string repoKey, string expectedRule)
        {
            var education = new Mock<IEducation>();
            var errorListHelper = CreateErrorListHelper(errorCode, ruleIsSonar: true);

            var expectedLanguage = Language.GetLanguageFromRepositoryKey(repoKey);

            var testSubject = CreateTestSubject(education: education.Object, errorListHelper: errorListHelper);
            var result = testSubject.HandleInterception();

            education.Verify(x => x.ShowRuleDescription(expectedLanguage, expectedRule), Times.Once);
            result.Should().Be(CommandProgression.Stop);
        }

        [TestMethod]
        public void HandleInterception_RuleIsNotSonar_ReturnsContinue()
        {
            var errorListHelper = CreateErrorListHelper("unknown:S222", ruleIsSonar: false);

            var testSubject = CreateTestSubject(errorListHelper: errorListHelper);

            var result = testSubject.HandleInterception();

            result.Should().Be(CommandProgression.Continue);
        }

        private ShowHelpFromErrorList CreateTestSubject(IServiceProvider serviceProvider = null, IEducation education = null, IErrorListHelper errorListHelper = null)
        {
            serviceProvider ??= CreateServiceProvider();
            education ??= Mock.Of<IEducation>();
            errorListHelper ??= Mock.Of<IErrorListHelper>();
            var threadHandling = new NoOpThreadHandler();

            var testSubject = new ShowHelpFromErrorList(serviceProvider, education, errorListHelper, threadHandling);

            return testSubject;
        }

        private IErrorListHelper CreateErrorListHelper(string errorCode, bool ruleIsSonar)
        {
            var errorListHelper = new Mock<IErrorListHelper>();

            SonarCompositeRuleId.TryParse(errorCode, out SonarCompositeRuleId ruleId);
            errorListHelper.Setup(x => x.TryGetRuleIdFromSelectedRow(out ruleId)).Returns(ruleIsSonar);

            return errorListHelper.Object;
        }

        private IServiceProvider CreateServiceProvider()
        {
            var priority = Mock.Of<IVsRegisterPriorityCommandTarget>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsRegisterPriorityCommandTarget))).Returns(priority);

            return serviceProvider.Object;
        }
    }
}
