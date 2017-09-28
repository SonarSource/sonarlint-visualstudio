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
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/* Assembly resolution:
 * We want assembly resolution in the test app domain to work the same way it
 * does in the main/calling app domain. To do this, we set up assembly resolver
 * for the test app domain to handle locating any assemblies the runtime fails
 * to find. The resolver method makes a call to code running in the main app
 * domain, and we try to locate the assembly in the main app domain.
 */

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Helper class that provides the plumbing to run a test in a separate app domain
    /// </summary>
    /// <typeparam name="T">The type of the remote object to be created the new app domain</typeparam>
    /// <remarks>The new AppDomain will be created when the test wrapper instance is created,
    /// and unloaded when the test wrapper is disposed.</remarks>
    public sealed class TestDomainWrapper<T> : MarshalByRefObject, IDisposable
        where T : MarshalByRefObject
    {
        private const string TestDomainName = "sonarlint remote test domain";
        private const string AppDomainDataKey = "test domain wrapper";

        public TestDomainWrapper()
        {
            CreateTestAppDomain();
        }

        public AppDomain TestAppDomain { get; private set; }

        /// <summary>
        /// Any method calls made on the remote object will be executed in the remote
        /// app domain
        /// </summary>
        public T RemoteObject { get; private set; }

        private void CreateTestAppDomain()
        {
            AppDomainSetup domainSetup = new AppDomainSetup()
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
            };

            TestAppDomain = AppDomain.CreateDomain(TestDomainName, null, domainSetup);

            TestAppDomain.SetData(AppDomainDataKey, this);

            TestAppDomain.AssemblyResolve += OnAssemblyResolve;

            RemoteObject = (T)TestAppDomain.CreateInstanceAndUnwrap(typeof(T).Assembly.FullName, typeof(T).FullName);
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            CheckIsInTestDomain();

            TestDomainWrapper<T> resolver = AppDomain.CurrentDomain.GetData(AppDomainDataKey) as TestDomainWrapper<T>;
            Assert.IsNotNull(resolver, "Test setup error: failed to obtain the remote domain wrapper");

            string asmLocation = resolver.GetAssemblyLocation(args.Name);
            if (asmLocation != null)
            {
                Assembly asm = Assembly.LoadFrom(asmLocation);
                return asm;
            }
            return null;
        }

        private string GetAssemblyLocation(string assemblyName)
        {
            CheckIsNotInTestDomain();

            // Try to locate the assembly in the main app domain i.e. using the configuration,
            // resolution paths, binding redirects etc set by the test runner, 

            // 1. Check if the exact version of the assembly is already loaded in this app domain
            Assembly asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == assemblyName);

            if (asm == null)
            {
                try
                {
                    // 2. If that fails, try to load the exact version
                    asm = Assembly.Load(assemblyName);
                }
                catch
                {
                    // 3. Finally, look to see if any version of the assembly is already loaded
                    //    (in case of binding redirects)
                    string partialName = new AssemblyName(assemblyName).Name;
                    asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == partialName);
                }
            }

            return asm?.Location;
        }

        private static void CheckIsInTestDomain()
        {
            Assert.IsTrue(IsInTestDomain(), "Test setup error: should be executing in the remote test app domain");
        }

        private static void CheckIsNotInTestDomain()
        {
            Assert.IsFalse(IsInTestDomain(), "Test setup error: should be executing in the main app domain");
        }

        private static bool IsInTestDomain()
        {
            return AppDomain.CurrentDomain.FriendlyName == TestDomainName;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AppDomain.Unload(TestAppDomain);
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
