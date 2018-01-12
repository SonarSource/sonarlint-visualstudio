/*
 * SonarQube Client
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

using System.Linq;

namespace SonarQube.Client.RequestGenerator
{
    public class ArgsParser
    {
        private readonly string[] args;
        private readonly string[] defaults = new[]
        {
                ".", // output path
                "http://localhost:9000", // sq url
                "admin", // user
                "admin", // pass
                "test1", // project name
                null, // organization name
                "Sonar Way" // quality profile
            };

        private int index;

        public ArgsParser(string[] args)
        {
            this.args = args;
            this.index = 0;
        }

        public string NextArg()
        {
            var arg = args.ElementAtOrDefault(index)
                ?? defaults.ElementAtOrDefault(index);
            index++;
            return arg;
        }
    }
}
