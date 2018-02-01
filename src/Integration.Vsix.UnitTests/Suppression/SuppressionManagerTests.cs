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


namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    // TODO: AMAURY - Move (and adapt) tests to SonarAnalyzerManager
    //[TestClass]
    //public class SuppressionManagerTests
    //{
    //    private ConfigurableServiceProvider configurableServiceProvider;
    //    private Mock<IActiveSolutionBoundTracker> activeSolutionBoundTrackerMock;
    //    private Mock<ILogger> sonarLintOutputMock;

    //    [TestInitialize]
    //    public void TestsInitialize()
    //    {
    //        activeSolutionBoundTrackerMock = new Mock<IActiveSolutionBoundTracker>();
    //        sonarLintOutputMock = new Mock<ILogger>();

    //        var mefExport1 = MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(activeSolutionBoundTrackerMock.Object);
    //        var mefExport2 = MefTestHelpers.CreateExport<ILogger>(sonarLintOutputMock.Object);
    //        var mefModel = ConfigurableComponentModel.CreateWithExports(mefExport1, mefExport2);

    //        configurableServiceProvider = new ConfigurableServiceProvider(false);
    //        configurableServiceProvider.RegisterService(typeof(SComponentModel), mefModel);
    //    }

    //    [TestMethod]
    //    public void Ctor_WithNullSolutionProvider_Throws()
    //    {
    //        Action op = () => new SuppressionManager(null);

    //        op.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
    //    }

    //    [TestMethod]
    //    public void Ctor_CallsRefresh()
    //    {
    //        // Arrange
    //        activeSolutionBoundTrackerMock.SetupGet(x => x.CurrentBindingConfiguration).Returns(BindingConfiguration.Standalone);

    //        // Act
    //        var suppressionManager = new SuppressionManager(configurableServiceProvider);

    //        // Assert
    //        activeSolutionBoundTrackerMock.Verify(x => x.CurrentBindingConfiguration, Times.Once);
    //        sonarLintOutputMock.Verify(x => x.WriteLine(It.IsAny<string>()), Times.Never);
    //    }

    //    [TestMethod]
    //    public void Ctor_ThrowDuringRefresh_Suppressed()
    //    {
    //        // Arrange
    //        activeSolutionBoundTrackerMock.SetupGet(x => x.CurrentBindingConfiguration).Throws<InvalidCastException>();

    //        // Act
    //        var suppressionManager = new SuppressionManager(configurableServiceProvider);

    //        // Assert
    //        activeSolutionBoundTrackerMock.Verify(x => x.CurrentBindingConfiguration, Times.Once);
    //        sonarLintOutputMock.Verify(x => x.WriteLine(It.IsAny<string>()), Times.Once);
    //    }

    //    [TestMethod]
    //    public void ChangingSolution_TriggersRefresh()
    //    {
    //        // Arrange
    //        activeSolutionBoundTrackerMock.SetupGet(x => x.CurrentBindingConfiguration).Returns(BindingConfiguration.Standalone);
    //        var suppressionManager = new SuppressionManager(configurableServiceProvider);
    //        activeSolutionBoundTrackerMock.ResetCalls();

    //        // Act
    //        activeSolutionBoundTrackerMock.Raise(x => x.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(BindingConfiguration.Standalone));

    //        // Assert
    //        activeSolutionBoundTrackerMock.Verify(x => x.CurrentBindingConfiguration, Times.Once);
    //    }

    //    [TestMethod]
    //    public void Disposing_UnregistersEventHandler()
    //    {
    //        // Arrange
    //        activeSolutionBoundTrackerMock.SetupGet(x => x.CurrentBindingConfiguration).Returns(BindingConfiguration.Standalone);
    //        var suppressionManager = new SuppressionManager(configurableServiceProvider);
    //        activeSolutionBoundTrackerMock.ResetCalls();

    //        // Act
    //        suppressionManager.Dispose();

    //        // Assert
    //        // Raise the event - should not trigger any action
    //        activeSolutionBoundTrackerMock.Raise(x => x.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(BindingConfiguration.Standalone));
    //        activeSolutionBoundTrackerMock.Verify(x => x.CurrentBindingConfiguration, Times.Never);
    //    }

    //}
}
