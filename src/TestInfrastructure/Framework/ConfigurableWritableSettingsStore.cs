//-----------------------------------------------------------------------
// <copyright file="ConfigurableWritableSettingsStore.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableWritableSettingsStore : WritableSettingsStore
    {
        private readonly IDictionary<string, IDictionary<string, object>> collections = new Dictionary<string, IDictionary<string, object>>();

        #region Helpers

        public void AssertCollectionExists(string collectionPath)
        {
            Assert.IsTrue(this.CollectionExists(collectionPath), $"Collection '{collectionPath}' does not exist. Collections: {string.Join(", ", this.collections.Keys)}");
        }

        public void AssertCollectionDoesNotExist(string collectionPath)
        {
            Assert.IsFalse(this.CollectionExists(collectionPath), $"Unexpected collection '{collectionPath}'");
        }

        public void AssertCollectionPropertyCount(string collectionPath, int numProperties)
        {
            this.AssertCollectionExists(collectionPath);
            ICollection<string> properties = this.collections[collectionPath].Keys;
            Assert.AreEqual(numProperties, properties.Count, $"Unexpected numer of properties in collection '{collectionPath}'. Properties: {string.Join(", ", properties)}");
        }

        public void AssertBoolean(string collectionPath, string key, bool value)
        {
            this.AssertCollectionExists(collectionPath);
            IDictionary<string, object> collection = this.collections[collectionPath];
            Assert.IsTrue(collection.ContainsKey(key), $"Property '{key}' was missing from collection '{collectionPath}'. Properties: {string.Join(", ", collection.Keys)}");
            Assert.AreEqual(value, collection[key], "Unexpected boolean property value");
        }

        #endregion

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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override void SetUInt32(string collectionPath, string propertyName, uint value)
        {
            throw new NotImplementedException();
        }

        public override void SetUInt64(string collectionPath, string propertyName, ulong value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
