/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using FluentAssertions;

using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using Xunit;

namespace SonarLint.VisualStudio.Progress.UnitTests
{

    public class ProgressViewModelTests
    {
        [Fact]
        
        public void ProgressViewModel_AllPublicPropertiesNotifyChanges()
        {
            ProgressViewModel testSubject = new ProgressViewModel();

            ViewModelVerifier.RunVerificationTest(testSubject, "Value", double.NaN, 1.0);
            ViewModelVerifier.RunVerificationTest(testSubject, "IsIndeterminate", true, false);
        }

        [Fact]
        
        public void ValueProperty_WhenSettingNegativeinfinity_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            ProgressViewModel testSubject = new ProgressViewModel();

            // Act
            Action act = () => testSubject.Value = double.NegativeInfinity;

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Fact]
        
        public void ValueProperty_WhenSettingPositiveinfinity_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            ProgressViewModel testSubject = new ProgressViewModel();

            // Act
            Action act = () => testSubject.Value = double.PositiveInfinity;

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Fact]
        
        public void ValueProperty_WhenSettingValueCloseToZero_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            ProgressViewModel testSubject = new ProgressViewModel();

            // Act
            Action act = () => testSubject.Value = 0.0 - double.Epsilon;

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Fact]
        
        public void ValueProperty_WhenSettingBiggerThanOne_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            ProgressViewModel testSubject = new ProgressViewModel();

            // Act
            Action act = () => testSubject.Value = 1.00001;

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ProgressViewModel_SetUpperBoundLimitedValue()
        {
            // Arrange
            ProgressViewModel testSubject = new ProgressViewModel();

            // Sanity
            testSubject.Value.Should().BeApproximately(0, double.Epsilon, "Default value expected");

            // Act
            Action act1 = () => testSubject.SetUpperBoundLimitedValue(double.NegativeInfinity);
            Action act2 = () => testSubject.SetUpperBoundLimitedValue(double.PositiveInfinity);
            Action act3 = () => testSubject.SetUpperBoundLimitedValue(0 - double.Epsilon);
            Action act4 = () => testSubject.SetUpperBoundLimitedValue(1.0 + ProgressViewModel.UpperBoundMarginalErrorSupport + ProgressViewModel.UpperBoundMarginalErrorSupport);

            // Assert
            act1.ShouldThrow<ArgumentOutOfRangeException>();
            act2.ShouldThrow<ArgumentOutOfRangeException>();
            act3.ShouldThrow<ArgumentOutOfRangeException>();
            act4.ShouldThrow<ArgumentOutOfRangeException>();

            // Sanity
            testSubject.Value.Should().Be(0.0, "Erroneous cases should not change the default value");

            // NaN supported
            testSubject.SetUpperBoundLimitedValue(double.NaN);
            double.IsNaN(testSubject.Value).Should().BeTrue();

            // Zero in range
            testSubject.SetUpperBoundLimitedValue(0);
            testSubject.Value.Should().Be(0.0);

            // One is in range
            testSubject.SetUpperBoundLimitedValue(1);
            testSubject.Value.Should().Be(1.0);

            // Anything between zero and one is in range
            Random r = new Random();
            double val = r.NextDouble();
            testSubject.SetUpperBoundLimitedValue(val);
            val.Should().Be(testSubject.Value);

            // More than one (i.e floating point summation errors) will become one
            testSubject.SetUpperBoundLimitedValue(1.0 + ProgressViewModel.UpperBoundMarginalErrorSupport);
            testSubject.Value.Should().Be(1.0);
        }
    }
}
