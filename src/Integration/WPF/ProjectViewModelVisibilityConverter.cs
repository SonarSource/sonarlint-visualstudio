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
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SonarLint.VisualStudio.Integration.WPF
{
    internal sealed class ProjectViewModelVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 4)
            {
                return DependencyProperty.UnsetValue;
            }

            var projectName = values[0] as string;
            var showAllProjects = values[1] as bool?;
            var isBound = values[2] as bool?;
            var filterText = values[3] as string;

            if (projectName == null || showAllProjects == null || isBound == null || filterText == null)
            {
                return DependencyProperty.UnsetValue;
            }

            if (showAllProjects.Value)
            {
                return projectName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) != -1
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            else
            {
                return isBound.Value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($"{nameof(ProjectViewModelVisibilityConverter)} does not support ConvertBack method.");
        }
    }
}
