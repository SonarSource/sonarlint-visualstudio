//-----------------------------------------------------------------------
// <copyright file="ConfigurableSolutionBinding.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSolutionBinding : ISolutionBinding
    {
        private readonly Dictionary<BoundSonarQubeProject, string> bindingToFileMap = new Dictionary<BoundSonarQubeProject, string>();

        #region ISolutionBinding
        BoundSonarQubeProject ISolutionBinding.ReadSolutionBinding()
        {
            return this.CurrentBinding;
        }

        string ISolutionBinding.WriteSolutionBinding(BoundSonarQubeProject binding)
        {
            Assert.IsNotNull(binding);

            this.CurrentBinding = binding;

            string filePath = null;
            this.bindingToFileMap.TryGetValue(binding, out filePath);
            return filePath;
        }
        #endregion

        #region Test helpers
        public BoundSonarQubeProject CurrentBinding
        {
            get;
            set;
        }

        public void RegisterFileToReturnForBinding(BoundSonarQubeProject binding, string filePath)
        {
            this.bindingToFileMap[binding] = filePath;
        }
        #endregion
    }
}
