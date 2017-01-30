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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableProgressStepExecutionEvents : IProgressStepExecutionEvents
    {
        private readonly List<Tuple<string, double>> progressEventsMessages = new List<Tuple<string, double>>();

        #region IProgressStepExecutionEvents
        void IProgressStepExecutionEvents.ProgressChanged(string progressDetailText, double progress)
        {
            this.progressEventsMessages.Add(Tuple.Create(progressDetailText, progress));
        }
        #endregion

        #region Test helpers
        /// <summary>
        /// Verifies <see cref="IProgressStepExecutionEvents"/> messages
        /// </summary>
        public void AssertProgressMessages(params string[] expectedOrderedMessages)
        {
            string[] actualMessages = this.progressEventsMessages.Select(kv => kv.Item1).ToArray();
            expectedOrderedMessages.Should().Equal(actualMessages, "Unexpected messages: {0}", string.Join(", ", actualMessages));
        }

        /// <summary>
        /// Verifies <see cref="IProgressStepExecutionEvents"/> progress
        /// </summary>
        public void AssertProgress(params double[] expectedProgress)
        {
            double[] actualProgress = this.progressEventsMessages.Select(kv => kv.Item2).ToArray();
            expectedProgress.Should().Equal(actualProgress, "Unexpected progress: {0}", string.Join(", ", actualProgress));
        }

        public void Reset()
        {
            this.progressEventsMessages.Clear();
        }
        #endregion
    }
}
