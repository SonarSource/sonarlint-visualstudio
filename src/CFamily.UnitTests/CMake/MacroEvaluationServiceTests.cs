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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class MacroEvaluationServiceTests
    {
        private static readonly IEvaluationContext EvaluationContext = Mock.Of<IEvaluationContext>();

        [TestMethod]
        public void Evaluate_NullInput_Null()
        {
            var evaluator = CreateEvaluator();
            var testSubject = new MacroEvaluationService(new TestLogger(), evaluator.Object);

            var result = testSubject.Evaluate(null, EvaluationContext);

            result.Should().BeNull();
            evaluator.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow("no macros")]
        [DataRow("{noOpeningDollar}")]
        [DataRow("$NoOpeningBrace}")]
        [DataRow("${NoClosingBrace")]
        [DataRow("${too.many.parts}")]
        [DataRow("${}")] // not enough parts
        [DataRow("$(wrong.brackets)")]
        [DataRow("${wrong:separator}")]
        [DataRow("${contains.nonword character}")]
        public void Evaluate_NoMacrosInInput_MacroEvaluatorNotCalled(string inputWithoutMacros)
        {
            var evaluator = new Mock<IMacroEvaluator>();
            var testSubject = CreateTestSubject(evaluator.Object);

            var result = testSubject.Evaluate(inputWithoutMacros, EvaluationContext);

            result.Should().Be(inputWithoutMacros);
            evaluator.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow("${name1}", "[XXX]")] // simple single param only 
        [DataRow("AAA_${name1}_BBB", "AAA_[XXX]_BBB")] // text and param
        [DataRow("${name1}${name1}", "[XXX][XXX]")] // duplicate param
        [DataRow("__${name1}__${a.x}__${AA.BB}__", "__[XXX]__[YYY]__[ZZZ]__")] // text and multiple params
        public void Evaluate_RecognisedMacrosInInput_ValuesReplaced(string input, string expectedOutput)
        {
            // The input parameters can use the following macros:
            // ${name1} => [XXX]
            // ${a.x}   => [YYY]
            // ${AA.BB} => [ZZZ]

            var evaluator = CreateEvaluator(
                (String.Empty, "name1", "[XXX]"),
                ("a", "x", "[YYY]"),
                ("AA", "BB", "[ZZZ]" ));

            var testSubject = CreateTestSubject(evaluator.Object);

            var result = testSubject.Evaluate(input, EvaluationContext);

            result.Should().Be(expectedOutput);
        }

        [TestMethod]
        [DataRow("${unknown}")]
        [DataRow("${unknown} ${a}")]
        [DataRow("${x.y} ${unknown}")]
        public void Evaluate_UnrecognisedMacrosInInput_Null(string inputWithMacros)
        {
            var evaluator = CreateEvaluator(
                (String.Empty, "a", "[a]"),
                ("x", "y", "[xy]"));

            var testSubject = CreateTestSubject(evaluator.Object);

            var result = testSubject.Evaluate(inputWithMacros, EvaluationContext);

            result.Should().BeNull();
        }

        [TestMethod]
        public void Evaluate_UnrecognisedMacrosInInput_MessageLogged()
        {
            var evaluator = CreateEvaluator();
            var logger = new TestLogger(logToConsole: true);
            var testSubject = CreateTestSubject(evaluator.Object, logger);

            var result = testSubject.Evaluate("${unknown} ${unknown2}", EvaluationContext);

            result.Should().BeNull();
            logger.AssertPartialOutputStringExists("${unknown}");
            logger.AssertPartialOutputStringDoesNotExist("${unknown2}"); // we give up on the the first failure
        }

        private static Mock<IMacroEvaluator> CreateEvaluator(params (string macroPrefix, string macroName, string valueToReturn)[] macroData)
        {
            Func<string, string, IEvaluationContext, string> finder = (prefix, name, context) =>
                macroData.FirstOrDefault(x => x.macroPrefix == prefix && x.macroName == name).valueToReturn;
           
            var evaluator = new Mock<IMacroEvaluator>();
            evaluator.Setup(x => x.TryEvaluate(It.IsAny<string>(), It.IsAny<string>(), EvaluationContext))
                .Returns(finder);
            return evaluator;
        }

        private static MacroEvaluationService CreateTestSubject(IMacroEvaluator evaluator, ILogger logger = null)
        {
            logger ??= new TestLogger(logToConsole: true);
            var testSubject = new MacroEvaluationService(logger, evaluator);
            return testSubject;
        }
    }
}
