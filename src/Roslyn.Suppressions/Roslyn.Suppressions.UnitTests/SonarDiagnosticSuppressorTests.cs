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
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    [TestClass]
    public class SonarDiagnosticSuppressorTests
    {
        [TestMethod]
        public void Suppressor_ReturnsExpectedSuppressions()
        {
            var testSubject = CreateTestSubject();

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
            var supported1 = CreateTestSubject().SupportedSuppressions;
            var supported2 = CreateTestSubject().SupportedSuppressions;

            // Have to use "Equals" to test identityt equality - see above test.
            supported1.Equals(supported2).Should().BeTrue();
        }

        [TestMethod]
        public void GetSuppressionsIsNotCalled_ContainerNotInitialized()
        {
            var createContainer = new Mock<Func<IContainer>>();

            var testSubject = CreateTestSubject(createContainer.Object);

            createContainer.Invocations.Count.Should().Be(0);

            var supportedSuppressions = testSubject.SupportedSuppressions;

            supportedSuppressions.Should().NotBeEmpty();

            createContainer.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void GetSuppressions_IsNotInConnectedMode_ContainerNotInitialized()
        {
            var createContainer = new Mock<Func<IContainer>>();

            var suppressionContext = CreateSuppressionContext(null);
            var reportedDiagnostics = CreateReportedDiagnostics();

            var testSubject = CreateTestSubject(createContainer.Object);

            var suppressions = testSubject.GetSuppressions(reportedDiagnostics, suppressionContext.Object);

            suppressions.Count().Should().Be(0);

            createContainer.Invocations.Count.Should().Be(0);

            suppressionContext.VerifyGet(c => c.IsInConnectedMode, Times.Once);
        }

        [TestMethod]
        public void GetSuppressions_IsInConnectedMode_ContainerInitialized()
        {
            var suppressionContext = CreateSuppressionContext("sonarKey");
            var suppressionChecker = CreateSuppressionChecker("sonarKey");
            
            var createContainer = new Mock<Func<IContainer>>();
            createContainer.Setup(x => x()).Returns(CreateContainer(suppressionChecker));

            var reportedDiagnostics = CreateReportedDiagnostics();

            var testSubject = CreateTestSubject(createContainer.Object);

            var suppressions = testSubject.GetSuppressions(reportedDiagnostics, suppressionContext.Object);

            suppressions.Count().Should().Be(0);

            createContainer.Invocations.Count.Should().Be(1);
        }

        [TestMethod]
        public void GetSuppressions_IsNotInConnectedMode_ShouldReturnEmpty()
        {
            var suppressionContext = CreateSuppressionContext(null);
            var reportedDiagnostics = CreateReportedDiagnostics();

            var testSubject = CreateTestSubject();

            var suppressions = testSubject.GetSuppressions(reportedDiagnostics, suppressionContext.Object);

            suppressions.Count().Should().Be(0);
        }

        [TestMethod]
        public void GetSuppressions_HasSuppressedDiagnostic_ShouldReturnSuppressions()
        {
            var suppressionContext = CreateSuppressionContext("sonarKey");
            var diag = CreateDiagnostic("S100");

            var suppressionChecker = CreateSuppressionChecker("sonarKey", diag);
            var container = CreateContainer(suppressionChecker);

            var reportedDiagnostics = CreateReportedDiagnostics(diag);
            
            var testSubject = CreateTestSubject(container);

            var suppressions = testSubject.GetSuppressions(reportedDiagnostics, suppressionContext.Object).ToList();

            suppressions.Count().Should().Be(1);
            suppressions[0].Descriptor.SuppressedDiagnosticId.Should().Be("S100");
        }

        [TestMethod]
        public void GetSuppressions_HasMixedSuppressedDiagnostic_ShouldReturnOnlySuppressed()
        {

            var suppressionContext = CreateSuppressionContext("sonarKey");
            var diag1 = CreateDiagnostic("S100");
            var diag2 = CreateDiagnostic("S101");
            var diag3 = CreateDiagnostic("S103");

            var suppressionChecker = CreateSuppressionChecker("sonarKey", diag1, diag2);
            var container = CreateContainer(suppressionChecker);

            var reportedDiagnostics = CreateReportedDiagnostics(diag1, diag2, diag3);

            var testSubject = CreateTestSubject(container);

            var suppressions = testSubject.GetSuppressions(reportedDiagnostics, suppressionContext.Object).ToList();

            suppressions.Count().Should().Be(2);
            suppressions[0].Descriptor.SuppressedDiagnosticId.Should().Be("S100");
            suppressions[1].Descriptor.SuppressedDiagnosticId.Should().Be("S101");
        }

        [TestMethod]
        public void GetSuppressions_HasNotSuppressedDiagnostic_ShouldReturnEmpty()
        {
            var suppressionContext = CreateSuppressionContext("sonarKey");
            var diag = CreateDiagnostic("S100");

            var reportedDiagnostics = CreateReportedDiagnostics(diag);

            var testSubject = CreateTestSubject();

            var suppressions = testSubject.GetSuppressions(reportedDiagnostics, suppressionContext.Object).ToList();

            suppressions.Count().Should().Be(0);
        }

        private ISuppressionChecker CreateSuppressionChecker(string settingsKey, params Diagnostic[] diagnostics)
        {
            var suppressionChecker = new Mock<ISuppressionChecker>();
            suppressionChecker.Setup(sc => sc.IsSuppressed(It.IsAny<Diagnostic>(), It.IsAny<string>())).Returns(false);

            foreach (var diagnostic in diagnostics)
            {
                suppressionChecker.Setup(sc => sc.IsSuppressed(diagnostic, settingsKey)).Returns(true);
            }

            return suppressionChecker.Object;
        }

        private Mock<ISuppressionExecutionContext> CreateSuppressionContext(string settingsKey)
        {
            var context = new Mock<ISuppressionExecutionContext>();
            context.SetupGet(c => c.SettingsKey).Returns(settingsKey);
            context.SetupGet(c => c.IsInConnectedMode).Returns(settingsKey != null);

            return context;
        }

        private Diagnostic CreateDiagnostic(string id)
        {
            var diagnostic = new Mock<Diagnostic>();
            diagnostic.SetupGet(d => d.Id).Returns(id);

            return diagnostic.Object;
        }

        private ImmutableArray<Diagnostic> CreateReportedDiagnostics(params Diagnostic[] diagnostics)
        {
            return diagnostics.ToImmutableArray();
        }

        private SonarDiagnosticSuppressor CreateTestSubject(IContainer container) =>
            CreateTestSubject(() => container);

        private SonarDiagnosticSuppressor CreateTestSubject(Func<IContainer> createContainer = null)
        {
            createContainer ??= () => CreateContainer();

            return new SonarDiagnosticSuppressor(createContainer);
        }

        private IContainer CreateContainer(ISuppressionChecker suppressionChecker = null, ILogger logger = null)
        {
            suppressionChecker ??= Mock.Of<ISuppressionChecker>();
            logger ??= Mock.Of<ILogger>();

            var container = new Mock<IContainer>();
            container.SetupGet(c => c.SuppressionChecker).Returns(suppressionChecker);
            container.SetupGet(c => c.Logger).Returns(logger);

            return container.Object;
        }

        
    }
}
