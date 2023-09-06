/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Settings;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings;

[TestClass]
public class WritableSettingsStoreFactoryTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
        => MefTestHelpers.CheckTypeCanBeImported<WritableSettingsStoreFactory, IWritableSettingsStoreFactory>(
            MefTestHelpers.CreateExport<SVsServiceProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
        => MefTestHelpers.CheckIsSingletonMefComponent<SonarLintSettings>();

    [TestMethod]
    public void Ctor_DoesNotCallAnyServices()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var threadHandling = new Mock<IThreadHandling>();

        _ = CreateTestSubject(serviceProvider.Object, threadHandling.Object);

        // The MEF constructor should be free-threaded, which it will be if
        // it doesn't make any external calls.
        serviceProvider.Invocations.Should().BeEmpty();
        threadHandling.Invocations.Should().BeEmpty();
    }

    [TestMethod]
    public void Create_FactoryMethodIsCalledCorrectly()
    {
        var serviceProvider = Mock.Of<IServiceProvider>();
        var settingsManager = new Mock<SettingsManager>();
        var factoryMethod = CreateFactoryMethod(settingsManager.Object);

        var testSubject = CreateTestSubject(serviceProvider,
            factoryMethod: factoryMethod.Object);

        testSubject.Create("any").Should().BeNull();

        factoryMethod.Invocations.Should().HaveCount(1);
        factoryMethod.Verify(x => x.Invoke(serviceProvider), Times.Once); 
    }

    [TestMethod]
    public void Create_SettingsManagerReturnsNullStore_ReturnsNull()
    {
        var settingsManager = CreateSettingsManager(storeToReturn: null);

        var testSubject = CreateConfiguredTestSubject(settingsManager.Object);

        testSubject.Create("any").Should().BeNull();

        settingsManager.Verify(x => x.GetWritableSettingsStore(SettingsScope.UserSettings), Times.Once);
        settingsManager.VerifyAll();
    }

    [TestMethod]
    [DataRow("root1", false)]
    [DataRow("root2", true)]
    public void Create_Store_CollectionCreatedIfDoesNotExist(string settingsRoot, bool collectionExists)
    {
        var store = CreateStore(settingsRoot, collectionExists);
        var settingsManager = CreateSettingsManager(store.Object);

        var testSubject = CreateConfiguredTestSubject(settingsManager.Object);

        testSubject.Create(settingsRoot).Should().Be(store.Object);

        settingsManager.Verify(x => x.GetWritableSettingsStore(SettingsScope.UserSettings), Times.Once);
        settingsManager.VerifyAll();

        store.Verify(x => x.CollectionExists(settingsRoot), Times.Once);

        if (collectionExists)
        {
            store.Verify(x => x.CreateCollection(settingsRoot), Times.Never);
        }
        else
        {
            store.Verify(x => x.CreateCollection(settingsRoot), Times.Once);
        }
    }

    // TODO - threading test

    private static Mock<WritableSettingsStoreFactory.VSSettingsFactoryMethod> CreateFactoryMethod(
        SettingsManager settingsManagerToReturn)
    {
        var factoryMethod = new Mock<WritableSettingsStoreFactory.VSSettingsFactoryMethod>();
        factoryMethod.Setup(x => x.Invoke(It.IsAny<IServiceProvider>())).Returns(settingsManagerToReturn);
        return factoryMethod;
    }

    private static Mock<SettingsManager> CreateSettingsManager(WritableSettingsStore storeToReturn)
    {
        var settingsManager = new Mock<SettingsManager>();
        settingsManager.Setup(x => x.GetWritableSettingsStore(SettingsScope.UserSettings)).Returns(storeToReturn);
        return settingsManager;
    }

    private static Mock<WritableSettingsStore> CreateStore(string inputSettingsRoot, bool collectionExists)
    {
        var store = new Mock<WritableSettingsStore>();
        store.Setup(x => x.CollectionExists(inputSettingsRoot)).Returns(collectionExists);
        return store;
    }

    /// <summary>
    /// Creates a new test subject with the correct service provider etc setup to return
    /// the supplied settings manager
    /// </summary>
    private static WritableSettingsStoreFactory CreateConfiguredTestSubject(SettingsManager settingsManagerToReturn)
    {
        var factoryMethod = CreateFactoryMethod(settingsManagerToReturn);
        return CreateTestSubject(Mock.Of<IServiceProvider>(), null, factoryMethod.Object);
    }

    private static WritableSettingsStoreFactory CreateTestSubject(
        IServiceProvider serviceProvider = null,
        IThreadHandling threadHandling = null,
        WritableSettingsStoreFactory.VSSettingsFactoryMethod factoryMethod = null)
    {
        var testSubject = new WritableSettingsStoreFactory(
            serviceProvider ?? Mock.Of<IServiceProvider>(),
            threadHandling ?? new NoOpThreadHandler(),
            factoryMethod ?? (svc => null));

        return testSubject;
    }
}

