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
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Enables conversion between <seealso cref="Language.Id"/> and <seealso cref="Language"/>.
    /// </summary>
    /// <remarks>
    /// When an <seealso cref="Language.Id"/> is provided which matches a <seealso cref="Language.KnownLanguages"/>,
    /// the corresponding known <seealso cref="Language"/> singleton is returned. Otherwise the <seealso cref="Language.Unknown"/>
    /// is returned.
    /// </remarks>
    internal class LanguageConverter : TypeConverter
    {
        #region Convert From

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var languageId = value as string;

            if (languageId != null)
            {
                return Language.KnownLanguages.FirstOrDefault(x => StringComparer.OrdinalIgnoreCase.Equals(x.Id, languageId))
                    ?? Language.Unknown;
            }

            Debug.Fail("Expected string input object");
            return Language.Unknown;
        }

        #endregion

        #region ConvertTo

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            Debug.Assert(value is Language, $"Expected {nameof(Language)} input object");
            Debug.Assert(destinationType == typeof(string), "Expected string destination type");

            return (value as Language)?.Id;
        }

        #endregion
    }
}
