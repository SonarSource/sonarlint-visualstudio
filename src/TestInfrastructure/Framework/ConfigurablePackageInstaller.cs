/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting; using FluentAssertions;
using NuGet;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurablePackageInstaller : IVsPackageInstaller
    {
        private readonly bool simulateInstallerException;
        private readonly Dictionary<Project, IList<PackageName>> installedPackages = new Dictionary<Project, IList<PackageName>>();

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
        public HashSet<NuGet.PackageName> ExpectedPackages
        {
            get;
        } = new HashSet<PackageName>();

        public void AssertInstalledPackages(Project project, IEnumerable<PackageName> expectedPackages)
        {
            IList<PackageName> packages = new List<PackageName>();
            this.installedPackages.TryGetValue(project, out packages).Should().BeTrue("Expecting installed packages for project {0}", project.FileName);

            var expected = expectedPackages.ToArray();
            var actual = packages.ToArray();

            actual.Length.Should().Be(expected.Length, "Different number of packages.");

            for (int i = 0; i < expected.Length; i++)
            {
                expected[i].Equals(actual[i]).Should().BeTrue($"Packages are different at index {i}.");
            }
        }

        public void AssertNoInstalledPackages(Project project)
        {
            this.installedPackages.ContainsKey(project).Should().BeFalse("Not expecting any installed packages for project {0}", project.FileName);
        }

        public Action<Project> InstallPackageAction
        {
            get;
            set;
        }
        #endregion

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
        #endregion
    }
}
