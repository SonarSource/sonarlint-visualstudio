/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

namespace CFamilyJarPreProcessor
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();

            if (args.Length != 2)
            {
                logger.LogError("Expected parameters: [plugin download url] [full path to destination directory]");
                return -1;
            }

            var downloadUrl = args[0];
            var destinationDir = args[1];

            var preprocessor = new Preprocessor(logger);
            
            preprocessor.Execute(downloadUrl, destinationDir);

            return 0;
        }
    }
}
