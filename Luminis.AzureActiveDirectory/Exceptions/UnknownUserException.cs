// <copyright file="UnknownUserException.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory.Exceptions
{
    using System;

    /// <summary>
    /// An exception that is thrown when the user is not known in the AD.
    /// </summary>
    public class UnknownUserException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownUserException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public UnknownUserException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownUserException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public UnknownUserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownUserException"/> class.
        /// </summary>
        public UnknownUserException()
        {
        }
    }
}
