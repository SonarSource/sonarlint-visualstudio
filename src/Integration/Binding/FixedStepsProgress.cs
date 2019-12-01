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

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Data class used for reporting progress through a fixed number of steps
    /// </summary>
    internal struct FixedStepsProgress
    {
        public FixedStepsProgress(string message, int currentStep, int totalSteps)
        {
            if (currentStep < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentStep));
            }
            if (totalSteps < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(totalSteps));
            }
            if (currentStep > totalSteps)
            {
                throw new ArgumentOutOfRangeException(nameof(currentStep));
            }

            Message = message;
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
        }

        public string Message { get; }

        public int CurrentStep { get; }

        public int TotalSteps { get; }
    }
}
