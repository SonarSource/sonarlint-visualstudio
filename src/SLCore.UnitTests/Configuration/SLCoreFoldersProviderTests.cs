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

using System.IO;
using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Configuration
{
    [TestClass]
    public class SLCoreFoldersProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SLCoreFoldersProvider, ISLCoreFoldersProvider>();
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SLCoreFoldersProvider>();
        }

        [TestMethod]
        public void GetWorkFolders_ShouldReturnFolders()
        {
            var expectedWorkDir = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "SLVS_SLOOP\\workDir");
            var expectedStorageRoot = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "SLVS_SLOOP\\storageRoot");

            var testSubject = new SLCoreFoldersProvider();

            var result = testSubject.GetWorkFolders();

            result.WorkDir.Should().Be(expectedWorkDir);
            result.StorageRoot.Should().Be(expectedStorageRoot);
            result.SonarlintUserHome.Should().BeNull();
        }
    }
}
