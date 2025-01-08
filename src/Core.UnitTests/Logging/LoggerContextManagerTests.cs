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

using SonarLint.VisualStudio.Core.Logging;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Logging;

[TestClass]
public class LoggerContextManagerTests
{
    private LoggerContextManager testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new LoggerContextManager();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<LoggerContextManager, ILoggerContextManager>();

    [TestMethod]
    public void MefCtor_CheckIsTransient() =>
        MefTestHelpers.CheckIsNonSharedMefComponent<LoggerContextManager>();

    [TestMethod]
    public void DefaultCtor_EmptyContext()
    {
        testSubject.GetFormattedContextOrNull(default).Should().BeNull();
        testSubject.GetFormattedVerboseContextOrNull(default).Should().BeNull();
    }

    [TestMethod]
    public void Augmented_Immutable()
    {
        var contextualized= testSubject.CreateAugmentedContext(["a"]);
        var verboseContextualized = testSubject.CreateAugmentedVerboseContext(["b"]);
        var doubleContextualized = testSubject.CreateAugmentedContext(["c"]).CreateAugmentedVerboseContext(["d"]);

        testSubject.GetFormattedContextOrNull(default).Should().BeNull();
        testSubject.GetFormattedVerboseContextOrNull(default).Should().BeNull();
        contextualized.GetFormattedContextOrNull(default).Should().Be("a");
        contextualized.GetFormattedVerboseContextOrNull(default).Should().BeNull();
        verboseContextualized.GetFormattedContextOrNull(default).Should().BeNull();
        verboseContextualized.GetFormattedVerboseContextOrNull(default).Should().Be("b");
        doubleContextualized.GetFormattedContextOrNull(default).Should().Be("c");
        doubleContextualized.GetFormattedVerboseContextOrNull(default).Should().Be("d");
    }

    [TestMethod]
    public void Augmented_MultipleAtOnce_Combines() =>
        testSubject
            .CreateAugmentedContext(["a", "b"])
            .GetFormattedContextOrNull(default).Should().Be("a > b");

    [TestMethod]
    public void Augmented_MultipleInSequence_Combines() =>
        testSubject
            .CreateAugmentedContext(["a"])
            .CreateAugmentedContext(["b"])
            .GetFormattedContextOrNull(default).Should().Be("a > b");

    [TestMethod]
    public void Augmented_AtOnceAndInSequence_CombinesInCorrectOrder() =>
        testSubject
            .CreateAugmentedContext(["a"])
            .CreateAugmentedContext(["b", "c"])
            .CreateAugmentedContext(["d"])
            .GetFormattedContextOrNull(default).Should().Be("a > b > c > d");

    [TestMethod]
    public void AugmentedVerbose_MultipleAtOnce_Combines() =>
        testSubject
            .CreateAugmentedVerboseContext(["a", "b"])
            .GetFormattedVerboseContextOrNull(default).Should().Be("a > b");

    [TestMethod]
    public void AugmentedVerbose_MultipleInSequence_Combines() =>
        testSubject
            .CreateAugmentedVerboseContext(["a"])
            .CreateAugmentedVerboseContext(["b"])
            .GetFormattedVerboseContextOrNull(default).Should().Be("a > b");

    [TestMethod]
    public void AugmentedVerbose_AtOnceAndInSequence_CombinesInCorrectOrder() =>
        testSubject
            .CreateAugmentedVerboseContext(["a"])
            .CreateAugmentedVerboseContext(["b", "c"])
            .CreateAugmentedVerboseContext(["d"])
            .GetFormattedVerboseContextOrNull(default).Should().Be("a > b > c > d");

    [TestMethod]
    public void GetFormattedContextOrNull_NoContext_ReturnsNull() =>
        testSubject.GetFormattedContextOrNull(new MessageLevelContext{Context = null, VerboseContext = ["c", "d"]}).Should().BeNull();

    [TestMethod]
    public void GetFormattedContextOrNull_NoBaseContext_ReturnsMessageLevelContextOnly() =>
        testSubject.GetFormattedContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = ["c", "d"]}).Should().Be("a > b");

    [TestMethod]
    public void GetFormattedContextOrNull_NullAndEmptyItems_ReturnsNonNullMessageLevelContextOnly() =>
        testSubject.GetFormattedContextOrNull(new MessageLevelContext{Context = ["a", null, "", "b"], VerboseContext = ["c", "d"]}).Should().Be("a > b");

    [TestMethod]
    public void GetFormattedContextOrNull_NullAndEmptyItemsOnly_ReturnsNull() =>
        testSubject.GetFormattedContextOrNull(new MessageLevelContext{Context = [null, ""], VerboseContext = ["c", "d"]}).Should().BeNull();

    [TestMethod]
    public void GetFormattedContextOrNull_MessageLevelContextNotCached()
    {
        testSubject.GetFormattedContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = ["c", "d"]}).Should().Be("a > b");
        testSubject.GetFormattedContextOrNull(new MessageLevelContext{Context = ["a2", "b2"], VerboseContext = ["c", "d"]}).Should().Be("a2 > b2");
    }

    [TestMethod]
    public void GetFormattedContextOrNull_NoMessageLevelContext_ReturnsBaseContextOnly() =>
        testSubject.CreateAugmentedContext(["x", "y"]).GetFormattedContextOrNull(new MessageLevelContext{Context = null, VerboseContext = ["c", "d"]}).Should().Be("x > y");

    [TestMethod]
    public void GetFormattedContextOrNull_BothContexts_CombinesInOrder() =>
        testSubject.CreateAugmentedContext(["x", "y"]).GetFormattedContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = ["c", "d"]}).Should().Be("x > y > a > b");

    [TestMethod]
    public void GetFormattedVerboseContextOrNull_NoContext_ReturnsNull() =>
        testSubject.GetFormattedVerboseContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = null}).Should().BeNull();

    [TestMethod]
    public void GetFormattedVerboseContextOrNull_NoBaseContext_ReturnsMessageLevelContextOnly() =>
        testSubject.GetFormattedVerboseContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = ["c", "d"]}).Should().Be("c > d");

    [TestMethod]
    public void GetFormattedVerboseContextOrNull_NullAndEmptyItems_ReturnsNonNullMessageLevelContextOnly() =>
        testSubject.GetFormattedVerboseContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = ["c", null, "", "d"]}).Should().Be("c > d");

    [TestMethod]
    public void GetFormattedVerboseContextOrNull_NullAndEmptyItemsOnly_ReturnsNull() =>
        testSubject.GetFormattedVerboseContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = [null, ""]}).Should().BeNull();

    [TestMethod]
    public void GetFormattedVerboseContextOrNull_MessageLevelContextNotCached()
    {
        testSubject.GetFormattedVerboseContextOrNull(new MessageLevelContext { Context = ["a", "b"], VerboseContext = ["c", "d"] }).Should().Be("c > d");
        testSubject.GetFormattedVerboseContextOrNull(new MessageLevelContext { Context = ["a", "b"], VerboseContext = ["c2", "d2"] }).Should().Be("c2 > d2");
    }

    [TestMethod]
    public void GetFormattedVerboseContextOrNull_NoMessageLevelContext_ReturnsBaseContextOnly() =>
        testSubject.CreateAugmentedVerboseContext(["v", "w"]).GetFormattedVerboseContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = null}).Should().Be("v > w");

    [TestMethod]
    public void GetFormattedVerboseContextOrNull_BothContexts_CombinesInOrder() =>
        testSubject.CreateAugmentedVerboseContext(["v", "w"]).GetFormattedVerboseContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = ["c", "d"]}).Should().Be("v > w > c > d");
}
