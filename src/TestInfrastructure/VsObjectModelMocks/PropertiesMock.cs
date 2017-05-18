/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

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

        #endregion Properties

        #region Test helpers

        public PropertyMock RegisterKnownProperty(string name)
        {
            if (this.properties.Any(p => p.Name == name))
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith($"Already has property: {name}");
            }

            PropertyMock prop = new PropertyMock(name, this);
            this.properties.Add(prop);
            return prop;
        }

        #endregion Test helpers
    }
}