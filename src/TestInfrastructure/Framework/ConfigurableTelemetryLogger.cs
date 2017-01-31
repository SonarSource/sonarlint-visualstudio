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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableTelemetryLogger : ITelemetryLogger
    {
        private readonly List<TelemetryEvent> events = new List<TelemetryEvent>();

        #region IVsTelemetryLogger
        void ITelemetryLogger.ReportEvent(TelemetryEvent telemetryEvent)
        {
            this.events.Add(telemetryEvent);
        }
        #endregion

        #region Helpers
        public void Reset()
        {
            this.events.Clear();
        }

        public void AssertSingleEventWasWritten(TelemetryEvent expected)
        {
            this.events.Should().HaveCount(1, "Unexpected events: {0}", string.Join(", ", this.events));
            TelemetryEvent actual = this.events.Single();
            expected.Should().Be( actual, "Unexpected entry name");
        }

        public void AssertNoEventWasWritten()
        {
            this.events.Should().HaveCount(0, "Unexpected events: {0}", string.Join(", ", this.events));
        }

        public void DumpAllToOutput()
        {
            this.events.ForEach(e => Debug.WriteLine(e));
        }

        #endregion
    }
}
