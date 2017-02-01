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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsOutputWindowPane : IVsOutputWindowPane
    {
        private readonly List<string> outputStrings = new List<string>();

        public bool IsActivated { get; private set; }

        public string Name { get; private set; }

        public bool ClearOnSolutionEvents { get; private set; }

        public ConfigurableVsOutputWindowPane()
        {
        }

        public ConfigurableVsOutputWindowPane(string name, bool initVisible, bool clearOnSolutionEvents)
        {
            this.Name = name;
            this.IsActivated = true;
            this.ClearOnSolutionEvents = clearOnSolutionEvents;
        }

        #region IVsOutputWindowPane

        int IVsOutputWindowPane.Activate()
        {
            this.IsActivated = true;
            return VSConstants.S_OK;
        }

        int IVsOutputWindowPane.Clear()
        {
            this.outputStrings.Clear();
            return VSConstants.S_OK;
        }

        int IVsOutputWindowPane.FlushToTaskList()
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.GetName(ref string pbstrPaneName)
        {
            pbstrPaneName = Name;
            return VSConstants.S_OK;
        }

        int IVsOutputWindowPane.Hide()
        {
            this.IsActivated = false;
            return VSConstants.S_OK;
        }

        int IVsOutputWindowPane.OutputString(string pszOutputString)
        {
            this.outputStrings.Add(pszOutputString);
            return VSConstants.S_OK;
        }

        int IVsOutputWindowPane.OutputStringThreadSafe(string pszOutputString)
        {
            this.outputStrings.Add(pszOutputString);
            return VSConstants.S_OK;
        }

        int IVsOutputWindowPane.OutputTaskItemString(string pszOutputString, VSTASKPRIORITY nPriority, VSTASKCATEGORY nCategory, string pszSubcategory, int nBitmap, string pszFilename, uint nLineNum, string pszTaskItemText)
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.OutputTaskItemStringEx(string pszOutputString, VSTASKPRIORITY nPriority, VSTASKCATEGORY nCategory, string pszSubcategory, int nBitmap, string pszFilename, uint nLineNum, string pszTaskItemText, string pszLookupKwd)
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.SetName(string pszPaneName)
        {
            this.Name = pszPaneName;
            return VSConstants.S_OK;
        }

        #endregion IVsOutputWindowPane

        public void AssertOutputStrings(int expectedOutputMessages)
        {
            this.outputStrings.Should().HaveCount(expectedOutputMessages);
        }

        public void AssertOutputStrings(params string[] orderedOutputMessages)
        {
            string[] expected = orderedOutputMessages.Select(o => o + Environment.NewLine).ToArray(); // All messages are postfixed by a newline
            this.outputStrings.Should().Equal(expected);
        }

        public void AssertMessageContainsAllWordsCaseSensitive(int messageIndex, string[] words, char[] splitter = null)
        {
            this.outputStrings.Count.Should().BeGreaterThan(messageIndex, "Message not found for specified index {0}", messageIndex);

            var allWords = new HashSet<string>(this.outputStrings[messageIndex].Split(splitter, StringSplitOptions.RemoveEmptyEntries));
            allWords.IsSupersetOf(words).Should().BeTrue("Not all words found. Missing: {0}.\nAll words: {1}", string.Join(", ", words.Except(allWords)), string.Join(" ", allWords));
        }

        public void Reset()
        {
            this.outputStrings.Clear();
        }
    }
}