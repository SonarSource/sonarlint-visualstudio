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

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.Settings;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableWritableSettingsStore : WritableSettingsStore
    {
        private readonly IDictionary<string, IDictionary<string, object>> collections = new Dictionary<string, IDictionary<string, object>>();

        #region Helpers

        public void AssertCollectionExists(string collectionPath)
        {
            this.CollectionExists(collectionPath).Should().BeTrue($"Collection '{collectionPath}' does not exist. Collections: {string.Join(", ", this.collections.Keys)}");
        }

        public void AssertCollectionDoesNotExist(string collectionPath)
        {
            this.CollectionExists(collectionPath).Should().BeFalse($"Unexpected collection '{collectionPath}'");
        }

        public void AssertCollectionPropertyCount(string collectionPath, int numProperties)
        {
            this.AssertCollectionExists(collectionPath);
            ICollection<string> properties = this.collections[collectionPath].Keys;
            properties.Should().HaveCount(numProperties, $"Unexpected number of properties in collection '{collectionPath}'. Properties: {string.Join(", ", properties)}");
        }

        public void AssertBoolean(string collectionPath, string key, bool value)
        {
            this.AssertCollectionExists(collectionPath);
            IDictionary<string, object> collection = this.collections[collectionPath];
            collection.ContainsKey(key).Should().BeTrue($"Property '{key}' was missing from collection '{collectionPath}'. Properties: {string.Join(", ", collection.Keys)}");
            collection[key].Should().Be(value, "Unexpected boolean property value");
        }

        public void AssertString(string collectionPath, string key, string value)
        {
            this.AssertCollectionExists(collectionPath);
            IDictionary<string, object> collection = this.collections[collectionPath];
            collection.ContainsKey(key).Should().BeTrue($"Property '{key}' was missing from collection '{collectionPath}'. Properties: {string.Join(", ", collection.Keys)}");
            collection[key].Should().Be(value, "Unexpected string property value");
        }

        public void AssertInt(string collectionPath, string key, int value)
        {
            this.AssertCollectionExists(collectionPath);
            IDictionary<string, object> collection = this.collections[collectionPath];
            collection.ContainsKey(key).Should().BeTrue($"Property '{key}' was missing from collection '{collectionPath}'. Properties: {string.Join(", ", collection.Keys)}");
            collection[key].Should().Be(value, "Unexpected string property value");
        }


        #endregion Helpers

        #region WritableSettingsStore

        public override bool CollectionExists(string collectionPath)
        {
            return this.collections.ContainsKey(collectionPath);
        }

        public override void CreateCollection(string collectionPath)
        {
            this.collections.Add(collectionPath, new Dictionary<string, object>());
        }

        public override bool DeleteCollection(string collectionPath)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteProperty(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override bool GetBoolean(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override bool GetBoolean(string collectionPath, string propertyName, bool defaultValue)
        {
            object rawValue = null;
            this.collections?[collectionPath].TryGetValue(propertyName, out rawValue);
            return (rawValue as bool?).GetValueOrDefault(defaultValue);
        }

        public override int GetInt32(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override int GetInt32(string collectionPath, string propertyName, int defaultValue)
        {
            object rawValue = null;
            this.collections?[collectionPath].TryGetValue(propertyName, out rawValue);
            return (rawValue as int?).GetValueOrDefault(defaultValue);
        }

        public override long GetInt64(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override long GetInt64(string collectionPath, string propertyName, long defaultValue)
        {
            throw new NotImplementedException();
        }

        public override DateTime GetLastWriteTime(string collectionPath)
        {
            throw new NotImplementedException();
        }

        public override MemoryStream GetMemoryStream(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override int GetPropertyCount(string collectionPath)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetPropertyNames(string collectionPath)
        {
            throw new NotImplementedException();
        }

        public override SettingsType GetPropertyType(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override string GetString(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override string GetString(string collectionPath, string propertyName, string defaultValue)
        {
            object rawValue = null;
            this.collections?[collectionPath].TryGetValue(propertyName, out rawValue);
            return rawValue != null ? rawValue as string : defaultValue;
        }

        public override int GetSubCollectionCount(string collectionPath)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetSubCollectionNames(string collectionPath)
        {
            throw new NotImplementedException();
        }

        public override uint GetUInt32(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override uint GetUInt32(string collectionPath, string propertyName, uint defaultValue)
        {
            throw new NotImplementedException();
        }

        public override ulong GetUInt64(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override ulong GetUInt64(string collectionPath, string propertyName, ulong defaultValue)
        {
            throw new NotImplementedException();
        }

        public override bool PropertyExists(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override void SetBoolean(string collectionPath, string propertyName, bool value)
        {
            this.collections[collectionPath][propertyName] = value;
        }

        public override void SetInt32(string collectionPath, string propertyName, int value)
        {
            this.collections[collectionPath][propertyName] = value;
        }

        public override void SetInt64(string collectionPath, string propertyName, long value)
        {
            throw new NotImplementedException();
        }

        public override void SetMemoryStream(string collectionPath, string propertyName, MemoryStream value)
        {
            throw new NotImplementedException();
        }

        public override void SetString(string collectionPath, string propertyName, string value)
        {
            this.collections[collectionPath][propertyName] = value;
        }

        public override void SetUInt32(string collectionPath, string propertyName, uint value)
        {
            throw new NotImplementedException();
        }

        public override void SetUInt64(string collectionPath, string propertyName, ulong value)
        {
            throw new NotImplementedException();
        }

        #endregion WritableSettingsStore
    }
}