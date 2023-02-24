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

using System;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    partial class IssuesSnapshot
    {
        internal static class SuppressedStateColumnHelper
        {
            // Comment in MS code for Microsoft.VisualStudio.Shell.TableManager.Boxes
            // "It is better, for performance reasons, to return values that have been
            // boxed when returning though an out <see cref="T:System.Object" />."
            private static readonly object BoxedActive;
            private static readonly object BoxedSuppressed;

            /// <summary>
            /// HACK: get a reference to the VS SuppressionState enum
            /// </summary>
            /// <remarks>
            /// The VS SuppressionState enum is publicly available in VS2019. However, it isn't available
            /// in the VS2017 version of the SDK we are referencing, or in the VS2019 SDK versions that
            /// are compatible with VS2019.3.
            /// This hack works round a build failure, pending an update of the SDK version.
            /// For VS2022, we could use the VS constants in Microsoft.VisualStudio.Shell.TableManager.Boxes.
            /// However, we're using the same code for both VS2019 and VS2022 to avoid conditional compilation.
            /// </remarks>

            static SuppressedStateColumnHelper()
            {
                try
                {
                    var suppressionStateEnumType = typeof(StandardTableKeyNames).Assembly.GetType("Microsoft.VisualStudio.Shell.TableManager.SuppressionState");

                    BoxedActive = Enum.Parse(suppressionStateEnumType, "Active");
                    BoxedSuppressed = Enum.Parse(suppressionStateEnumType, "Suppressed");
                }
                catch (Exception ex)
                {
                    // Squash - can't usefully do anything else.
                    // If this happens at runtime, any calls to SuppressedStateColumnHelper will
                    // return null - it won't cause another exception.
                    System.Diagnostics.Debug.Write("Error fetching SuppressionState enum: " + ex);
                }
            }

            public static object GetValue(IAnalysisIssueVisualization issueViz)
                => issueViz.IsSuppressed ? BoxedSuppressed : BoxedActive;
        }
    }
}
