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
using System.Globalization;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Default implementation of <see cref="IProgressStepFactory"/>
    /// </summary>
    /// <remarks>The class supports only <see cref="ProgressStepDefinition"/> definitions and <see cref="ProgressControllerStep"/> operation</remarks>
    public class DefaultProgressStepFactory : IProgressStepFactory
    {
        #region IProgressStepFactory
        /// <summary>
        /// Creates an operation from definition
        /// </summary>
        /// <param name="controller">Required <see cref="IProgressController"/></param>
        /// <param name="definition">Required and must derived from <see cref="ProgressStepDefinition"/></param>
        /// <returns><see cref="ProgressControllerStep"/></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1", Justification = "False positive")]
        public IProgressStepOperation CreateStepOperation(IProgressController controller, IProgressStepDefinition definition)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            ProgressStepDefinition supportedDefinition = definition as ProgressStepDefinition;
            if (supportedDefinition == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ProgressResources.UnsupportedTypeException, definition.GetType().FullName, typeof(ProgressStepDefinition).FullName));
            }

            ProgressControllerStep step = new ProgressControllerStep(controller, supportedDefinition);
            return step;
        }

        /// <summary>
        /// Returns a callback for <see cref="IProgressStepOperation"/>
        /// </summary>
        /// <param name="stepOperation">Required and must derived from <see cref="ProgressControllerStep"/></param>
        /// <returns>Returns a callback supporting instance</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "False positive")]
        public IProgressStepExecutionEvents GetExecutionCallback(IProgressStepOperation stepOperation)
        {
            ProgressControllerStep supportedStep = stepOperation as ProgressControllerStep;
            if (supportedStep == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ProgressResources.UnsupportedTypeException, stepOperation.GetType().FullName, typeof(ProgressControllerStep).FullName));
            }

            return supportedStep;
        }
        #endregion
    }
}
