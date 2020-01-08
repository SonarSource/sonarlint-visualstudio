/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using SonarLint.VisualStudio.Progress.Observation;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IProgressVisualizer"/>
    /// </summary>
    public partial class ConfigurableProgressVisualizer : IProgressVisualizer
    {
        public bool IsShown { get; private set; }

        private ProgressControllerViewModel viewModel;

        public ConfigurableProgressVisualizer()
        {
            this.Reset();
        }

        #region Customization properties

        public ProgressControllerViewModel Root
        {
            get { return this.viewModel; }
        }

        public bool ThrowIfAccessedNotFromUIThread
        {
            get;
            set;
        }

        #endregion Customization properties

        #region Verification methods

        public void Reset()
        {
            this.viewModel = new ProgressControllerViewModel();
            this.IsShown = false;
        }

        #endregion Verification methods
    }
}