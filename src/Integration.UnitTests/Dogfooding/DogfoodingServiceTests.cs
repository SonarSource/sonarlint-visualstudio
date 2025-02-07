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
using SonarLint.VisualStudio.Integration.Dogfooding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Dogfooding;

[TestClass]
public class DogfoodingServiceTests
{
    private IEnvironmentVariableProvider environmentVariableProvider;

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<DogfoodingService, IDogfoodingService>(
            MefTestHelpers.CreateExport<IEnvironmentVariableProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<DogfoodingService>();

    [TestInitialize]
    public void TestInitialize()
    {
        environmentVariableProvider = Substitute.For<IEnvironmentVariableProvider>();
    }

    [DataRow("", false)]
    [DataRow(null, false)]
    [DataRow(" ", false)]
    [DataRow("0", false)]
    [DataRow("1", true)]
    [DataRow("2", false)]
    [DataTestMethod]
    public void IsDogfooding_ReturnsAsExpected(string dogfoodingEnvVarValue, bool expectedStatus)
    {
        environmentVariableProvider.TryGet("SONARSOURCE_DOGFOODING").Returns(dogfoodingEnvVarValue);
        var testSubject = new DogfoodingService(environmentVariableProvider);

        testSubject.IsDogfoodingEnvironment.Should().Be(expectedStatus);
    }

    [TestMethod]
    public void IsDogfooding_CachesValue()
    {
        environmentVariableProvider.TryGet("SONARSOURCE_DOGFOODING").Returns("1");
        var testSubject = new DogfoodingService(environmentVariableProvider);

        _ = testSubject.IsDogfoodingEnvironment;
        _ = testSubject.IsDogfoodingEnvironment;
        _ = testSubject.IsDogfoodingEnvironment;

        environmentVariableProvider.ReceivedWithAnyArgs(1).TryGet(default);
    }
}
