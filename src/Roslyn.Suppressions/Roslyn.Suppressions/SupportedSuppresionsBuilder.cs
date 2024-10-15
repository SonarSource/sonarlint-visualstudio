/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace SonarLint.VisualStudio.Roslyn.Suppressions
{
    /// <summary>
    /// Generated class that returns SupportedSuppressions for all Sonar C# and VB.NET rules
    /// </summary>
    internal sealed class SupportedSuppressionsBuilder
    {
        private static readonly Lazy<SupportedSuppressionsBuilder> lazy = new Lazy<SupportedSuppressionsBuilder>(() => new SupportedSuppressionsBuilder());

        public static SupportedSuppressionsBuilder Instance => lazy.Value;

        public ImmutableArray<SuppressionDescriptor> Descriptors { get; }

        private SupportedSuppressionsBuilder()
        {
            Descriptors = GetDescriptors();
        }

        private static ImmutableArray<SuppressionDescriptor> GetDescriptors()
        {
            // we declare all possible rules as a workaround since the roslyn analyzers are loaded dynamically 
            // this is a temporary solution as it can have an impact on performance
            var descriptors = Enumerable.Range(100, 9899).Select(id => CreateDescriptor($"S{id}"));
            return ImmutableArray.ToImmutableArray(descriptors);
        }

        private static SuppressionDescriptor CreateDescriptor(string diagId) =>
            new SuppressionDescriptor("X" + diagId, diagId, "Suppressed on the Sonar server");
    }
}
