//-----------------------------------------------------------------------
// <copyright file="ConfigurableSolutionBinding.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSolutionBinding : ISolutionBinding
    {
        private int pendingFiles;
        private int writeSolutionBindingRequests;

        #region ISolutionBinding
        BoundSonarQubeProject ISolutionBinding.ReadSolutionBinding()
        {
            return this.CurrentBinding;
        }

        string ISolutionBinding.WriteSolutionBinding(ISourceControlledFileSystem sccFileSystem, BoundSonarQubeProject binding)
        {
            this.writeSolutionBindingRequests++;

            Assert.IsNotNull(sccFileSystem, "Required argument");
            Assert.IsNotNull(binding, "Required argument");

            string filePath = this.WriteSolutionBindingAction?.Invoke(binding) ?? binding.ProjectKey;
            this.pendingFiles++;

            sccFileSystem.QueueFileWrite(filePath ,() =>
            {
                this.pendingFiles--;
                return true;
            });
            return filePath;
        }
        #endregion

        #region Test helpers
        public BoundSonarQubeProject CurrentBinding { get; set; }

        public void AssertAllPendingWritten()
        {
            Assert.AreEqual(0, this.pendingFiles, "Not all the pending files were written");
        }

        public void AssertPendingFiles(int expected)
        {
            Assert.AreEqual(expected, this.pendingFiles, "Unexpected number of pending files");
        }

        public void AssertWriteSolutionBindingRequests(int expectedCount)
        {
            Assert.AreEqual(expectedCount, this.writeSolutionBindingRequests, "Unexpected number of calls");
        }

        public Func<BoundSonarQubeProject, string> WriteSolutionBindingAction { get; set; }
        #endregion
    }
}
