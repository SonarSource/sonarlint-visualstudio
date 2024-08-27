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
using Microsoft.VisualStudio.Threading;
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
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<ITaskToPerformParams<AdapterResponse>>()).Returns(new AdapterResponse(true));

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
    public void FinalConnectionInfo_SetByDefaultToNull()
    {
        testSubject.FinalConnectionInfo.Should().BeNull();
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
    public async Task LoadOrganizationsAsync_AddsOrganization()
    {
        await testSubject.LoadOrganizationsAsync();

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(
                Arg.Is<TaskToPerformParams<AdapterResponseWithData<List<OrganizationDisplay>>>>(x =>
                    x.TaskToPerform == testSubject.AdapterLoadOrganizationsAsync &&
                    x.ProgressStatus == UiResources.LoadingOrganizationsProgressText &&
                    x.WarningText == UiResources.LoadingOrganizationsFailedText &&
                    x.AfterSuccess == testSubject.UpdateOrganizations));
    }

    [TestMethod]
    public void UpdateOrganizations_AddsOrganization()
    {
        var loadedOrganizations = new List<OrganizationDisplay> { new("key", "name") };
        var response = new AdapterResponseWithData<List<OrganizationDisplay>>(true, loadedOrganizations);

        testSubject.UpdateOrganizations(response);
       
        testSubject.Organizations.Should().BeEquivalentTo(loadedOrganizations);
    }

    [TestMethod]
    public void UpdateOrganizations_ClearsPreviousOrganizations()
    {
        testSubject.Organizations.Add(new("key", "name"));
        var loadedOrganizations = new List<OrganizationDisplay> { new("new_key", "new_name") };
        var response = new AdapterResponseWithData<List<OrganizationDisplay>>(true, loadedOrganizations);

        testSubject.UpdateOrganizations(response);

        testSubject.Organizations.Should().BeEquivalentTo(loadedOrganizations);
    }

    [TestMethod]
    public void UpdateOrganizations_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();
        var response = new AdapterResponseWithData<List<OrganizationDisplay>>(true, []);

        testSubject.UpdateOrganizations(response);

        eventHandler.Received().Invoke(testSubject,
            Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.NoOrganizationExists)));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ValidateConnectionForOrganizationAsync_ReturnsResponseFromSlCore(bool success)
    {
        progressReporterViewModel.ExecuteTaskWithProgressAsync(Arg.Any<ITaskToPerformParams<AdapterResponse>>()).Returns(new AdapterResponse(success));

        var response = await testSubject.ValidateConnectionForOrganizationAsync("key","warning");

        response.Should().Be(success);
    }

    [TestMethod]
    public async Task ValidateConnectionForOrganizationAsync_CallsExecuteTaskWithProgressAsync()
    {
        var organizationKey = "key";
        var warningText = "warning";

        await testSubject.ValidateConnectionForOrganizationAsync(organizationKey, warningText);

        await progressReporterViewModel.Received(1)
            .ExecuteTaskWithProgressAsync(Arg.Is<ITaskToPerformParams<AdapterResponse>>(x =>
                IsExpectedSlCoreAdapterValidateConnectionAsync(x.TaskToPerform, organizationKey) &&
                x.ProgressStatus == UiResources.ValidatingConnectionProgressText &&
                x.WarningText == warningText));
    }

    [TestMethod]
    public void UpdateFinalConnectionInfo_ValueChanges_UpdatesConnectionInfo()
    {
        testSubject.UpdateFinalConnectionInfo("newKey");

        testSubject.FinalConnectionInfo.Should().NotBeNull();
        testSubject.FinalConnectionInfo.Id.Should().Be("newKey");
        testSubject.FinalConnectionInfo.ServerType.Should().Be(ConnectionServerType.SonarCloud);
    }

    private bool IsExpectedSlCoreAdapterValidateConnectionAsync(Func<Task<AdapterResponse>> xTaskToPerform, string organizationKey)
    {
        xTaskToPerform().Forget();
        slCoreConnectionAdapter.Received(1).ValidateConnectionAsync(Arg.Is<ConnectionInfo>(x=> x.Id == organizationKey), Arg.Any<ICredentialsModel>());
        return true;
    }
}
