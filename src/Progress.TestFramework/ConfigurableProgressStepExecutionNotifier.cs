/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System;
using System.Collections.Generic;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test helper class to monitor the progress change notification (implements <see cref="IProgressStepExecutionEvents"/>)
    /// </summary>
    internal class ConfigurableProgressStepExecutionNotifier : IProgressStepExecutionEvents
    {
        public ConfigurableProgressStepExecutionNotifier()
        {
            this.ProgressChanges = new List<Tuple<string, double>>();
        }

        #region Verification

        public List<Tuple<string, double>> ProgressChanges
        {
            get;
            set;
        }

        #endregion Verification

        #region Configuration

        public Action<string, double> ProgressChangedAction
        {
            get;
            set;
        }

        #endregion Configuration

        #region Test implementation of IProgressStepExecutionEvents  (not to be used explicitly by the test code)

        void IProgressStepExecutionEvents.ProgressChanged(string progressDetailText, double progress)
        {
            this.ProgressChanges.Add(Tuple.Create(progressDetailText, progress));

            this.ProgressChangedAction?.Invoke(progressDetailText, progress);
        }

        #endregion Test implementation of IProgressStepExecutionEvents  (not to be used explicitly by the test code)
    }
}