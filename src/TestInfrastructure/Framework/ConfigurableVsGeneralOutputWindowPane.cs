//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsGeneralOutputWindowPane.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsGeneralOutputWindowPane : IVsOutputWindowPane
    {
        private readonly List<string> outputStrings = new List<string>();

        #region IVsOutputWindowPane
        int IVsOutputWindowPane.Activate()
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.Clear()
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.FlushToTaskList()
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.GetName(ref string pbstrPaneName)
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.Hide()
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.OutputString(string pszOutputString)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
        #endregion

        public void AssertOutputStrings(int expectedOutputMessages)
        {
            Assert.AreEqual(expectedOutputMessages, this.outputStrings.Count, "Unexpected number of messages. Messages: {0}", string.Join(", ", this.outputStrings));
        }

        public void AssertOutputStrings(params string[] orderedOutputMessages)
        {
            string[] expected = orderedOutputMessages.Select(o => Environment.NewLine + o).ToArray(); // All messages are prefixed by a newline
            CollectionAssert.AreEqual(expected, this.outputStrings.ToArray(), "Unexpected messages: '{0}'", string.Join(", ", this.outputStrings));
        }

        public void AssertMessageContainsAllWordsCaseSensitive(int messageIndex, string[] words, char[] splitter = null)
        {
            Assert.IsTrue(this.outputStrings.Count > messageIndex, "Message not found for specified index {0}", messageIndex);

            var allWords = new HashSet<string>(this.outputStrings[messageIndex].Split(splitter, StringSplitOptions.RemoveEmptyEntries));
            Assert.IsTrue(allWords.IsSupersetOf(words), "Not all words found. Missing: {0}.\nAll words: {1}", string.Join(", ", words.Except(allWords)), string.Join(" ", allWords));
        }

        public void Reset()
        {
            this.outputStrings.Clear();
        }
    }
}
