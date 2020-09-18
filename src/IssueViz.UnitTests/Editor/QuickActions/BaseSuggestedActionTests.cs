/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions
{
    [TestClass]
    public class BaseSuggestedActionTests
    {
        [TestMethod]
        public async Task DefaultProperties()
        {
            var testBaseSuggestedAction = new TestBaseSuggestedAction();

            testBaseSuggestedAction.HasActionSets.Should().BeFalse();
            testBaseSuggestedAction.HasPreview.Should().BeFalse();
            testBaseSuggestedAction.IconAutomationText.Should().BeNull();
            testBaseSuggestedAction.IconMoniker.Should().BeEquivalentTo(new ImageMoniker());
            testBaseSuggestedAction.InputGestureText.Should().BeNull();

            var previewAsync = await testBaseSuggestedAction.GetPreviewAsync(CancellationToken.None);
            previewAsync.Should().BeNull();

            var actionSets = await testBaseSuggestedAction.GetActionSetsAsync(CancellationToken.None);
            actionSets.Should().BeEmpty();

            testBaseSuggestedAction.TryGetTelemetryId(out var guid).Should().BeFalse();
            guid.Should().BeEmpty();
        }

        [TestMethod]
        public void Dispose_CallsSubclassImplementation()
        {
            var testBaseSuggestedAction = new TestBaseSuggestedAction();
            testBaseSuggestedAction.DisposeCalled.Should().BeFalse();

            testBaseSuggestedAction.Dispose();

            testBaseSuggestedAction.DisposeCalled.Should().BeTrue();
        }

        private class TestBaseSuggestedAction : BaseSuggestedAction
        {
            internal bool DisposeCalled;

            public override string DisplayText => "Test";

            public override void Invoke(CancellationToken cancellationToken)
            {
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCalled = true;
                base.Dispose(disposing);
            }
        }
    }
}
