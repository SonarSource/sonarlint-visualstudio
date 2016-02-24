//-----------------------------------------------------------------------
// <copyright file="IActiveSolutionTracker.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration
{
    internal interface IActiveSolutionTracker
    {
        /// <summary>
        /// The active solution has changed (either opened or closed).
        /// </summary>
        event EventHandler ActiveSolutionChanged;
    }
}
