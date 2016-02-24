//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressStepFactory.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test helper for verification of <see cref="IProgressStepFactory"/> usage
    /// </summary>
    public class ConfigurableProgressStepFactory : IProgressStepFactory
    {
        public ConfigurableProgressStepFactory()
        {
            this.CreatedOperations = new Dictionary<IProgressStepDefinition, IProgressStepOperation>();
            this.CreatedNotifiers = new Dictionary<IProgressStepOperation, IProgressStepExecutionEvents>();
            this.CreateOpeartion = (definition) => new ConfigurableProgressTestOperation((c, n) => { });
        }

        #region Customization properties
        /// <summary>
        /// Map of the created <see cref="ConfigurableProgressTestOperation"/> for <see cref="IProgressStepDefinition"/>
        /// </summary>
        public Dictionary<IProgressStepDefinition, IProgressStepOperation> CreatedOperations
        {
            get;
            set;
        }

        /// <summary>
        /// Map of the <see cref="ConfigurableProgressStepExecutionNotifier"/> for <see cref="IProgressStepOperation"/>
        /// </summary>
        public Dictionary<IProgressStepOperation, IProgressStepExecutionEvents> CreatedNotifiers
        {
            get;
            set;
        }

        /// <summary>
        /// Replacement for the default <see cref="ConfigurableProgressTestOperation"/> which will be called for each <see cref="IProgressStepDefinition"/>
        /// </summary>
        public Func<IProgressStepDefinition, IProgressStepOperation> CreateOpeartion
        {
            get;
            set;
        }
        #endregion

        #region Verification methods
        public void AssertStepOperationsCreatedForDefinitions(IProgressStepDefinition[] definitions, IProgressStepOperation[] stepOperations)
        {
            Assert.AreEqual(definitions.Length, stepOperations.Length, "The number of definitions doesn't match the number of step operations");
            for (int i = 0; i < definitions.Length; i++)
            {
                Assert.AreSame(stepOperations[i], this.CreatedOperations[definitions[i]], "Mismatch at definition {0}", i);
            }
        }
        #endregion

        #region Test implementation of IProgressStepFactory (not to be used explicitly by the test code)
        IProgressStepOperation IProgressStepFactory.CreateStepOperation(IProgressController controller, IProgressStepDefinition definition)
        {
            return this.CreatedOperations[definition] = this.CreateOpeartion(definition);
        }

        IProgressStepExecutionEvents IProgressStepFactory.GetExecutionCallback(IProgressStepOperation stepOperation)
        {
            return this.CreatedNotifiers[stepOperation] = new ConfigurableProgressStepExecutionNotifier();
        }
        #endregion
    }
}
