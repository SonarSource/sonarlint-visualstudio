/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class TestLogger : ILogger
    {
        public IList<string> OutputStrings { get; } = new List<string>();

        private readonly bool logToConsole;

        public TestLogger()
            : this(false)
        {
        }

        public TestLogger(bool logToConsole)
        {
            // When executing tests in VS, the console output will automatically be captured by
            // the test runner. The Properties window for the test result will have an "Output"
            // link to show the output.
            this.logToConsole = logToConsole;
        }

        public void AssertOutputStrings(int expectedOutputMessages)
        {
            this.OutputStrings.Should().HaveCount(expectedOutputMessages);
        }

        public void AssertOutputStrings(params string[] orderedOutputMessages)
        {
            string[] expected = orderedOutputMessages.Select(o => o + Environment.NewLine).ToArray(); // All messages are postfixed by a newline
            this.OutputStrings.Should().Equal(expected);
        }

        public void AssertPartialOutputStrings(params string[] orderedPartialOutputMessages)
        {
            this.OutputStrings.Should().Equal(orderedPartialOutputMessages, (actualValue, expectedValue) =>
                actualValue.Contains(expectedValue));
        }

        public void AssertOutputStringExists(string expected)
        {
            this.OutputStrings.Should().Contain(expected + Environment.NewLine); // All messages are postfixed by a newline
        }

        public void AssertOutputStringDoesNotExist(string expected)
        {
            this.OutputStrings.Contains(expected + Environment.NewLine).Should().BeFalse(); // All messages are postfixed by a newline
        }

        public void AssertPartialOutputStringExists(params string[] expected)
        {
            this.OutputStrings.Should()
                .Contain(msg => expected.All(partial => msg.Contains(partial)),
                because: $"MISSING TEXT: {string.Join(",", expected)}");
        }

        public void AssertNoOutputMessages()
        {
            OutputStrings.Should().HaveCount(0);
        }

        public void Reset()
        {
            OutputStrings.Clear();
        }

        #region ILogger methods

        public void WriteLine(string message)
        {
            OutputStrings.Add(message + Environment.NewLine);
            if (logToConsole)
            {
                Console.WriteLine(message);
            }
        }

        public void WriteLine(string messageFormat, params object[] args)
        {
            WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, messageFormat, args));
        }

        #endregion
    }
}
