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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots
{
    /// <summary>
    /// Provides a mapping for hotspots to their review priority
    /// </summary>
    public interface IHotspotReviewPriorityProvider
    {
        /// <summary>
        /// Supported languages:
        /// <list type="bullet">
        /// <item><description>JavaScript</description></item>
        /// <item><description>TypeScript</description></item>
        /// <item><description>C++</description></item>
        /// <item><description>C</description></item>
        /// </list>
        /// </summary>
        /// <param name="hotspotKey">Hotspot Rule Key</param>
        /// <returns>Associated review priority or null if not mapped</returns>
        HotspotPriority? GetPriority(string hotspotKey);
    }

    [Export(typeof(IHotspotReviewPriorityProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class HotspotReviewPriorityProvider : IHotspotReviewPriorityProvider
    {
        // In order to save time and considering the fact that hotspots are no longer being updated, we decided to make a hardcoded list of priorities by rule key
        // See (https://github.com/SonarSource/sonarlint-visualstudio/issues/4532)
        private static readonly Dictionary<string, HotspotPriority> KeyToPriorityMap = new Dictionary<string, HotspotPriority>
        {
            { "cpp:S5443", HotspotPriority.Low },
            { "cpp:S2068", HotspotPriority.High },
            { "cpp:S5332", HotspotPriority.Low },
            { "cpp:S6069", HotspotPriority.High },
            { "cpp:S5042", HotspotPriority.Low },
            { "cpp:S5802", HotspotPriority.Low },
            { "cpp:S5801", HotspotPriority.High },
            { "cpp:S1313", HotspotPriority.Low },
            { "cpp:S5824", HotspotPriority.Low },
            { "cpp:S5815", HotspotPriority.High },
            { "cpp:S5816", HotspotPriority.High },
            { "cpp:S5813", HotspotPriority.High },
            { "cpp:S5814", HotspotPriority.High },
            { "cpp:S2612", HotspotPriority.Medium },
            { "cpp:S4790", HotspotPriority.Low },
            { "cpp:S2245", HotspotPriority.Medium },
            { "cpp:S5849", HotspotPriority.Medium },
            { "cpp:S5982", HotspotPriority.Low },

            { "c:S5802", HotspotPriority.Low },
            { "c:S5801", HotspotPriority.High },
            { "c:S1313", HotspotPriority.Low },
            { "c:S5824", HotspotPriority.Low },
            { "c:S5815", HotspotPriority.High },
            { "c:S5816", HotspotPriority.High },
            { "c:S5813", HotspotPriority.High },
            { "c:S5814", HotspotPriority.High },
            { "c:S2612", HotspotPriority.Medium },
            { "c:S5443", HotspotPriority.Low },
            { "c:S2068", HotspotPriority.High },
            { "c:S5332", HotspotPriority.Low },
            { "c:S4790", HotspotPriority.Low },
            { "c:S2245", HotspotPriority.Medium },
            { "c:S6069", HotspotPriority.High },
            { "c:S5042", HotspotPriority.Low },
            { "c:S5849", HotspotPriority.Medium },
            { "c:S5982", HotspotPriority.Low },
        };

        public HotspotPriority? GetPriority(string hotspotKey) =>
            KeyToPriorityMap.TryGetValue(hotspotKey ?? throw new ArgumentNullException(nameof(hotspotKey)), out var priority) ? priority : (HotspotPriority?)null;
    }
}
