/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.CFamily.Analysis
{
    [Export(typeof(IRequestFactoryAggregate))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class RequestFactoryAggregate : IRequestFactoryAggregate
    {
        private readonly IEnumerable<IRequestFactory> requestFactories;

        [ImportingConstructor]
        public RequestFactoryAggregate([ImportMany] IEnumerable<IRequestFactory> requestFactories)
        {
            this.requestFactories = requestFactories;
        }

        public Task<IRequest> TryCreateAsync(string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions)
        {
            if (string.IsNullOrEmpty(analyzedFilePath))
            {
                throw new ArgumentNullException(nameof(analyzedFilePath));
            }

            return TryCreate(analyzedFilePath, analyzerOptions);
        }

        private async Task<IRequest> TryCreate(string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions)
        {
            foreach (var requestFactory in requestFactories)
            {
                var request = await requestFactory.TryCreateAsync(analyzedFilePath, analyzerOptions);

                if (request != null)
                {
                    return request;
                }
            }

            return null;
        }
    }
}
