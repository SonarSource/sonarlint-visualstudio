//-----------------------------------------------------------------------
// <copyright file="InternalsVisibleTo.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Integration.UnitTests")]
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Integration.Vsix.UnitTests")]
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Integration.TestInfrastructure")]
