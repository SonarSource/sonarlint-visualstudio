//-----------------------------------------------------------------------
// <copyright file="ConfigurableSolutionBindingSerializer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSolutionBindingSerializer : ISolutionBindingSerializer
    {
        private int writtenFiles;

        #region ISolutionBindingSerializer
        BoundSonarQubeProject ISolutionBindingSerializer.ReadSolutionBinding()
        {
            this.ReadSolutionBindingAction?.Invoke();
            return this.CurrentBinding;
        }

        string ISolutionBindingSerializer.WriteSolutionBinding(BoundSonarQubeProject binding)
        {
            Assert.IsNotNull(binding, "Required argument");

            string filePath = this.WriteSolutionBindingAction?.Invoke(binding) ?? binding.ProjectKey;
            this.writtenFiles++;

            return filePath;
        }
        #endregion

        #region Test helpers
        public BoundSonarQubeProject CurrentBinding { get; set; }

        public void AssertWrittenFiles(int expected)
        {
            Assert.AreEqual(expected, this.writtenFiles, "Unexpected number of pending files");
        }

        public Func<BoundSonarQubeProject, string> WriteSolutionBindingAction { get; set; }

        public Action ReadSolutionBindingAction { get; set; }
        #endregion
    }
}
