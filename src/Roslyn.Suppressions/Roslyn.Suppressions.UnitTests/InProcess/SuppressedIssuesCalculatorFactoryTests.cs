/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarLint.VisualStudio.TestInfrastructure;
using static SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.TestHelper;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.InProcess;

[TestClass]
public class SuppressedIssuesCalculatorFactoryTests
{
    private IRoslynSettingsFileStorage roslynSettingsFileStorage;
    private ILogger logger;
    private SuppressedIssuesCalculatorFactory testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        roslynSettingsFileStorage = Substitute.For<IRoslynSettingsFileStorage>();
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);

        testSubject = new SuppressedIssuesCalculatorFactory(logger, roslynSettingsFileStorage);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SuppressedIssuesCalculatorFactory, ISuppressedIssuesCalculatorFactory>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IRoslynSettingsFileStorage>());

    [TestMethod]
    public void MefCtor_CheckTypeIsNonShared() => MefTestHelpers.CheckIsNonSharedMefComponent<SuppressedIssuesCalculatorFactory>();

    [TestMethod]
    public void Ctor_SetsLogContext() => logger.Received(1).ForContext(SuppressedIssuesCalculatorFactory.SuppressedIssuesCalculatorLogContext);

    [TestMethod]
    public void CreateAllSuppressedIssuesCalculator_CreatesAllSuppressedIssuesCalculator()
    {
        var sonarQubeIssues = new[] { CreateSonarQubeIssue("csharpsquid:S111") };

        var calculator = testSubject.CreateAllSuppressedIssuesCalculator(sonarQubeIssues);

        calculator.Should().BeOfType<AllSuppressedIssuesCalculator>();
    }

    [TestMethod]
    public void CreateNewSuppressedIssuesCalculator_CreatesNewSuppressedIssuesCalculator()
    {
        var sonarQubeIssues = new[] { CreateSonarQubeIssue("csharpsquid:S111") };

        var calculator = testSubject.CreateNewSuppressedIssuesCalculator(sonarQubeIssues);

        calculator.Should().BeOfType<NewSuppressedIssuesCalculator>();
    }

    [TestMethod]
    public void CreateSuppressedIssuesRemovedCalculator_CreatesSuppressedIssuesRemovedCalculator()
    {
        var issueServerKeys = new[] { Guid.NewGuid().ToString() };

        var calculator = testSubject.CreateSuppressedIssuesRemovedCalculator(issueServerKeys);

        calculator.Should().BeOfType<SuppressedIssuesRemovedCalculator>();
    }
}
