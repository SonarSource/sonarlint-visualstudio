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

using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Core.UnitTests.Binding;

[TestClass]
public class SolutionStateTests
{
    private static readonly string NoSolution = null;
    private const string Solution1 = "SolutionOne";
    private const string Solution2 = "SolutionTwo";
    private static BindingConfiguration BoundConfig1 = new(new BoundServerProject("LocalKey1", "ServerKey1", new ServerConnection.SonarCloud("OrgKey1")), SonarLintMode.Connected, "any");
    private static BindingConfiguration BoundConfig2
        = new(new BoundServerProject("LocalKey1", "ServerKey2", new ServerConnection.SonarCloud("OrgKey1")), SonarLintMode.Connected, "any");
    private static BindingConfiguration BoundConfig3 = new(new BoundServerProject("LocalKey2", "ServerKey2", new ServerConnection.SonarQube(new Uri("http://myhost"))), SonarLintMode.Connected, "any");
    private static BindingConfiguration BoundConfig3WithDifferentConnectionSettings
        = new(new BoundServerProject("LocalKey2", "ServerKey2", new ServerConnection.SonarQube(new Uri("http://myhost"), new ServerConnectionSettings(false))), SonarLintMode.Connected, "any");

    public static object[][] SolutionNameAndBindingConfiguration_PropertiesAreSet_Data =>
    [
        [NoSolution, BindingConfiguration.Standalone],
        [Solution1, BindingConfiguration.Standalone],
        [Solution2, BindingConfiguration.Standalone],
        [Solution1, BoundConfig1],
        [Solution1, BoundConfig2],
    ];
    [DynamicData(nameof(SolutionNameAndBindingConfiguration_PropertiesAreSet_Data))]
    [DataTestMethod]
    public void SolutionNameAndBindingConfiguration_PropertiesAreSet(string solutionName, BindingConfiguration bindingConfiguration)
    {
        var testSubject = new SolutionState(solutionName, bindingConfiguration);

        testSubject.SolutionName.Should().Be(solutionName);
        testSubject.BindingConfiguration.Should().Be(bindingConfiguration);
    }

    public static object[][] IsOpen_ReturnsCorrectValue_Data =>
    [
        [NoSolution, false],
        [Solution1, true],
        [Solution2, true],
    ];
    [DynamicData(nameof(IsOpen_ReturnsCorrectValue_Data))]
    [DataTestMethod]
    public void IsOpen_ReturnsCorrectValue(string solutionName, bool isOpen)
    {
        var testSubject = new SolutionState(solutionName, BindingConfiguration.Standalone);

        testSubject.IsOpen.Should().Be(isOpen);
    }

    public static object[][] IsInConnectedMode_ReturnsCorrectValue_Data =>
    [
        [BindingConfiguration.Standalone, false],
        [new BindingConfiguration(null, SonarLintMode.Standalone, null), false],
        [BoundConfig1, true],
        [BoundConfig2, true],
    ];
    [DynamicData(nameof(IsInConnectedMode_ReturnsCorrectValue_Data))]
    [DataTestMethod]
    public void IsInConnectedMode_ReturnsCorrectValue(BindingConfiguration bindingConfiguration, bool isInConnectedMode)
    {
        var testSubject = new SolutionState(Solution1, bindingConfiguration);

        testSubject.IsInConnectedMode.Should().Be(isInConnectedMode);
    }

    [TestMethod]
    public void Ctor_NullBindingConfiguration_Throws()
    {
        var act = () => new SolutionState(Solution1, null);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("bindingConfiguration");
    }

    [TestMethod]
    public void Equals_Combinations()
    {
        var solution1Standalone = new SolutionState(Solution1, BindingConfiguration.Standalone);
        solution1Standalone.Equals(null).Should().BeFalse();
        solution1Standalone.Equals(new SolutionState(Solution1, BindingConfiguration.Standalone)).Should().BeTrue();
        solution1Standalone.Equals(solution1Standalone).Should().BeTrue();

        var solution2Standalone = new SolutionState(Solution2, BindingConfiguration.Standalone);
        solution1Standalone.Equals(solution2Standalone).Should().BeFalse();
        solution2Standalone.Equals(solution1Standalone).Should().BeFalse();

        var solution1Binding1 = new SolutionState(Solution1, BoundConfig1);
        solution1Standalone.Equals(solution1Binding1).Should().BeFalse();
        solution1Binding1.Equals(solution1Standalone).Should().BeFalse();
        solution1Binding1.Equals(new SolutionState(Solution1, BoundConfig1)).Should().BeTrue();
        solution1Binding1.Equals(solution1Binding1).Should().BeTrue();

        var solution1Binding2 = new SolutionState(Solution1, BoundConfig2);
        solution1Binding1.Equals(solution1Binding2).Should().BeFalse();
        solution1Binding2.Equals(solution1Binding1).Should().BeFalse();

        var solution2Binding1 = new SolutionState(Solution2, BoundConfig2);
        solution1Binding1.Equals(solution2Binding1).Should().BeFalse();
        solution2Binding1.Equals(solution1Binding1).Should().BeFalse();

        var solution2Binding3 = new SolutionState(Solution2, BoundConfig3);
        var solution2Binding3Modified = new SolutionState(Solution2, BoundConfig3WithDifferentConnectionSettings);
        solution2Binding3.Equals(solution2Binding3Modified).Should().BeTrue();
        solution2Binding3Modified.Equals(solution2Binding3).Should().BeTrue();
    }
}
