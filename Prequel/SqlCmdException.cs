// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Runtime.Serialization;

namespace Prequel;

/// <summary>
///   Represents an error that occurred during the operation of
///   <see cref="SqlCmdPreprocessor"/>.
/// </summary>
[Serializable]
public class SqlCmdException : Exception
{
    /// <summary>
    ///   Initializes a new <see cref="SqlCmdException"/> instance with a
    ///   default message.
    /// </summary>
    public SqlCmdException()
        : base("An error occurred during SqlCmd preprocessing.") { }

    /// <summary>
    ///   Initializes a new <see cref="SqlCmdException"/> instance with the
    ///   specified message.
    /// </summary>
    /// <param name="message">
    ///   A message that describes the exception.
    /// </param>
    public SqlCmdException(string message)
        : base(message) { }

    /// <summary>
    ///   Initializes a new <see cref="SqlCmdException"/> instance with the
    ///   specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///   A message that describes the exception.
    /// </param>
    /// <param name="innerException">
    ///   The inner exception that is the cause of the new exception, or
    ///   <see langword="null"/>.
    /// </param>
    public SqlCmdException(string message, Exception? innerException)
        : base(message, innerException) { }

    /// <summary>
    ///   Initializes a new <see cref="SqlCmdException"/> instance with
    ///   serialized data.
    /// </summary>
    /// <param name="info">
    ///   The serialized object data from which to construct the exception.
    /// </param>
    /// <param name="context">
    ///   Contextual information about the deserialization operation.
    /// </param>
    /// <remarks>
    ///   ⚠ <strong>WARNING:</strong> The <c>BinaryFormatter</c> type is
    ///   dangerous and is <strong><em>not</em></strong> recommended for data
    ///   processing.  Applications should stop using <c>BinaryFormatter</c> as
    ///   soon as possible, even if they believe the data they are processing
    ///   to be trustworthy.  <c>BinaryFormatter</c> is insecure and cannot be
    ///   made secure.
    ///   <a href="https://docs.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide">See here for more information.</a>.
    /// </remarks>
    [Obsolete("BinaryFormatter serialization is insecure and cannot be made secure.")]
    protected SqlCmdException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
