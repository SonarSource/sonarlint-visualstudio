/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using System.IO;

namespace SonarLint.VisualStudio.E2E
{
    internal sealed class E2ESettings
    {
        private E2ESettings(string vsixPath, string resultsDirectory, string visualStudioInstallPath, Version expectedVisualStudioVersion, string expectedVisualStudioProduct)
        {
            VsixPath = vsixPath;
            ResultsDirectory = resultsDirectory;
            VisualStudioInstallPath = visualStudioInstallPath;
            ExpectedVisualStudioVersion = expectedVisualStudioVersion;
            ExpectedVisualStudioProduct = expectedVisualStudioProduct;
        }

        public string VsixPath { get; }
        public string ResultsDirectory { get; }
        public string VisualStudioInstallPath { get; }
        public Version ExpectedVisualStudioVersion { get; }
        public string ExpectedVisualStudioProduct { get; }

        public static E2ESettings FromEnvironment()
        {
            var vsixPath = Path.GetFullPath(Required("E2E_VSIX_PATH", "a built VSIX"));
            if (!string.Equals(Path.GetExtension(vsixPath), ".vsix", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("E2E_VSIX_PATH must point to a built .vsix package.");
            }
            var resultsDirectory = Path.GetFullPath(Required("E2E_RESULTS_DIRECTORY", "a new directory"));
            if (Directory.Exists(resultsDirectory))
            {
                throw new ArgumentException("Use a new E2E_RESULTS_DIRECTORY for each run.");
            }
            var installPath = Path.GetFullPath(Required("E2E_VS_INSTALL_PATH", "the full Visual Studio installation to test"));
            if (!Version.TryParse(Required("E2E_EXPECTED_VS_VERSION", "the pinned Visual Studio build"), out var expectedVersion))
            {
                throw new ArgumentException("E2E_EXPECTED_VS_VERSION must be a four-part Visual Studio build number.");
            }
            var expectedProduct = Required("E2E_EXPECTED_VS_PRODUCT", "the Visual Studio product ID");
            return new E2ESettings(vsixPath, resultsDirectory, installPath, expectedVersion, expectedProduct);
        }

        private static string Required(string name, string description)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Set {name} to {description}.");
            }
            return value;
        }
    }
}
