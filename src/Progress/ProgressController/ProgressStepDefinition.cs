/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Diagnostics;
using System.Threading;

namespace SonarLint.VisualStudio.Progress.Controller
{
    /// <summary>
    /// Simple data class representing the step definition
    /// </summary>
    public class ProgressStepDefinition : IProgressStepDefinition
    {
        public ProgressStepDefinition(string displayText, StepAttributes attributes, Action<CancellationToken, IProgressStepExecutionEvents> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            this.DisplayText = displayText;
            this.Attributes = attributes;
            this.Operation = operation;
        }

        /// <summary>
        /// Display text describing the step.
        /// </summary>
        public string DisplayText { get; private set; }

        /// <summary>
        /// Operation to run when executing the step
        /// </summary>
        public Action<CancellationToken, IProgressStepExecutionEvents> Operation { get; private set; }

        /// <summary>
        /// Attributes of the step
        /// </summary>
        public StepAttributes Attributes { get; private set; }
    }
}
