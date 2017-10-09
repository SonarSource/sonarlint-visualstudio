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
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.Tests
{
    [TestClass]
    public class RetryHelperTests
    {
        [TestMethod]
        public void RetryOnException_WhenGivingZeroTimesRetry_ReturnsFalse()
        {
            // Arrange & Act
            var result = RetryHelper.RetryOnException(0, TimeSpan.Zero, () => { });

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void RetryOnException_WhenOperationSucceed_ReturnsTrue()
        {
            // Arrange & Act
            var result = RetryHelper.RetryOnException(1, TimeSpan.Zero, () => { });

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void RetryOnException_WhenOperationAlwaysFails_ReturnsFalse()
        {
            // Arrange & Act
            var result = RetryHelper.RetryOnException(1, TimeSpan.Zero, () => { throw new Exception(); });

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void RetryOnException_WhenFailsTwiceWithAFiveMilliSecondDelay_ReturnsFalseAndCompletesInTenSeconds()
        {
            // Arrange
            var delayMilliseconds = 5;
            var delay = TimeSpan.FromMilliseconds(delayMilliseconds);
            var timeWatch = new System.Diagnostics.Stopwatch();

            // Act
            timeWatch.Start();
            var result = RetryHelper.RetryOnException(2, delay, () => { throw new Exception(); });
            timeWatch.Stop();

            // Assert
            result.Should().BeFalse();
            timeWatch.ElapsedMilliseconds.Should().BeInRange(
                (delayMilliseconds * 2) - 200,
                (delayMilliseconds * 2) + 200);
        }

        [TestMethod]
        public async Task RetryOnExceptionAsync_WhenGivingZeroTimesRetry_ReturnsFalse()
        {
            // Arrange & Act
            var result = await RetryHelper.RetryOnExceptionAsync(0, TimeSpan.Zero, () => { return Task.CompletedTask; });

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public async Task RetryOnExceptionAsync_WhenOperationSucceed_ReturnsTrue()
        {
            // Arrange & Act
            var result = await RetryHelper.RetryOnExceptionAsync(1, TimeSpan.Zero, () => { return Task.CompletedTask; });

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task RetryOnExceptionAsync_WhenFailsTwiceWithAFiveMilliSecondDelay_ReturnsFalseAndCompletesInTenSeconds()
        {
            // Arrange
            var delayMilliseconds = 5;
            var delay = TimeSpan.FromMilliseconds(delayMilliseconds);
            var timeWatch = new System.Diagnostics.Stopwatch();

            // Act
            timeWatch.Start();
            var result = await RetryHelper.RetryOnExceptionAsync(2, delay, () => { throw new Exception(); });
            timeWatch.Stop();

            // Assert
            result.Should().BeFalse();
            timeWatch.ElapsedMilliseconds.Should().BeInRange(
                (delayMilliseconds * 2) - 200,
                (delayMilliseconds * 2) + 200);
        }
    }
}
