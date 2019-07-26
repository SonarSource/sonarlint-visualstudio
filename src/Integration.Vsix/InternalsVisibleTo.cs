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

using System.Runtime.CompilerServices;

#if SignAssembly
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Integration.Vsix.UnitTests,PublicKey=002400000480000094000000060200000024000052534131000400000100010081b4345a022cc0f4b42bdc795a5a7a1623c1e58dc2246645d751ad41ba98f2749dc5c4e0da3a9e09febcb2cd5b088a0f041f8ac24b20e736d8ae523061733782f9c4cd75b44f17a63714aced0b29a59cd1ce58d8e10ccdb6012c7098c39871043b7241ac4ab9f6b34f183db716082cd57c1ff648135bece256357ba735e67dc6")]
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Integration.TestInfrastructure,PublicKey=002400000480000094000000060200000024000052534131000400000100010081b4345a022cc0f4b42bdc795a5a7a1623c1e58dc2246645d751ad41ba98f2749dc5c4e0da3a9e09febcb2cd5b088a0f041f8ac24b20e736d8ae523061733782f9c4cd75b44f17a63714aced0b29a59cd1ce58d8e10ccdb6012c7098c39871043b7241ac4ab9f6b34f183db716082cd57c1ff648135bece256357ba735e67dc6")]

// Moq -- see https://github.com/Moq/moq4/wiki/Quickstart
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

#else
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Integration.Vsix.UnitTests")]
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Integration.TestInfrastructure")]

// Moq
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif
