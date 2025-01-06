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
public class LoggerContextManagerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<LoggerContextManager, ILoggerContextManager>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsNonSharedMefComponent<LoggerContextManager>();

    [TestMethod]
    public void EmptyContext()
    {
        var testSubject = new LoggerContextManager();

        testSubject.GetFormattedContextOrNull(default).Should().BeNull();
        testSubject.GetFormattedVerboseContextOrNull(default).Should().BeNull();
    }

    [TestMethod]
    public void Augmented_Immutable()
    {
        var testSubject = new LoggerContextManager();
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
        new LoggerContextManager()
            .CreateAugmentedContext(["a", "b"])
            .GetFormattedContextOrNull(default).Should().Be("a > b");

    [TestMethod]
    public void Augmented_MultipleInSequence_Combines() =>
        new LoggerContextManager()
            .CreateAugmentedContext(["a"])
            .CreateAugmentedContext(["b"])
            .GetFormattedContextOrNull(default).Should().Be("a > b");

    [TestMethod]
    public void Augmented_AtOnceAndInSequence_CombinesInCorrectOrder() =>
        new LoggerContextManager()
            .CreateAugmentedContext(["a"])
            .CreateAugmentedContext(["b", "c"])
            .CreateAugmentedContext(["d"])
            .GetFormattedContextOrNull(default).Should().Be("a > b > c > d");

    [TestMethod]
    public void AugmentedVerbose_MultipleAtOnce_Combines() =>
        new LoggerContextManager()
            .CreateAugmentedVerboseContext(["a", "b"])
            .GetFormattedVerboseContextOrNull(default).Should().Be("a > b");

    [TestMethod]
    public void AugmentedVerbose_MultipleInSequence_Combines() =>
        new LoggerContextManager()
            .CreateAugmentedVerboseContext(["a"])
            .CreateAugmentedVerboseContext(["b"])
            .GetFormattedVerboseContextOrNull(default).Should().Be("a > b");

    [TestMethod]
    public void AugmentedVerbose_AtOnceAndInSequence_CombinesInCorrectOrder() =>
        new LoggerContextManager()
            .CreateAugmentedVerboseContext(["a"])
            .CreateAugmentedVerboseContext(["b", "c"])
            .CreateAugmentedVerboseContext(["d"])
            .GetFormattedVerboseContextOrNull(default).Should().Be("a > b > c > d");

    [TestMethod]
    public void Get_NoContext_ReturnsNull() =>
        new LoggerContextManager().GetFormattedContextOrNull(new MessageLevelContext{Context = null, VerboseContext = ["c", "d"]}).Should().BeNull();

    [TestMethod]
    public void Get_NoBaseContext_ReturnsMessageLevelContextOnly() =>
        new LoggerContextManager().GetFormattedContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = ["c", "d"]}).Should().Be("a > b");

    [TestMethod]
    public void Get_MessageLevelContextNotCached()
    {
        var testSubject = new LoggerContextManager();
        testSubject.GetFormattedContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = ["c", "d"]}).Should().Be("a > b");
        testSubject.GetFormattedContextOrNull(new MessageLevelContext{Context = ["a2", "b2"], VerboseContext = ["c", "d"]}).Should().Be("a2 > b2");
    }

    [TestMethod]
    public void Get_NoMessageLevelContext_ReturnsBaseContextOnly() =>
        new LoggerContextManager().CreateAugmentedContext(["x", "y"]).GetFormattedContextOrNull(new MessageLevelContext{Context = null, VerboseContext = ["c", "d"]}).Should().Be("x > y");

    [TestMethod]
    public void Get_BothContexts_CombinesInOrder() =>
        new LoggerContextManager().CreateAugmentedContext(["x", "y"]).GetFormattedContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = ["c", "d"]}).Should().Be("x > y > a > b");

    [TestMethod]
    public void GetVerbose_NoContext_ReturnsNull() =>
        new LoggerContextManager().GetFormattedVerboseContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = null}).Should().BeNull();

    [TestMethod]
    public void GetVerbose_NoBaseContext_ReturnsMessageLevelContextOnly() =>
        new LoggerContextManager().GetFormattedVerboseContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = ["c", "d"]}).Should().Be("c > d");

    [TestMethod]
    public void GetVerbose_MessageLevelContextNotCached()
    {
        var testSubject = new LoggerContextManager();
        testSubject.GetFormattedVerboseContextOrNull(new MessageLevelContext { Context = ["a", "b"], VerboseContext = ["c", "d"] }).Should().Be("c > d");
        testSubject.GetFormattedVerboseContextOrNull(new MessageLevelContext { Context = ["a", "b"], VerboseContext = ["c2", "d2"] }).Should().Be("c2 > d2");
    }

    [TestMethod]
    public void GetVerbose_NoMessageLevelContext_ReturnsBaseContextOnly() =>
        new LoggerContextManager().CreateAugmentedVerboseContext(["v", "w"]).GetFormattedVerboseContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = null}).Should().Be("v > w");

    [TestMethod]
    public void GetVerbose_BothContexts_CombinesInOrder() =>
        new LoggerContextManager().CreateAugmentedVerboseContext(["v", "w"]).GetFormattedVerboseContextOrNull(new MessageLevelContext{Context = ["a", "b"], VerboseContext = ["c", "d"]}).Should().Be("v > w > c > d");
}
