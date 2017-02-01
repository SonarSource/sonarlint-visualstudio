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
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TelemetryLoggerAccessorTests
    {
        [TestMethod]
        public void TelemetryLoggerAccessor_GetLogger()
        {
            // Setup
            ConfigurableServiceProvider sp = new ConfigurableServiceProvider();
            var loggerInstance = new ConfigurableTelemetryLogger();
            var componentModel = ConfigurableComponentModel.CreateWithExports(
                MefTestHelpers.CreateExport<ITelemetryLogger>(loggerInstance));
            sp.RegisterService(typeof(SComponentModel), componentModel);

            // Act
            ITelemetryLogger logger = TelemetryLoggerAccessor.GetLogger(sp);

            // Verify
            logger.Should().Be(loggerInstance, "Failed to find the MEF service: {0}", nameof(ITelemetryLogger));
        }
    }
}