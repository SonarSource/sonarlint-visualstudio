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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.MefServices;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.MefServices;

[TestClass]
public class ProjectDocumentsEventsListenerTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<ProjectDocumentsEventsListener, IProjectDocumentsEventsListener>(
            MefTestHelpers.CreateExport<SVsServiceProvider>(),
            MefTestHelpers.CreateExport<IFileTracker>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void Initialize_CallsAdviseTrackProjectDocumentsEvents()
    {
        var trackProjectDocuments2 = Substitute.For<IVsTrackProjectDocuments2>();
        var testSubject = CreateTestSubject(trackProjectDocuments2: trackProjectDocuments2);

        testSubject.Initialize();

        trackProjectDocuments2.Received()
            .AdviseTrackProjectDocumentsEvents(Arg.Any<IVsTrackProjectDocumentsEvents2>(), out Arg.Any<uint>());
    }

    [TestMethod]
    public void OnAfterAddFilesEx_NotifiesFileTracker()
    {
        var fileTracker = Substitute.For<IFileTracker>();
        var testSubject = CreateTestSubject(fileTracker: fileTracker);

        testSubject.OnAfterAddFilesEx(1, 1, [null], [0], ["C:\\Users\\test\\TestProject\\AFile.cs"],
            [VSADDFILEFLAGS.VSADDFILEFLAGS_NoFlags]);

        fileTracker.Received().AddFiles("C:\\Users\\test\\TestProject\\AFile.cs");
    }

    [TestMethod]
    public void OnAfterRemoveFiles_NotifiesFileTracker()
    {
        var fileTracker = Substitute.For<IFileTracker>();
        var testSubject = CreateTestSubject(fileTracker: fileTracker);

        testSubject.OnAfterRemoveFiles(1, 1, [null], [0], ["C:\\Users\\test\\TestProject\\AFile.cs"],
            [VSREMOVEFILEFLAGS.VSREMOVEFILEFLAGS_NoFlags]);

        fileTracker.Received().RemoveFiles("C:\\Users\\test\\TestProject\\AFile.cs");
    }

    [TestMethod]
    public void OnAfterRenameFiles_NotifiesFileTracker()
    {
        var fileTracker = Substitute.For<IFileTracker>();
        var testSubject = CreateTestSubject(fileTracker: fileTracker);

        testSubject.OnAfterRenameFiles(1, 1, [null], [0],
            ["C:\\Users\\test\\TestProject\\AFile.cs"],
            ["C:\\Users\\test\\TestProject\\ARenamedFile.cs"],
            [VSRENAMEFILEFLAGS.VSRENAMEFILEFLAGS_NoFlags]);

        fileTracker.Received().RenameFiles(
            Arg.Is<string[]>(strings => strings[0] == "C:\\Users\\test\\TestProject\\AFile.cs"),
            Arg.Is<string[]>(strings => strings[0] == "C:\\Users\\test\\TestProject\\ARenamedFile.cs"));
    }

    [TestMethod]
    public void OnQueryAddFiles_DoesNothing()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.OnQueryAddFiles(null, 1, ["C:\\Users\\test\\TestProject\\AFile.cs"], [], [], []);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void OnAfterAddDirectoriesEx_DoesNothing()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.OnAfterAddDirectoriesEx(1, 1, [], [], [], []);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void OnAfterRemoveDirectories_DoesNothing()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.OnAfterRemoveDirectories(1, 1, [], [], [], []);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void OnQueryRenameFiles_DoesNothing()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.OnQueryRenameFiles(null, 1, [], [], [], [], []);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void OnQueryRenameDirectories_DoesNothing()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.OnQueryRenameDirectories(null, 1, [], [], [], [], []);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void OnAfterRenameDirectories_DoesNothing()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.OnAfterRenameDirectories(1, 1, [], [], [], [], []);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void OnQueryAddDirectories_DoesNothing()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.OnQueryAddDirectories(null, 1, [], [], [], []);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void OnQueryRemoveFiles_DoesNothing()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.OnQueryRemoveFiles(null, 1, [], [], [], []);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void OnQueryRemoveDirectories_DoesNothing()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.OnQueryRemoveDirectories(null, 1, [], [], [], []);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void OnAfterSccStatusChanged_DoesNothing()
    {
        var testSubject = CreateTestSubject();

        var result = testSubject.OnAfterSccStatusChanged(1, 1, [], [], [], []);

        result.Should().Be(VSConstants.S_OK);
    }

    [TestMethod]
    public void Dispose_UnadviseTrackProjectDocumentsEvents()
    {
        var trackProjectDocuments2 = Substitute.For<IVsTrackProjectDocuments2>();
        var testSubject = CreateTestSubject(trackProjectDocuments2: trackProjectDocuments2);

        testSubject.Dispose();

        trackProjectDocuments2.Received().UnadviseTrackProjectDocumentsEvents(Arg.Any<uint>());
    }

    private static ProjectDocumentsEventsListener CreateTestSubject(IFileTracker fileTracker = null,
        IVsTrackProjectDocuments2 trackProjectDocuments2 = null)
    {
        trackProjectDocuments2 ??= Substitute.For<IVsTrackProjectDocuments2>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(SVsTrackProjectDocuments)).Returns(trackProjectDocuments2);
        fileTracker ??= Substitute.For<IFileTracker>();
        var threadHandling = Substitute.For<IThreadHandling>();
        return new ProjectDocumentsEventsListener(serviceProvider, fileTracker, threadHandling);
    }
}
