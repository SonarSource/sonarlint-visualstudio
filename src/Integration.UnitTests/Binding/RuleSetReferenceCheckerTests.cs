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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class RuleSetReferenceCheckerTests
    {
        private Mock<ISolutionRuleSetsInformationProvider> solutionRuleSetsInformationProviderMock;
        private Mock<IRuleSetSerializer> ruleSetSerializerMock;
        private RuleSetReferenceChecker testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            solutionRuleSetsInformationProviderMock = new Mock<ISolutionRuleSetsInformationProvider>();
            ruleSetSerializerMock = new Mock<IRuleSetSerializer>();

            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(ISolutionRuleSetsInformationProvider)))
                .Returns(solutionRuleSetsInformationProviderMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IRuleSetSerializer)))
                .Returns(ruleSetSerializerMock.Object);

            testSubject = new RuleSetReferenceChecker(serviceProviderMock.Object);
        }

        [TestMethod]
        public void IsReferenced_ProjectHasNoRuleSets_False()
        {
            var solutionRuleSet = new RuleSet("name");
            var projectMock = new ProjectMock("c:\\test.csproj");

            solutionRuleSetsInformationProviderMock
                .Setup(x => x.GetProjectRuleSetsDeclarations(projectMock))
                .Returns(Array.Empty<RuleSetDeclaration>());

            var result = testSubject.IsReferenced(projectMock, solutionRuleSet);
            result.Should().BeFalse();

            ruleSetSerializerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IsReferenced_ProjectHasOneRuleset_CantLoadProjectRulesetFile_False()
        {
            var solutionRuleSet = new RuleSet("name");
            var projectMock = new ProjectMock("c:\\test.csproj");
            var ruleSetDeclaration = GetRuleSetDeclaration(projectMock);

            solutionRuleSetsInformationProviderMock
                .Setup(x => x.GetProjectRuleSetsDeclarations(projectMock))
                .Returns(new List<RuleSetDeclaration> { ruleSetDeclaration });

            var filePath = "";
            solutionRuleSetsInformationProviderMock
                .Setup(x => x.TryGetProjectRuleSetFilePath(projectMock, ruleSetDeclaration, out filePath))
                .Returns(false);

            var result = testSubject.IsReferenced(projectMock, solutionRuleSet);
            result.Should().BeFalse();

            ruleSetSerializerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsReferenced_ProjectHasOneRuleset_ReturnIfReferencesSolutionRuleset(bool referencesSolutionRuleset)
        {
            Assert.Inconclusive("TBD");
        }

        [TestMethod]
        [DataRow(true, true, true)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(false, false, false)]
        public void IsReferenced_ProjectHasTwoRulesets_ReturnIfAllReferenceSolutionRuleset(bool firstReferencesSolutionRuleset, bool secondReferencesSolutionRuleset, bool expectedResult)
        {
            Assert.Inconclusive("TBD");
        }

        private RuleSetDeclaration GetRuleSetDeclaration(ProjectMock projectMock)
        {
            var mockDeclaration =
                new RuleSetDeclaration(projectMock, new PropertyMock("name", null), "test path", null);

            return mockDeclaration;
        }
    }
}
