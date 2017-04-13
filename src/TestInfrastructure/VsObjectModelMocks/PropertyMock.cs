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
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class PropertyMock : Property
    {
        private readonly Properties parent;

        public PropertyMock(string name, Properties parent)
        {
            this.parent = parent;
            this.Name = name;
        }

        #region Property

        public object Application
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Properties Collection
        {
            get
            {
                return parent;
            }
        }

        public DTE DTE
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Name
        {
            get;
            set;
        }

        public short NumIndices
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public object Object
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Properties Parent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public object Value
        {
            get;
            set;
        }

        public object get_IndexedValue(object Index1, object Index2, object Index3, object Index4)
        {
            throw new NotImplementedException();
        }

        public void let_Value(object lppvReturn)
        {
            throw new NotImplementedException();
        }

        public void set_IndexedValue(object Index1, object Index2, object Index3, object Index4, object Val)
        {
            throw new NotImplementedException();
        }

        #endregion Property
    }
}