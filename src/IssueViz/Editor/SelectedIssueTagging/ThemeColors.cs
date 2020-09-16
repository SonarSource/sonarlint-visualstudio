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

using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging
{
    internal static class ThemeColors
    {
        internal static readonly IThemeColors LightTheme = new LightThemeColors();
        internal static readonly IThemeColors DarkTheme = new DarkThemeColors();

        public static bool IsLightTheme(Brush textBrush)
        {
            var isLightTheme = true;

            if (textBrush is SolidColorBrush solidColorBrush)
            {
                var isDarkText = solidColorBrush.Color.IsDark();
                isLightTheme = isDarkText;
            }

            return isLightTheme;
        }

        public static IThemeColors BasedOnText(Brush textBrush)
        {
            return IsLightTheme(textBrush) ? LightTheme : DarkTheme;
        }
    }

    internal class LightThemeColors : IThemeColors
    {
        public Brush BackgroundBrush { get; } = new SolidColorBrush(Colors.LightPink);
        public Brush BorderBrush { get; } = new SolidColorBrush(Colors.LightCoral);
        public Brush Highlight { get; } = new SolidColorBrush(Colors.LightPink) { Opacity = 0.5 };
    }

    internal class DarkThemeColors : IThemeColors
    {
        public Brush BackgroundBrush { get; } = new SolidColorBrush(Colors.DarkRed);
        public Brush BorderBrush { get; } = new SolidColorBrush(Colors.Red);
        public Brush Highlight { get; } = new SolidColorBrush(Colors.DarkRed) { Opacity = 0.5 };
    }

    internal interface IThemeColors
    {
        Brush BackgroundBrush { get; }
        Brush BorderBrush { get; }
        Brush Highlight { get; }
    }
}
