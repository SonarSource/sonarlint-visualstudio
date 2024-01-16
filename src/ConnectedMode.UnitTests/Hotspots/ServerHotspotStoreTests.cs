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

using System;
using System.Collections.Generic;
using SonarLint.VisualStudio.ConnectedMode.Hotspots;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Hotspots
{
    [TestClass]
    public class ServerHotspotStoreTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerHotspotStore, IServerHotspotStore>();
        }

        [TestMethod]
        public void GetAll_NotRefreshed_ReturnsEmpty()
        {
            var testSubject = new ServerHotspotStore();

            var result = testSubject.GetAll();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Refresh_SetsCollection()
        {
            var serverHotspots1 = CreateHotspotList("key1", "key2");
            var serverHotspots2 = CreateHotspotList("key3", "key4", "key5");

            var testSubject = new ServerHotspotStore();
            var eventCounter = new EventCounter(testSubject);

            testSubject.Refresh(serverHotspots1);

            var result = testSubject.GetAll();

            result.Should().BeEquivalentTo(serverHotspots1);
            eventCounter.RefreshedCount.Should().HaveCount(1);

            testSubject.Refresh(serverHotspots2);

            result = testSubject.GetAll();

            result.Should().BeEquivalentTo(serverHotspots2);
            eventCounter.RefreshedCount.Should().HaveCount(2);
        }

        [TestMethod]
        public void Refresh_NullParameter_Fails()
        {
            var testSubject = new ServerHotspotStore();
            var eventCounter = new EventCounter(testSubject);

            Action act = () => testSubject.Refresh(null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("serverHotspots");

            eventCounter.RefreshedCount.Should().BeEmpty();
        }

        private IList<SonarQubeHotspot> CreateHotspotList(params string[] hotspotKeys)
        {
            var result = new List<SonarQubeHotspot>();

            foreach (var hotspotKey in hotspotKeys)
            {
                result.Add(new SonarQubeHotspot(hotspotKey, null, null, null, null, null, null, null, null, null, DateTimeOffset.Now, DateTimeOffset.Now, null, null, null));
            }

            return result;
        }

        private class EventCounter
        {
            private readonly List<EventArgs> refreshedCount = new List<EventArgs>();

            public IReadOnlyList<EventArgs> RefreshedCount => refreshedCount;

            public EventCounter(IServerHotspotStore store)
            {
                store.Refreshed += (_, e) =>
                {
                    refreshedCount.Add(e);
                };
            }
        }
    }
}
