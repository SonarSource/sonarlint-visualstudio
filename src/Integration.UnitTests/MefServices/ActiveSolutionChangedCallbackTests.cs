/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.MefServices;

namespace SonarLint.VisualStudio.Integration.UnitTests.MefServices
{
    [TestClass]
    public class ActiveSolutionChangedCallbackTests
    {
        [TestMethod]
        public void MefCtor_CheckExports()
        {
            var batch = new CompositionBatch();

            var callbacks = new[]
            {
                new Mock<Action>(),
                new Mock<Action>(),
                new Mock<Action>()
            };

            foreach (var callback in callbacks)
            {
                batch.AddExport(MefTestHelpers.CreateExport<Action>(callback.Object, ActiveSolutionChangedCallback.CallbackContractName));
            }

            var activeSolutionTracker = SetupActiveSolutionTracker();
            batch.AddExport(MefTestHelpers.CreateExport<IActiveSolutionTracker>(activeSolutionTracker.Object));

            var testSubjectImport = new SingleObjectImporter<IActiveSolutionChangedCallback>();
            batch.AddPart(testSubjectImport);

            using var catalog = new TypeCatalog(typeof(ActiveSolutionChangedCallback));
            using var container = new CompositionContainer(catalog);
            container.Compose(batch);

            testSubjectImport.Import.Should().NotBeNull();
            activeSolutionTracker.VerifyAdd(x=> x.ActiveSolutionChanged += It.IsAny<EventHandler<ActiveSolutionChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void OnActiveSolutionChanged_NoCallbacks_NoException()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var callbacks = Enumerable.Empty<Action>();
            
            new ActiveSolutionChangedCallback(activeSolutionTracker.Object, callbacks);

            Action act = () => activeSolutionTracker.Raise(x=> x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(true));
            act.Should().NotThrow();
        }

        [TestMethod]
        public void OnActiveSolutionChanged_HasCallbacks_CallbacksCalled()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var callbacks = new[] {new Mock<Action>(), new Mock<Action>()};

            new ActiveSolutionChangedCallback(activeSolutionTracker.Object, callbacks.Select(x=> x.Object));

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(true));

            foreach (var callback in callbacks)
            {
                callback.Verify(x => x(), Times.Once);
            }
        }

        [TestMethod]
        public void Dispose_UnsubscribesFromActiveSolutionChangedEvent()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var callback = new Mock<Action>();

            var testSubject = new ActiveSolutionChangedCallback(activeSolutionTracker.Object, new[] {callback.Object});
            
            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(true));
            callback.Verify(x => x(), Times.Once);
            callback.Reset();

            testSubject.Dispose();

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(true));
            callback.Verify(x => x(), Times.Never);
            callback.VerifyNoOtherCalls();
        }

        private static Mock<IActiveSolutionTracker> SetupActiveSolutionTracker()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionTracker>();
            activeSolutionTracker.SetupAdd(x => x.ActiveSolutionChanged += null);

            return activeSolutionTracker;
        }
    }
}
