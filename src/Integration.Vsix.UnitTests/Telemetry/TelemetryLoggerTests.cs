/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
 using Xunit;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests.Telemetry
{
    public class TelemetryLoggerTests
    {
        private static readonly Guid ExpectedCommandSetIdentifier = new Guid("{DB0701CC-1E44-41F7-97D6-29B160A70BCB}");

        private ConfigurableServiceProvider serviceProvider;
        private DTEMock dte;
        private ConfigurableVsOutputWindowPane outputPane;
        private TelemetryLogger testSubject;

        public TelemetryLoggerTests()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            this.dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(EnvDTE.DTE), this.dte);

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.testSubject = new TelemetryLogger(this.serviceProvider);
        }

        [Fact]
        public void TelemetryLogger_ReportEvent()
        {
            // Arrange
            Dictionary<TelemetryEvent, int> discoveredMap = new Dictionary<TelemetryEvent, int>();

            foreach (TelemetryEvent evnt in Enum.GetValues(typeof(TelemetryEvent)).OfType<TelemetryEvent>())
            {
                this.dte.Commands.RaiseAction = (commandGroup, commandId) =>
                {
                    ExpectedCommandSetIdentifier.Should().Be( commandGroup, "Unexpected command group");
                    discoveredMap[evnt] = commandId;
                };

                // Act
                this.testSubject.ReportEvent(evnt);
            }

            // Assert
            TelemetryEvent[] expectedEvents = Enum.GetValues(typeof(TelemetryEvent)).Cast<TelemetryEvent>().ToArray();
            TelemetryEvent[] actualEvents = discoveredMap.Keys.ToArray();

            actualEvents.Should().Equal(expectedEvents,
                "Expecting each telemetry event to call a unique command - no all telemetry events invoked a command. Missing:{0}, NotExpected:{1}",
                  string.Join(", ", expectedEvents.Except(actualEvents)),
                    string.Join(", ", actualEvents.Except(expectedEvents)));

            int[] expectedIds = Enum.GetValues(typeof(SonarLintSqmCommandIds)).Cast<int>().ToArray();
            int[] actualIds = discoveredMap.Values.ToArray();

            actualIds.Should().Equal(expectedIds,
                "Expecting each telemetry event to call a unique command - some of the commands were not uniquely called. Missing:{0}, NotExpected:{1}",
                string.Join(", ", expectedIds.Except(actualIds).Cast<SonarLintSqmCommandIds>()),
                string.Join(", ", actualIds.Except(expectedIds).Cast<SonarLintSqmCommandIds>()));
        }
    }
}
