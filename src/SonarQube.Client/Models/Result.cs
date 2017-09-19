using System;
using System.Net;

namespace SonarQube.Client.Models
{
    public struct Result<TValue>
    {
        public bool IsSuccess => StatusCode.HasValue || Exception != null || ErrorMessage != null;
        public bool IsFailure => !IsSuccess;

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
