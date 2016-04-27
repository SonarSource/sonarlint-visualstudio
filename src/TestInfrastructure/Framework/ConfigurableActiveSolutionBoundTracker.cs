//-----------------------------------------------------------------------
// <copyright file="ConfigurableActiveSolutionBoundTracker.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableActiveSolutionBoundTracker : IActiveSolutionBoundTracker
    {
        public bool IsActiveSolutionBound { get; set; }

        public event EventHandler<bool> SolutionBindingChanged;

        public void SimulateSolutionBindingChanged(bool bound)
        {
            SolutionBindingChanged(this, bound);
        }
    }
}
