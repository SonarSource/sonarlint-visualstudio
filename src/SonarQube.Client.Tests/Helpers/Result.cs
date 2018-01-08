/*
 * SonarQube Client
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

using System.Net;
using System.Net.Http;
using SonarQube.Client.Services;

namespace SonarQube.Client.Tests
{
    // Factory method to simplify result creation in tests
    public static class Result
    {
        public static Result<TValue> Ok<TValue>(TValue value) =>
            new Result<TValue>(new HttpResponseMessage(), value);

        public static Result<TValue> NotFound<TValue>(TValue value) =>
            new Result<TValue>(new HttpResponseMessage(HttpStatusCode.NotFound), value);
    }
}
