/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace SonarLint.VisualStudio.Core.WPF;

/// <summary>
/// Converts an enum value that uses PascalCase to a string with words separated by spaces.
/// If the value is not an Enum, or if it contains only upper case letters, returns the string representation of the received value.
/// </summary>
[ValueConversion(typeof(Enum), typeof(string))]
public class PascalCaseEnumToSpacedStringConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        var enumValue = value?.ToString();
        return value is not Enum ? enumValue : SeparateWordsBySpace(enumValue);
    }

    private static string SeparateWordsBySpace(string text)
    {
        if (text.All(char.IsUpper))
        {
            return text;
        }
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(text[0]);
        for (var i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]))
            {
                stringBuilder.Append(' ');
            }
            stringBuilder.Append(text[i]);
        }
        return stringBuilder.ToString();
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) =>
        throw new NotImplementedException();
}
