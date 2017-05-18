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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableProgressStepExecutionEvents : IProgressStepExecutionEvents
    {
        private readonly List<Tuple<string, double>> progressEventsMessages = new List<Tuple<string, double>>();

        #region IProgressStepExecutionEvents

        void IProgressStepExecutionEvents.ProgressChanged(string progressDetailText, double progress)
        {
            this.progressEventsMessages.Add(Tuple.Create(progressDetailText, progress));
        }

        #endregion IProgressStepExecutionEvents

        #region Test helpers

        /// <summary>
        /// Verifies <see cref="IProgressStepExecutionEvents"/> messages
        /// </summary>
        public void AssertProgressMessages(params string[] expectedOrderedMessages)
        {
            string[] actualMessages = this.progressEventsMessages.Select(kv => kv.Item1).ToArray();
            actualMessages.Should().Equal(expectedOrderedMessages);
        }

        /// <summary>
        /// Verifies <see cref="IProgressStepExecutionEvents"/> progress
        /// </summary>
        public void AssertProgress(params double[] expectedProgress)
        {
            double[] actualProgress = this.progressEventsMessages.Select(kv => kv.Item2).ToArray();
            actualProgress.Should().Equal(expectedProgress);
        }

        public void Reset()
        {
            this.progressEventsMessages.Clear();
        }

        #endregion Test helpers
    }
}