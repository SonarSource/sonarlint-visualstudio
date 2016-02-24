//-----------------------------------------------------------------------
// <copyright file="IProgressVisualizer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Observation.ViewModels;

namespace SonarLint.VisualStudio.Progress.Observation
{
    /// <summary>
    /// Common inteface for components that can display the <see cref="ProgressControllerViewModel"/> content
    /// </summary>
    public interface IProgressVisualizer
    {
        /// <summary>
        /// Gets/sets the view model to be used by the visualizer
        /// </summary>
        ProgressControllerViewModel ViewModel { get; set; }

        /// <summary>
        /// Shows the visualizer
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the visualizer
        /// </summary>
        void Hide();
    }
}
