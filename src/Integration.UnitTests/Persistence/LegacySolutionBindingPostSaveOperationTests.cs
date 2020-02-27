/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.IO;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class LegacySolutionBindingPostSaveOperationTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private LegacySolutionBindingPostSaveOperation testSubject;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            serviceProvider = new ConfigurableServiceProvider();

            var dte = new DTEMock();
            dte.Solution = new SolutionMock(dte,
                Path.Combine(this.TestContext.TestRunDirectory, this.TestContext.TestName, "solution.sln"));
            serviceProvider.RegisterService(typeof(DTE), dte);

            projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider)
            {
                SolutionItemsProject = dte.Solution.AddOrGetProject("Solution Items")
            };
            serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);


            testSubject = new LegacySolutionBindingPostSaveOperation(serviceProvider);
        }

        [TestMethod]
        public void Ctor_NullArgument_Exception()
        {
            Action act = () => new LegacySolutionBindingPostSaveOperation(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }
    }
}
