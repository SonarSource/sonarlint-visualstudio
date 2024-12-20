/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using SonarLint.VisualStudio.Core.Logging;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Logging;

[TestClass]
public class LogContextManagerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<LogContextManager, ILogContextManager>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsNonSharedMefComponent<LogContextManager>();

    [TestMethod]
    public void EmptyContext()
    {
        var testSubject = new LogContextManager();

        testSubject.FormatedContext.Should().BeNull();
        testSubject.FormatedVerboseContext.Should().BeNull();
    }

    [TestMethod]
    public void Augmented_Immutable()
    {
        var testSubject = new LogContextManager();
        var contextualized= testSubject.CreateAugmentedContext(["a"]);
        var verboseContextualized = testSubject.CreateAugmentedVerboseContext(["b"]);
        var doubleContextualized = testSubject.CreateAugmentedContext(["c"]).CreateAugmentedVerboseContext(["d"]);

        testSubject.FormatedContext.Should().BeNull();
        testSubject.FormatedVerboseContext.Should().BeNull();
        contextualized.FormatedContext.Should().Be("a");
        contextualized.FormatedVerboseContext.Should().BeNull();
        verboseContextualized.FormatedContext.Should().BeNull();
        verboseContextualized.FormatedVerboseContext.Should().Be("b");
        doubleContextualized.FormatedContext.Should().Be("c");
        doubleContextualized.FormatedVerboseContext.Should().Be("d");
    }

    [TestMethod]
    public void Augmented_MultipleAtOnce_Combines() =>
        new LogContextManager()
            .CreateAugmentedContext(["a", "b"])
            .FormatedContext.Should().Be("a > b");

    [TestMethod]
    public void Augmented_MultipleInSequence_Combines() =>
        new LogContextManager()
            .CreateAugmentedContext(["a"])
            .CreateAugmentedContext(["b"])
            .FormatedContext.Should().Be("a > b");

    [TestMethod]
    public void Augmented_AtOnceAndInSequence_CombinesInCorrectOrder() =>
        new LogContextManager()
            .CreateAugmentedContext(["a"])
            .CreateAugmentedContext(["b", "c"])
            .CreateAugmentedContext(["d"])
            .FormatedContext.Should().Be("a > b > c > d");

    [TestMethod]
    public void AugmentedVerbose_MultipleAtOnce_Combines() =>
        new LogContextManager()
            .CreateAugmentedVerboseContext(["a", "b"])
            .FormatedVerboseContext.Should().Be("a > b");

    [TestMethod]
    public void AugmentedVerbose_MultipleInSequence_Combines() =>
        new LogContextManager()
            .CreateAugmentedVerboseContext(["a"])
            .CreateAugmentedVerboseContext(["b"])
            .FormatedVerboseContext.Should().Be("a > b");

    [TestMethod]
    public void AugmentedVerbose_AtOnceAndInSequence_CombinesInCorrectOrder() =>
        new LogContextManager()
            .CreateAugmentedVerboseContext(["a"])
            .CreateAugmentedVerboseContext(["b", "c"])
            .CreateAugmentedVerboseContext(["d"])
            .FormatedVerboseContext.Should().Be("a > b > c > d");
}
