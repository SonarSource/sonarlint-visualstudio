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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class FixedStepsProgressTests
    {
        [TestMethod]
        public void Ctor_Valid()
        {
            // 1. Null message is ok, 0 current step is ok
            var progressData = new FixedStepsProgress(null, 0, 1);
            progressData.Message.Should().BeNull();
            progressData.CurrentStep.Should().Be(0);
            progressData.TotalSteps.Should().Be(1);

            // 2. Non-null message, current and total steps are different
            progressData = new FixedStepsProgress("some message", 101, 202);
            progressData.Message.Should().Be("some message");
            progressData.CurrentStep.Should().Be(101);
            progressData.TotalSteps.Should().Be(202);

            // 3. Non-null message, current and total steps are same
            progressData = new FixedStepsProgress("some other message", 202, 202);
            progressData.Message.Should().Be("some other message");
            progressData.CurrentStep.Should().Be(202);
            progressData.TotalSteps.Should().Be(202);
        }

        [TestMethod]
        public void Ctor_InvalidCurrentStep_Throws()
        {
            // 1. Current step < 0
            Action act = () => new FixedStepsProgress(null, -1, 1);
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("currentStep");

            // 2. Current step > total steps
            act = () => new FixedStepsProgress(null, 2, 1);
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("currentStep");
        }

        [TestMethod]
        public void Ctor_InvalidTotalSteps_Throws()
        {
            // 1. Total steps < 1
            Action act = () => new FixedStepsProgress(null, 0, 0);
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("totalSteps");
        }
    }
}
