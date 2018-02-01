/*
 * SonarLint for Visual Studio
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

using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using NuGet;
using NuGet.VisualStudio;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurablePackageInstaller : IVsPackageInstaller
    {
        private readonly bool simulateInstallerException;
        private readonly Dictionary<Project, IList<PackageName>> installedPackages =
            new Dictionary<Project, IList<PackageName>>();

        public ConfigurablePackageInstaller(bool simulateInstallerException = false)
            : this(Enumerable.Empty<PackageName>(), simulateInstallerException)
        {
        }

        public ConfigurablePackageInstaller(IEnumerable<PackageName> allowedPackages, bool simulateInstallerException = false)
        {
            this.simulateInstallerException = simulateInstallerException;
            this.ExpectedPackages.UnionWith(allowedPackages);
        }

        #region Test helpers

        public HashSet<PackageName> ExpectedPackages
        {
            get;
        } = new HashSet<PackageName>();

        public void AssertInstalledPackages(Project project, IEnumerable<PackageName> expectedPackages)
        {
            IList<PackageName> packages = new List<PackageName>();
            this.installedPackages.TryGetValue(project, out packages).Should().BeTrue("Expecting installed packages for project {0}", project.FileName);

            var expected = expectedPackages.ToArray();
            var actual = packages.ToArray();

            actual.Should().HaveSameCount(expected);

            for (int i = 0; i < expected.Length; i++)
            {
                expected[i].Equals(actual[i]).Should().BeTrue($"Packages are different at index {i}.");
            }
        }

        public void AssertNoInstalledPackages(Project project)
        {
            this.installedPackages.Should().NotContainKey(project, "Not expecting any installed packages for project {0}", project.FileName);
        }

        public Action<Project> InstallPackageAction
        {
            get;
            set;
        }

        #endregion Test helpers

        #region IVsPackageInstaller

        void IVsPackageInstaller.InstallPackage(string source, Project project, string packageId, string version, bool ignoreDependencies)
        {
            var package = new PackageName(packageId, new SemanticVersion(version));

            source.Should().BeNull("Not expecting source, should resolve by itself");
            project.Should().NotBeNull("Expecting a project");
            ignoreDependencies.Should().BeFalse("Should be complete install");
            this.ExpectedPackages.Any(x => x.Equals(package)).Should().BeTrue($"Unexpected package {packageId}");

            this.InstallPackageAction?.Invoke(project);

            if (this.simulateInstallerException)
            {
                throw new Exception("Oops");
            }

            IList<PackageName> packages = new List<PackageName>();
            if (!this.installedPackages.TryGetValue(project, out packages))
            {
                var packageList = new List<PackageName>();
                this.installedPackages[project] = packageList;
                packages = packageList;
            }

            var newEntry = new PackageName(packageId, new SemanticVersion(version));
            packages.Contains(newEntry).Should().BeFalse("The same package was attempted to be installed twice. Id:{0}, version: {1}", packageId, version);
            packages.Add(newEntry);
        }

        void IVsPackageInstaller.InstallPackage(string source, Project project, string packageId, Version version, bool ignoreDependencies)
        {
            throw new NotImplementedException();
        }

        void IVsPackageInstaller.InstallPackage(NuGet.IPackageRepository repository, Project project, string packageId, string version, bool ignoreDependencies, bool skipAssemblyReferences)
        {
            throw new NotImplementedException();
        }

        void IVsPackageInstaller.InstallPackagesFromRegistryRepository(string keyName, bool isPreUnzipped, bool skipAssemblyReferences, Project project, IDictionary<string, string> packageVersions)
        {
            throw new NotImplementedException();
        }

        void IVsPackageInstaller.InstallPackagesFromRegistryRepository(string keyName, bool isPreUnzipped, bool skipAssemblyReferences, bool ignoreDependencies, Project project, IDictionary<string, string> packageVersions)
        {
            throw new NotImplementedException();
        }

        void IVsPackageInstaller.InstallPackagesFromVSExtensionRepository(string extensionId, bool isPreUnzipped, bool skipAssemblyReferences, Project project, IDictionary<string, string> packageVersions)
        {
            throw new NotImplementedException();
        }

        void IVsPackageInstaller.InstallPackagesFromVSExtensionRepository(string extensionId, bool isPreUnzipped, bool skipAssemblyReferences, bool ignoreDependencies, Project project, IDictionary<string, string> packageVersions)
        {
            throw new NotImplementedException();
        }

        #endregion IVsPackageInstaller
    }
}