/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Reflection;
using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Based on https://github.com/microsoft/vssdktestfx/blob/main/doc/mstest.md
    /// </summary>
    [TestClass]
    public class VsThreadingFixer
    {
        internal static GlobalServiceProvider MockServiceProvider { get; private set; }

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            MockServiceProvider = new GlobalServiceProvider();
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine($"XXX AssemblyResolve: {args.Name}");
            Console.WriteLine($"YYY Requesting assembly: {args?.RequestingAssembly?.FullName}");
            return null;
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            MockServiceProvider.Dispose();
        }
    }
}
