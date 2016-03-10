//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressStepExecutionEvents.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableProgressStepExecutionEvents : IProgressStepExecutionEvents
    {
        private List<Tuple<string, double>> progressEventsMessages = new List<Tuple<string, double>>();

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
            CollectionAssert.AreEqual(expectedOrderedMessages, actualMessages, "Unexpected messages: {0}", string.Join(", ", actualMessages));
        }

        /// <summary>
        /// Verifies <see cref="IProgressStepExecutionEvents"/> progress
        /// </summary>
        public void AssertProgress(params double[] expectedProgress)
        {
            double[] actualProgress = this.progressEventsMessages.Select(kv => kv.Item2).ToArray();
            CollectionAssert.AreEqual(expectedProgress, actualProgress, "Unexpected progress: {0}", string.Join(", ", actualProgress));
        }

        public void Reset()
        {
            this.progressEventsMessages.Clear();
        }
        #endregion
    }
}
