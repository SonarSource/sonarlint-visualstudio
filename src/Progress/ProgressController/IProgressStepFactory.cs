//-----------------------------------------------------------------------
// <copyright file="IProgressStepFactory.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Factory representation to separate the <see cref="IProgressStepDefinition"/> from <see cref="IProgressStepOperation"/> and the execution notification using <see cref="IProgressStepExecutionEvents"/>
    /// </summary>
    public interface IProgressStepFactory
    {
        /// <summary>
        /// Create <see cref="CreateStepOperation"/> based on the <see cref="IProgressStepDefinition"/>
        /// </summary>
        /// <param name="controller">An instance of <see cref="IProgressController"/> for which to create the operation for <see cref="IProgressStepDefinition"/></param>
        /// <param name="definition">The definition to use when creating <see cref="IProgressStepOperation"/></param>
        /// <returns>An instance of <see cref="IProgressStepOperation"/></returns>
        IProgressStepOperation CreateStepOperation(IProgressController controller, IProgressStepDefinition definition);

        /// <summary>
        /// Returns a <see cref="IProgressStepExecutionEvents"/> that can be used to inform about the <see cref="IProgressStepOperation"/> execution changes
        /// </summary>
        /// <param name="stepOperation">The operation for which to fetch the <see cref="IProgressStepExecutionEvents"/></param>
        /// <returns>An instance of <see cref="IProgressStepExecutionEvents"/></returns>
        IProgressStepExecutionEvents GetExecutionCallback(IProgressStepOperation stepOperation);
    }
}
