﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System.Collections.Generic;
using LibGit2Sharp;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.LibGit2SharpWrappers
{
    internal class BranchCollectionWrapper : BranchCollection
    {
        private readonly IEnumerable<Branch> branches;

        internal BranchCollectionWrapper(IEnumerable<Branch> branches)
        {
            this.branches = branches;
        }

        public override IEnumerator<Branch> GetEnumerator()
        {
            return branches.GetEnumerator();
        }
    }
}
