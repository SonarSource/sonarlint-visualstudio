//-----------------------------------------------------------------------
// <copyright file="InternalsVisibleTo.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Runtime.CompilerServices;

#if SignAssembly
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Progress.UnitTests,PublicKey=002400000480000094000000060200000024000052534131000400000100010081b4345a022cc0f4b42bdc795a5a7a1623c1e58dc2246645d751ad41ba98f2749dc5c4e0da3a9e09febcb2cd5b088a0f041f8ac24b20e736d8ae523061733782f9c4cd75b44f17a63714aced0b29a59cd1ce58d8e10ccdb6012c7098c39871043b7241ac4ab9f6b34f183db716082cd57c1ff648135bece256357ba735e67dc6")]
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Progress.TestFramework,PublicKey=002400000480000094000000060200000024000052534131000400000100010081b4345a022cc0f4b42bdc795a5a7a1623c1e58dc2246645d751ad41ba98f2749dc5c4e0da3a9e09febcb2cd5b088a0f041f8ac24b20e736d8ae523061733782f9c4cd75b44f17a63714aced0b29a59cd1ce58d8e10ccdb6012c7098c39871043b7241ac4ab9f6b34f183db716082cd57c1ff648135bece256357ba735e67dc6")]
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.ProgressVS,PublicKey=002400000480000094000000060200000024000052534131000400000100010081b4345a022cc0f4b42bdc795a5a7a1623c1e58dc2246645d751ad41ba98f2749dc5c4e0da3a9e09febcb2cd5b088a0f041f8ac24b20e736d8ae523061733782f9c4cd75b44f17a63714aced0b29a59cd1ce58d8e10ccdb6012c7098c39871043b7241ac4ab9f6b34f183db716082cd57c1ff648135bece256357ba735e67dc6")]
#else
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Progress.UnitTests")]
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Progress.TestFramework")]
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.ProgressVS")]
#endif
