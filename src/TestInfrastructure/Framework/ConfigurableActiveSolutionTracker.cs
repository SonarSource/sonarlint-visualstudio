//-----------------------------------------------------------------------
// <copyright file="ConfigurableActiveSolutionTracker.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableActiveSolutionTracker : IActiveSolutionTracker
    {
        public event EventHandler ActiveSolutionChanged;

        #region Test helpers
        public void SimulateActiveSolutionChanged()
        {
            this.ActiveSolutionChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
