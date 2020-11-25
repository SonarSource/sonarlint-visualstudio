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

using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    public static class ILoggerExtensions
    {
        private static bool shouldLogDebug;

        static ILoggerExtensions() => Initialize(new EnvironmentSettings());

        internal /* for testing*/ static void Initialize(IEnvironmentSettings settings)
        {
            shouldLogDebug = settings.LogDebugMessages();
        }

        /// <summary>
        /// Logs messages only when an environment variable is set. This is temporary
        /// solution for not having log verbosity setting.
        /// </summary>
        public static void LogDebug(this ILogger logger, string message, params object[] args)
        {
            if(shouldLogDebug)
            {
                var text = args.Length == 0 ? message : string.Format(message, args);
                logger.WriteLine("DEBUG: " + text);
            }
        }
    }
}
