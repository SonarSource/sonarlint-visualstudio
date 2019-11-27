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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class BindingProcessImplTests
    {
        [TestMethod]
        public void Ctor_ArgChecks()
        {
            var validHost = new ConfigurableHost();
            var bindingArgs = new BindCommandArgs("key", "name", new ConnectionInformation(new Uri("http://server")));
            var slnBindOp = new Mock<ISolutionBindingOperation>().Object;
            var nuGetOp = new Mock<INuGetBindingOperation>().Object;
            var bindingInfoProvider = new ConfigurableSolutionBindingInformationProvider();

            // 1. Null host
            Action act = () => new BindingProcessImpl(null, bindingArgs, slnBindOp, nuGetOp, bindingInfoProvider);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("host");

            // 2. Null binding args
            act = () => new BindingProcessImpl(validHost, null, slnBindOp, nuGetOp, bindingInfoProvider);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingArgs");

            // 3. Null solution binding operation
            act = () => new BindingProcessImpl(validHost, bindingArgs, null, nuGetOp, bindingInfoProvider);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingOperation");

            // 4. Null NuGet operation
            act = () => new BindingProcessImpl(validHost, bindingArgs, slnBindOp, null, bindingInfoProvider);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("nugetBindingOperation");

            // 5. Null binding info provider
            act = () => new BindingProcessImpl(validHost, bindingArgs, slnBindOp, nuGetOp, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingInformationProvider");
        }
    }
}
