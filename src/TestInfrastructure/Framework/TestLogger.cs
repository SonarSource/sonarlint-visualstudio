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

using System.Collections.Concurrent;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Logging;

namespace SonarLint.VisualStudio.TestInfrastructure
{
    public class TestLogger : ILogger, ILoggerWriter, ILoggerSettingsProvider
    {
        public BlockingCollection<string> OutputStrings { get; private set; } = new();

        public event EventHandler LogMessageAdded;

        private readonly bool logToConsole;
        private readonly bool logThreadId;
        private readonly ILogger logger;

        public TestLogger(bool logToConsole = false, bool logThreadId = false)
        {
            // When executing tests in VS, the console output will automatically be captured by
            // the test runner. The Properties window for the test result will have an "Output"
            // link to show the output.
            this.logToConsole = logToConsole;

            this.logThreadId = logThreadId;
            logger = LoggerFactory.Default.Create(this, this);
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

        public void AssertPartialOutputStringDoesNotExist(params string[] expected)
        {
            this.OutputStrings.Should()
                .NotContain(msg => expected.All(partial => msg.Contains(partial)),
                because: $"MISSING TEXT: {string.Join(",", expected)}");
        }

        public void AssertNoOutputMessages()
        {
            OutputStrings.Should().HaveCount(0);
        }

        public void Reset()
        {
            OutputStrings = new BlockingCollection<string>();
        }

        #region ILogger methods

        public void WriteLine(string message) => logger.WriteLine(message);

        public void WriteLine(string messageFormat, params object[] args) => logger.WriteLine(messageFormat, args);

        public void WriteLine(MessageLevelContext context, string messageFormat, params object[] args) => logger.WriteLine(context, messageFormat, args);

        public void LogVerbose(string message, params object[] args) => logger.LogVerbose(message, args);

        public void LogVerbose(MessageLevelContext context, string messageFormat, params object[] args) => logger.WriteLine(context, messageFormat, args);

        public ILogger ForContext(params string[] context) => logger.ForContext(context);

        public ILogger ForVerboseContext(params string[] context) => logger.ForVerboseContext();

        #endregion

        void ILoggerWriter.WriteLine(string message)
        {
            var messageToLog = message + Environment.NewLine;
            OutputStrings.Add(messageToLog);
            if (logToConsole)
            {
                Console.WriteLine(messageToLog);
            }

            LogMessageAdded?.Invoke(this, EventArgs.Empty);
        }
        bool ILoggerSettingsProvider.IsVerboseEnabled => true;
        bool ILoggerSettingsProvider.IsThreadIdEnabled => logThreadId;
    }
}
