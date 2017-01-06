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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SonarLint.VisualStudio.Integration.WPF
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void RaisePropertyChanged([CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void SetAndRaisePropertyChanged<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
        {
            bool equal;

            if (value is IEquatable<T>)
            {
                equal = ((IEquatable<T>)value).Equals(field);
            }
            else if (typeof(T).IsSubclassOf(typeof(Enum)))
            {
                equal = Enum.Equals(value, field);
            }
            else
            {
                equal = ReferenceEquals(value, field);
            }

            if (!equal)
            {
                field = value;
                RaisePropertyChanged(propertyName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void SetAndRaisePropertyChanged(ref string field, string value, [CallerMemberName]string propertyName = null)
        {
            if (!string.Equals(field, value, StringComparison.Ordinal))
            {
                field = value;
                RaisePropertyChanged(propertyName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected bool SetAndRaisePropertyChanged(ref bool propertyDataField, bool value, [CallerMemberName]string propertyName = null)
        {
            if (propertyDataField != value)
            {
                propertyDataField = value;
                RaisePropertyChanged(propertyName);
                return true;
            }

            return false;
        }
    }
}
