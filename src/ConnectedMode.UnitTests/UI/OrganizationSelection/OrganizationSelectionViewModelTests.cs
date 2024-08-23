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

using System.ComponentModel;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.OrganizationSelection;

[TestClass]
public class OrganizationSelectionViewModelTests
{
    private OrganizationSelectionViewModel testSubject;
    private ISlCoreConnectionAdapter slCoreConnectionAdapter;
    private ICredentialsModel credentialsModel;
    private IProgressReporterViewModel progressReporterViewModel;

    [TestInitialize]
    public void TestInitialize()
    {
        credentialsModel = Substitute.For<ICredentialsModel>();
        slCoreConnectionAdapter = Substitute.For<ISlCoreConnectionAdapter>();
        progressReporterViewModel = Substitute.For<IProgressReporterViewModel>();

        testSubject = new(credentialsModel, slCoreConnectionAdapter, progressReporterViewModel);
    }

    [TestMethod]
    public void Ctor_Organizations_SetsEmptyAsDefault()
    {
        new OrganizationSelectionViewModel(credentialsModel, slCoreConnectionAdapter, progressReporterViewModel).Organizations.Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_OrganizationList_SetsPropertyValue()
    { 
        var organization = new OrganizationDisplay("key", "name");

        testSubject.AddOrganization(organization);

        testSubject.Organizations.Count.Should().Be(1);
        testSubject.Organizations[0].Should().Be(organization);
    }

    [TestMethod]
    public void SelectedOrganization_NotSet_ValueIsNull()
    {
        testSubject.SelectedOrganization.Should().BeNull();
        testSubject.IsValidSelectedOrganization.Should().BeFalse();
    }

    [TestMethod]
    public void SelectedOrganization_Set_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.SelectedOrganization = new OrganizationDisplay("key", "name");

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.SelectedOrganization)));
        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsValidSelectedOrganization)));
    }

    [TestMethod]
    public void IsValidSelectedOrganization_NullOrganization_ReturnsFalse()
    {
        testSubject.SelectedOrganization = null;

        testSubject.IsValidSelectedOrganization.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("      ")]
    public void IsValidSelectedOrganization_OrganizationWithInvalidKey_ReturnsFalse(string key)
    {
        testSubject.SelectedOrganization = new OrganizationDisplay(key, "value");

        testSubject.IsValidSelectedOrganization.Should().BeFalse();
    }
    
    [DataTestMethod]
    [DataRow("mykey")]
    [DataRow("my key")]
    public void IsValidSelectedOrganization_OrganizationWithValidKey_ReturnsTrue(string key)
    {
        testSubject.SelectedOrganization = new OrganizationDisplay(key, "value");

        testSubject.IsValidSelectedOrganization.Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow("key", null, true)]
    [DataRow("key", "name", true)]
    [DataRow(null, null, false)]
    [DataRow(null, "name", false)]
    public void IsValidSelectedOrganization_IgnoresName(string key, string name, bool expectedResult)
    {
        testSubject.SelectedOrganization = new OrganizationDisplay(key, name);

        testSubject.IsValidSelectedOrganization.Should().Be(expectedResult);
    }

    [TestMethod]
    public void NoOrganizationExists_NoOrganizations_ReturnsTrue()
    { 
        testSubject.NoOrganizationExists.Should().BeTrue();
    }

    [TestMethod]
    public void NoOrganizationExists_HasOrganizations_ReturnsFalse()
    {
        testSubject.AddOrganization(new OrganizationDisplay("key", "name"));

        testSubject.NoOrganizationExists.Should().BeFalse();
    }

    [TestMethod]
    public void AddOrganization_RaisesEvent()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        testSubject.AddOrganization(new OrganizationDisplay("key", "name"));

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.NoOrganizationExists)));
    }

    [TestMethod]
    public void AddOrganization_AddsToList()
    {
        testSubject.Organizations.Should().BeEmpty();
        var newOrganization = new OrganizationDisplay("key", "name");

        testSubject.AddOrganization(newOrganization);

        testSubject.Organizations.Should().BeEquivalentTo(newOrganization);
    }

    [TestMethod]
    public async Task LoadOrganizationsAsync_UpdatesProgress()
    {
        MockAdapterLoadOrganizationsAsync();

        await testSubject.LoadOrganizationsAsync();

        Received.InOrder(() =>
        {
            progressReporterViewModel.ProgressStatus = UiResources.LoadingOrganizationsProgressText;
            slCoreConnectionAdapter.GetOrganizationsAsync(Arg.Any<ICredentialsModel>());
            progressReporterViewModel.ProgressStatus = null;
        });
    }

    [TestMethod]
    public async Task LoadOrganizationsAsync_AdapterThrowsException_SetsProgressToNull()
    {
        testSubject.ProgressReporterViewModel.ProgressStatus.Returns(UiResources.LoadingOrganizationsProgressText);

        await ExecuteLoadOrganizationsAsyncThrowingException();

        testSubject.ProgressReporterViewModel.Received(1).ProgressStatus = null;
    }

    [TestMethod]
    public async Task LoadOrganizationsAsync_AdapterReturnsFailedResponse_UpdatesWarning()
    {
        MockAdapterLoadOrganizationsAsync(success: false);

        await testSubject.LoadOrganizationsAsync();

        progressReporterViewModel.Received(1).Warning = UiResources.LoadingOrganizationsFailedText;
    }

    [TestMethod]
    public async Task LoadOrganizationsAsync_AdapterSucceeds_DoesNotUpdateWarning()
    {
        MockAdapterLoadOrganizationsAsync(success: true);

        await testSubject.LoadOrganizationsAsync();

        progressReporterViewModel.DidNotReceive().Warning = UiResources.LoadingOrganizationsFailedText;
    }

    [TestMethod]
    public async Task LoadOrganizationsAsync_ResetsPreviousWarningBeforeCallingAdapter()
    {
        MockAdapterLoadOrganizationsAsync(success: false);

        await testSubject.LoadOrganizationsAsync();

        Received.InOrder(() =>
        {
            progressReporterViewModel.Warning = null;
            slCoreConnectionAdapter.GetOrganizationsAsync(Arg.Any<ICredentialsModel>());
            progressReporterViewModel.Warning = UiResources.LoadingOrganizationsFailedText;
        });
    }

    [TestMethod]
    public async Task LoadOrganizationsAsync_AdapterSucceeds_AddsOrganization()
    {
        var loadedOrganizations = new List<OrganizationDisplay> { new("key", "name") };
        MockAdapterLoadOrganizationsAsync(success: true, organizations: loadedOrganizations);

        await testSubject.LoadOrganizationsAsync();
       
        testSubject.Organizations.Should().BeEquivalentTo(loadedOrganizations);
    }

    [TestMethod]
    public async Task LoadOrganizationsAsync_AdapterSucceeds_ClearsPreviousOrganizations()
    {
        testSubject.Organizations.Add(new("key", "name"));
        var loadedOrganizations = new List<OrganizationDisplay> { new("new_key", "new_name") };
        MockAdapterLoadOrganizationsAsync(success: true, organizations: loadedOrganizations);

        await testSubject.LoadOrganizationsAsync();

        testSubject.Organizations.Should().BeEquivalentTo(loadedOrganizations);
    }

    [TestMethod]
    public async Task LoadOrganizationsAsync_AdapterSucceeds_RaisesEvents()
    {
        MockAdapterLoadOrganizationsAsync(success: true);
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        await testSubject.LoadOrganizationsAsync();

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.NoOrganizationExists)));
    }

    private void MockAdapterLoadOrganizationsAsync(bool success = true, List<OrganizationDisplay> organizations = null)
    {
        slCoreConnectionAdapter.GetOrganizationsAsync(Arg.Any<ICredentialsModel>())
            .Returns(new AdapterResponse<List<OrganizationDisplay>>(success, organizations ?? []));
    }

    private async Task ExecuteLoadOrganizationsAsyncThrowingException()
    {
        slCoreConnectionAdapter.When(x => x.GetOrganizationsAsync(Arg.Any<ICredentialsModel>()))
            .Do(x => throw new Exception("testing"));
        try
        {
            await testSubject.LoadOrganizationsAsync();
        }
        catch (Exception)
        {
            // this is only for testing purposes
        }
    }
}
