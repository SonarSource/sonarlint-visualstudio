/*
 * SonarQube Client
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
using System.Net;

namespace SonarQube.Client.Models
{
    public struct Result<TValue>
    {
        public bool IsFailure => StatusCode.HasValue || Exception != null || ErrorMessage != null;
        public bool IsSuccess => !IsFailure;

        public TValue Value { get; private set; }
        public HttpStatusCode? StatusCode { get; private set; }
        public Exception Exception { get; private set; }
        public string ErrorMessage { get; private set; }

        public static Result<T> Ok<T>(T value)
        {
            return new Result<T> { Value = value };
        }

        public static Result<T> Fail<T>(HttpStatusCode statusCode)
        {
            return new Result<T> { StatusCode = statusCode };
        }

        public static Result<T> Fail<T>(Exception exception)
        {
            return new Result<T> { Exception = exception };
        }

        public static Result<T> Fail<T>(string errorMessage)
        {
            return new Result<T> { ErrorMessage = errorMessage };
        }
    }
}
