//-----------------------------------------------------------------------
// <copyright file="ConfigurablePackageInstaller.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurablePackageInstaller : IVsPackageInstaller
    {
        private bool simulateInstallerException;
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
            Assert.IsTrue(this.installedPackages.TryGetValue(project, out packages), "Expecting installed packages for project {0}", project.FileName);

            var expected = expectedPackages.ToArray();
            var actual = packages.ToArray();

            Assert.AreEqual(expected.Length, actual.Length, "Different number of packages.");

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(expected[i].Equals(actual[i]), $"Packages are different at index {i}.");
            }
        }

        public void AssertNoInstalledPackages(Project project)
        {
            Assert.IsFalse(this.installedPackages.ContainsKey(project), "Not expecting any installed packages for project {0}", project.FileName);
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

            Assert.IsNull(source, "Not expecting source, should resolve by itself");
            Assert.IsNotNull(project, "Expecting a project");
            Assert.IsFalse(ignoreDependencies, "Should be complete install");
            Assert.IsTrue(this.ExpectedPackages.Any(x => x.Equals(package)), $"Unexpected package {packageId}");

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
            Assert.IsFalse(packages.Contains(newEntry), "The same package was attempted to be installed twice. Id:{0}, version: {1}", packageId, version);
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
