//-----------------------------------------------------------------------
// <copyright file="IProgressStepDefinition.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Represents the step definition
    /// <seealso cref="ProgressStepDefinition"/>
    /// </summary>
    /// <remarks>The <see cref="IProgressStepFactory"/> needs to support the concrete type of the definition</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces", Justification = "Marker interface")]
    public interface IProgressStepDefinition
    {
        // Marker interface
    }
}
