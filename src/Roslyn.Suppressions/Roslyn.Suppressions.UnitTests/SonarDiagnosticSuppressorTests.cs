/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    [TestClass]
    public class SonarDiagnosticSuppressorTests
    {
        [TestMethod]
        public void Suppressor_ReturnsExpectedSuppressions()
        {
            var testSubject = new SonarDiagnosticSuppressor();

            var actual = testSubject.SupportedSuppressions;

            // Note: ImmutableArray<> is a value type so we can't use ReferenceEquals or ".Should().BeSameAs(...)"
            // to check if the object is the same. Howevere, "Equals" is overloaded to test the underlying arrays are
            // the same instance.
            // See https://docs.microsoft.com/en-us/dotnet/api/system.collections.immutable.immutablearray-1.equals?view=net-6.0
            actual.Equals(SupportedSuppressionsBuilder.Instance.Descriptors).Should().BeTrue();
        }

        [TestMethod]
        public void Suppressors_AllSuppressorsReturnSameInstances()
        {
            // Perf - check every instance reuses the same set of descriptors
            var supported1 = new SonarDiagnosticSuppressor().SupportedSuppressions;
            var supported2 = new SonarDiagnosticSuppressor().SupportedSuppressions;

            // Have to use "Equals" to test identityt equality - see above test.
            supported1.Equals(supported2).Should().BeTrue();
        }
    }
}
