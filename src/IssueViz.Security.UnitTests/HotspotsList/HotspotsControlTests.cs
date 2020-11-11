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

using System.Windows;
using System.Windows.Controls;
using FluentAssertions;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotsControlTests
    {
        [TestMethod]
        public void Ctor_HotspotsListIsWpfTableControl()
        {
            var uiControl = Mock.Of<FrameworkElement>();
            var wpfTableControl = new Mock<IWpfTableControl>();
            wpfTableControl.Setup(x => x.Control).Returns(uiControl);

            var testSubject = CreateTestSubject(wpfTableControl);

            testSubject.hotspotsList.Child.Should().NotBeNull();
            testSubject.hotspotsList.Child.Should().Be(uiControl);
        }

        [TestMethod]
        public void Ctor_WpfTableControlConfiguration_SelectionSet()
        {
            var uiControl = Mock.Of<FrameworkElement>();
            var wpfTableControl = new Mock<IWpfTableControl>();
            wpfTableControl.Setup(x => x.Control).Returns(uiControl);

            CreateTestSubject(wpfTableControl);

            wpfTableControl.VerifySet(x => x.SelectionMode = SelectionMode.Single);
        }

        [TestMethod]
        public void Dispose_WpfTableControlIsDisposed()
        {
            var wpfTableControl = new Mock<IWpfTableControl>();
            var testSubject = CreateTestSubject(wpfTableControl);

            wpfTableControl.Verify(x=> x.Dispose(), Times.Never);

            testSubject.Dispose();

            wpfTableControl.Verify(x => x.Dispose(), Times.Once);
        }

        private static Security.HotspotsList.HotspotsControl CreateTestSubject(Mock<IWpfTableControl> wpfTableControl)
        {
            var tableManager = Mock.Of<ITableManager>();

            var tableManagerProviderMock = new Mock<ITableManagerProvider>();
            tableManagerProviderMock
                .Setup(x => x.GetTableManager(HotspotsTableConstants.TableManagerIdentifier))
                .Returns(tableManager);

            var wpfTableControlProviderMock = new Mock<IWpfTableControlProvider>();

            wpfTableControlProviderMock
                .Setup(x => x.CreateControl(
                    tableManager,
                    true,
                    HotspotsTableColumns.InitialStates,
                    HotspotsTableColumns.Names))
                .Returns(wpfTableControl.Object);

            var testSubject = new Security.HotspotsList.HotspotsControl(tableManagerProviderMock.Object, wpfTableControlProviderMock.Object);

            return testSubject;
        }
    }
}
