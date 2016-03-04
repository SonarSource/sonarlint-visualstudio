//-----------------------------------------------------------------------
// <copyright file="PropertiesMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class PropertiesMock : Properties
    {
        private readonly List<PropertyMock> properties = new List<PropertyMock>();
        private readonly object parent;

        public PropertiesMock(object parent)
        {
            this.parent = parent;
        }

        #region Properties
        object Properties.Application
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        int Properties.Count
        {
            get
            {
                return this.properties.Count;
            }
        }

        DTE Properties.DTE
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object Properties.Parent
        {
            get
            {
                return this.parent;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.properties.GetEnumerator();
        }

        IEnumerator Properties.GetEnumerator()
        {
            return this.properties.GetEnumerator();
        }

        Property Properties.Item(object index)
        {
            return this.properties[(int)index - 1]; // Starts from 1.
        }
        #endregion

        #region Test helpers
        public PropertyMock RegisterKnownProperty(string name)
        {
            if (this.properties.Any(p=>p.Name == name))
            {
                Assert.Inconclusive($"Already has property: {name}");
            }

            PropertyMock prop = new PropertyMock(name, this);
            this.properties.Add(prop);
            return prop;
        }

        public void AssertPropertyExists(string name, object value)
        {
            PropertyMock property = this.properties.SingleOrDefault(p => p.Name == name);
            Assert.IsNotNull(property, $"Could not find property {name}");
            Assert.AreEqual(value, property.Value, $"Unexpected property {name} value");
        }
        #endregion  
    }
}
