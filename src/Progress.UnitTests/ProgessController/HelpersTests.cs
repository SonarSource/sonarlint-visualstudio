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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    public class HelpersTests
    {
        [TestMethod]
        public void Helpers_RunOnFinished()
        {
            // Arrange
            ConfigurableProgressEvents progressEvents = new ConfigurableProgressEvents();
            ProgressControllerResult? result = null;
            Action<ProgressControllerResult> action = (r) => result = r;

            foreach (ProgressControllerResult progressResult in Enum.GetValues(typeof(ProgressControllerResult)))
            {
                result = null;
                Helpers.RunOnFinished(progressEvents, action);

                // Act
                progressEvents.InvokeFinished(progressResult);

                // Assert
                result.Should().Be(progressResult, "Action was not called");
                progressEvents.AssertAllEventsAreUnregistered();
            }
        }
    }
}