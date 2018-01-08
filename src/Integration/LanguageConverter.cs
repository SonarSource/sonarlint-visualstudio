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
