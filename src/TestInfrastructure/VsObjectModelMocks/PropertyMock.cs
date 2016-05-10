//-----------------------------------------------------------------------
// <copyright file="PropertyMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System;

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
        #endregion
    }
}
