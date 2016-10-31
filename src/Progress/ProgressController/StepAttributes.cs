//-----------------------------------------------------------------------
// <copyright file="StepAttributes.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Step attribute flags
    /// <seealso cref="ProgressStepDefinition"/>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "None represents zero in bits")]
    [Flags]
    public enum StepAttributes
    {
        /// <summary>
        /// Cancellable, Visible, Determinate, Foreground thread and impacting on the overall progress
        /// </summary>
        None = 0,

        /// <summary>
        /// Background thread flag
        /// </summary>
        BackgroundThread = 1,

        /// <summary>
        /// The step is not cancellable
        /// </summary>
        /// <seealso cref="IProgressStep.Cancellable"/>
        NonCancellable = 2,

        /// <summary>
        /// Hidden flag
        /// <seealso cref="IProgressStep.Hidden"/>
        /// </summary>
        Hidden = 4,

        /// <summary>
        /// Indeterminate progress flag
        /// </summary>
        /// <seealso cref="IProgressStep.Indeterminate"/>
        Indeterminate = 8,

        /// <summary>
        /// Does not impact the overall progress
        /// </summary>
        /// <seealso cref="IProgressStep.ImpactsProgress"/>
        NoProgressImpact = 16
    }
}
