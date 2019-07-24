/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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

// Note: copied from the S4MSB
// https://github.com/SonarSource/sonar-scanner-msbuild/blob/b28878e21cbdda9aca6bd08d90c3364cca882861/src/SonarScanner.MSBuild.Common/Interfaces/IProcessRunner.cs#L23

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    public interface IProcessRunner
    {
        bool Execute(ProcessRunnerArguments runnerArgs);
    }
}
