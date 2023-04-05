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

namespace SonarLint.VisualStudio.Core
{
    public static class RegexConstants
    {
        /// <summary>
        /// Standard timeout to use for regular expressions
        /// </summary>
        /// <remarks>
        /// Security hotspot S6444 requires a timeout for regular expressions to mitigate denial of service attacks.
        /// See https://rules.sonarsource.com/csharp/RSPEC-6444
        /// The risk and impact for SLVS are both we - in general, we control the input to the regular expression, and
        /// the IDE isn't a public service.
        /// However, avoiding the issue is straightforward by adding a (fairly arbitrary) timeout.
        /// </remarks>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(300);
    }
}
