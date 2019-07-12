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
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.Connection.UI
{
    /// <summary>
    /// Converter to validate an organization key. Used in the organization selection window
    /// to enabled/disable the OK button automatically as the text changes.
    /// </summary>
    /// <remarks>We're just checking that there is some non-whitespace content. We're not trying to
    /// doing comprehensive validation as the format of the keys might change on the server side.</remarks>
    public class IsValidOrganisationKeyConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Boolean))
            {
                throw new ArgumentException("Invalid target type", nameof(targetType));
            }

            if (value is null) // can't check the type if null
            {
                return false;
            }

            if (!(value is string))
            {
                throw new ArgumentException("Invalid input", nameof(value));
            }

            var orgKey = (string)value;
            return !string.IsNullOrEmpty(GetTrimmedKey(orgKey));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static string GetTrimmedKey(string input) =>
            input?.Trim();
    }
}
