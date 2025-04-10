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
using SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;

namespace SonarLint.VisualStudio.Integration.UnitTests.CSharpVB.StandaloneMode;

[TestClass]
public class StandaloneRoslynRuleParametersTests
{
    [TestMethod]
    public void Properties_ReturnExpectedValues()
    {
        var parameters = new Dictionary<string, string> { { "a", "b" } };
        const string repoKey = "csharpsquid";
        const string ruleKey = "S123";

        var testSubject = new StandaloneRoslynRuleParameters(new SonarCompositeRuleId(repoKey, ruleKey), parameters);

        testSubject.Key.Should().Be(ruleKey);
        testSubject.RepositoryKey.Should().Be(repoKey);
        testSubject.Parameters.Should().BeSameAs(parameters);
    }
}
