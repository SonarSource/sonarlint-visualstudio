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

using System.Collections.Generic;
using System.Threading;
using SonarLint.VisualStudio.Core.Binding;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Binding.UnitTests
{
    [TestClass]
    public class UnintrusiveBindingControllerTests
    {
        private static readonly BoundSonarQubeProject AnyBoundProject = new BoundSonarQubeProject(new Uri("http://localhost:9000"), "any-key", "any-name");

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<UnintrusiveBindingController>();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<UnintrusiveBindingController, IUnintrusiveBindingController>(
                MefTestHelpers.CreateExport<IBindingProcessFactory>());
        }

        [TestMethod]
        public async Task BindAsnyc_GetsBindingProcessFromFactory()
        {
            var bindingProcessFactory = CreateBindingProcessFactory();

            var testSubject = CreateTestSubject(bindingProcessFactory: bindingProcessFactory.Object);
            await testSubject.BindAsync(AnyBoundProject, null, CancellationToken.None);
            
            var args = bindingProcessFactory.Invocations[0].Arguments[0] as BindCommandArgs;

            args.ProjectName.Should().Be(AnyBoundProject.ProjectName);
            args.ProjectKey.Should().Be(AnyBoundProject.ProjectKey);
            args.Connection.ServerUri.Should().Be(AnyBoundProject.ServerUri);

            bindingProcessFactory.Verify(x => x.Create(It.IsAny<BindCommandArgs>()), Times.Once);
        }

        [TestMethod]
        public async Task BindAsnyc_CallsBindingProcessInOrder()
        {
            var calls = new List<string>();
            var cancellationToken = CancellationToken.None;

            var bindingProcess = new Mock<IBindingProcess>();
            bindingProcess.Setup(x => x.DownloadQualityProfileAsync(null, cancellationToken)).Callback(() => calls.Add("DownloadQualityProfiles"));
            bindingProcess.Setup(x => x.SaveServerExclusionsAsync(cancellationToken)).Callback(() => calls.Add("SaveServerExclusionsAsync"));

            var testSubject = CreateTestSubject(bindingProcessFactory: CreateBindingProcessFactory(bindingProcess.Object).Object);
            await testSubject.BindAsync(AnyBoundProject, null, cancellationToken);

            calls.Should().ContainInOrder("DownloadQualityProfiles", "SaveServerExclusionsAsync");
        }

        private UnintrusiveBindingController CreateTestSubject(IBindingProcessFactory bindingProcessFactory = null)
        {
            bindingProcessFactory ??= CreateBindingProcessFactory().Object;

            var testSubject = new UnintrusiveBindingController(bindingProcessFactory);

            return testSubject;
        }

        private Mock<IBindingProcessFactory> CreateBindingProcessFactory(IBindingProcess bindingProcess = null)
        {
            bindingProcess ??= Mock.Of<IBindingProcess>();

            var bindingProcessFactory = new Mock<IBindingProcessFactory>();
            bindingProcessFactory.Setup(x => x.Create(It.IsAny<BindCommandArgs>())).Returns(bindingProcess);

            return bindingProcessFactory;
        }
    }
}
