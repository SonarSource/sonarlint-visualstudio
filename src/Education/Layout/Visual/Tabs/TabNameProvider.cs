/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
//
// using System;
//
// namespace SonarLint.VisualStudio.Education.Layout.Visual.Tabs
// {
//     /// <summary>
//     /// Provides WPF element names in specific format that can be parsed back
//     /// </summary>
//     internal static class TabNameProvider
//     {
//         private const string Separator = "__";
//         private static readonly string[] SplitArray = new[] { Separator };
//
//         public static string GetTabButtonName(string tabGroup, string tabName) =>
//             GetName(tabGroup, tabName, "Button");
//
//         public static string GetTabSectionName(string tabGroup, string tabName) =>
//             GetName(tabGroup, tabName, "Section");
//
//         public static (string TabGroup, string TabName) GetTabIdentifier(string name)
//         {
//             if (name == null)
//             {
//                 throw new ArgumentNullException(nameof(name));
//             }
//
//             var components = name.Split(SplitArray, StringSplitOptions.None);
//
//             if (components.Length != 3)
//             {
//                 throw new ArgumentException("Incorrect format");
//             }
//
//             return (components[0], components[1]);
//         }
//
//         private static string GetName(string tabGroup, string tabName, string itemType)
//         {
//             if (tabGroup == null)
//             {
//                 throw new ArgumentNullException(nameof(tabGroup));
//             }
//
//             if (tabName == null)
//             {
//                 throw new ArgumentNullException(nameof(tabName));
//             }
//
//             if (tabGroup.Contains(Separator))
//             {
//                 throw new ArgumentException($"Parameter contains illegal sequence: {Separator}", nameof(tabGroup));
//             }
//
//             if (tabName.Contains(Separator))
//             {
//                 throw new ArgumentException($"Parameter contains illegal sequence: {Separator}", nameof(tabName));
//             }
//
//
//             return string.Join(Separator, tabGroup, tabName, itemType);
//         }
//     }
// }
