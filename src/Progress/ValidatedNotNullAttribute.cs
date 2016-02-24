//-----------------------------------------------------------------------
// <copyright file="ValidatedNotNullAttribute.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Progress
{
    /// <summary>
    /// CodeAnalysis attribute that tells the CA1062 validate arguments rule that this method validates the argument is not null.
    /// Apply this attribute to the value parameter of your validation methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ValidatedNotNullAttribute : Attribute
    {
    }
}
