// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Prequel;

using static MathHelpers;
using static RegexOptions;

/// <summary>
///   A minimal <c>sqlcmd</c>-style preprocessor.
/// </summary>
/// <remarks>
///   <para>
///     This type supports a limited subset of <c>sqlcmd</c> preprocessing features:
///   </para>
///   <list type="table">
///     <item>
///       <term><c>GO</c></term>
///       <description>batch separator</description>
///     </item>
///     <item>
///       <term><c>$()</c></term>
///       <description><c>sqlcmd</c> variable expansion</description>
///     </item>
///     <item>
///       <term><c>:setvar</c></term>
///       <description>set a <c>sqlcmd</c> variable</description>
///     </item>
///     <item>
///       <term><c>:r</c></term>
///       <description>include a file</description>
///     </item>
///   </list>
///   <para>
///     For more information, see
///     <a href="https://docs.microsoft.com/en-us/sql/tools/sqlcmd-utility">the documentation</a>
///     for the <c>sqlcmd</c> utility.
///   </para>
/// </remarks>
public class SqlCmdPreprocessor
{
    // Minimum initial capacity of StringBuilder when needed
    internal const int MinimumBuilderCapacity = 4096;

    private readonly Dictionary<string, string> _variables;
    private          StringBuilder?             _builder;

    /// <summary>
    ///   Initializes a new <see cref="SqlCmdPreprocessor"/> instance.
    /// </summary>
    public SqlCmdPreprocessor()
    {
        _variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///   Gets the collection of <c>sqlcmd</c> variables currently defined.
    /// </summary>
    public IDictionary<string, string> Variables
        => _variables;

    /// <summary>
    ///   Preprocesses the specified text.
    /// </summary>
    /// <param name="text">
    ///   The text to be preprocessed.
    /// </param>
    /// <param name="name">
    ///   <para>
    ///     The logical name of the file containing <paramref name="text"/>, or
    ///     <see langword="null"/> to use the default name, <c>"(script)"</c>.
    ///   </para>
    ///   <para>
    ///     This value is informational only and need not be a valid file name.
    ///   </para>
    ///   <para>
    ///     Currently, this parameter is not used.  In the future, it might be
    ///     used to augment exceptions with text locations of errors.
    ///   </para>
    /// </param>
    /// <returns>
    ///   The result of preprocessing <paramref name="text"/>, split into
    ///   <c>GO</c>-separated batches.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="text"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="SqlCmdException">
    ///   The preprocessor encountered an error in the usage of a <c>sqlcmd</c>
    ///   feature.
    /// </exception>
    public IEnumerable<string> Process(string text, string? name = null)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));
        if (text.Length == 0)
            return Enumerable.Empty<string>();

        var input = new Input(name ?? DefaultInputName, text);
        return ProcessCore(input);
    }

    private IEnumerable<string> ProcessCore([DisallowNull] Input? input)
    {
        string batch;

        do
        {
            (batch, input) = GetNextBatch(input);

            if (batch.Length != 0)
                yield return batch;
        }
        while (input != null);
    }

    // Verbatim mode - reuse input string if possible
    private (string, Input?) GetNextBatch(Input input)
    {
        var start = input.Index;

        for (;;)
        {
            var match = input.NextToken();

            // Handle end of input
            if (!match.Success)
            {
                // End of top-level input => final batch
                if (input.Parent == null)
                    return (input.Range(start), null);

                // Non-empty batch continuing in parent input => switch to builder mode
                if (start != input.Length)
                    return BuildNextBatch(input, start, match);

                // Empty batch continuing in the parent input => continue in verbatim mode
                input = input.Parent;
                start = input.Index;
                continue;
            }

            // Handle token found
            switch (match.Value[0])
            {
                // Comments
                default:
                case '-':
                case '/':
                    // Comments are verbatim
                    continue;

                // Quoted
                case '\'':
                case '[':
                    // Variable expansion requires switch to builder mode
                    if (HasVariableReplacement(match.Value))
                        return BuildNextBatch(input, start, match);

                    // Other quoted strings/identifiers are verbatim
                    continue;

                // Preprocessor directives
                case '$':
                case ':':
                    // Requires switch to builder mode
                    return BuildNextBatch(input, start, match);

                // Batch separator
                case 'g':
                case 'G':
                    // Entire batch is verbatim => return portion of original input
                    return (input.Range(start, match.Index), input);
            }
        }
    }

    // Builder mode - assemble batch in a StringBuilder
    private (string, Input?) BuildNextBatch(Input input, int start, Match match)
    {
        var builder = InitializeBuilder(start, match.Index, input.Length);

        for (;;)
        {
            // Handle end of input
            if (!match.Success)
            {
                input.AppendRangeTo(builder, start);

                // End of top-level input => final batch
                if (input.Parent == null)
                    return (builder.ToString(), null);

                // Batch continues in parent input
                input = input.Parent;
                start = input.Index;
                match = input.NextToken();
                continue;
            }

            input.AppendRangeTo(builder, start, match.Index);

            // Handle token found
            switch (match.Value[0])
            {
                // Comments
                default:
                case '-':
                case '/':
                    // Comments are verbatim
                    builder.Append(match.Value);
                    break;

                // Quoted
                case '\'':
                case '[':
                    // Quoted strings/identifiers are subject to variable replacement
                    PerformVariableReplacement(match.Value);
                    break;

                // Variable expansion
                case '$':
                    builder.Append(GetVariableReplacement(match));
                    break;

                // Preprocessor directive
                case ':':
                    var args = match.Groups["args"].Value;
                    if (match.Value[1] == 'r')
                        Include(args, ref input);
                    else // :setvar
                        SetVariable(args);
                    break;

                // Batch separator
                case 'g':
                case 'G':
                    // Finish batch
                    return (builder.ToString(), input);
            }

            start = input.Index;
            match = input.NextToken();
        }
    }

    private void Include(string args, ref Input input)
    {
        var match = IncludeRegex.Match(args);
        if (!match.Success)
            throw new SqlCmdException("Invalid syntax in :r directive.");

        string path;
        path = match.Groups[nameof(path)].Value;
        path = Unquote(path);
        path = ReplaceVariables(path);

        var text = IoHelper.ReadText(path);

        input = new Input(path, text, input);
    }

    private void SetVariable(string args)
    {
        var match = SetVariableRegex.Match(args);
        if (!match.Success)
            throw new SqlCmdException("Invalid syntax in :setvar directive.");

        string name, value;
        name  = match.Groups[nameof(name )].Value;
        value = match.Groups[nameof(value)].Value;

        if (string.IsNullOrEmpty(value))
            _variables.Remove(name);
        else
            _variables[name] = Unquote(value);
    }

    internal static string Unquote(string value)
    {
        const char
            Quote = '"';

        const string
            QuoteUnescaped = @"""",
            QuoteEscaped   = QuoteUnescaped + QuoteUnescaped;

        if (value.Length == 0 || value[0] != Quote)
            return value;

        if (value.Length == 1 || value[value.Length - 1] != Quote)
            throw new SqlCmdException("Unterminated double-quoted string.");

        return value
            .Substring(1, value.Length - 2)
            .Replace(QuoteEscaped, QuoteUnescaped);
    }

    private string ReplaceVariables(string text)
    {
        // TODO: Potential for optimization here
        var builder = new StringBuilder();
        PerformVariableReplacement(builder, text);
        return builder.ToString();
    }

    private static bool HasVariableReplacement(string text)
    {
        return VariableRegex.IsMatch(text);
    }

    private void PerformVariableReplacement(string text)
    {
        // NULLS: InitializeBuilder is called before this method is used,
        // so _builder will not be null.
        PerformVariableReplacement(_builder!, text);
    }

    private void PerformVariableReplacement(StringBuilder builder, string text)
    {
        var start  = 0;
        var length = text.Length;

        while (start < length)
        {
            var match = VariableRegex.Match(text, start);

            if (!match.Success)
            {
                builder.Append(text, start, length - start);
                return;
            }

            builder.Append(text, start, match.Index - start);
            builder.Append(GetVariableReplacement(match));

            start = match.Index + match.Length;
        }
    }

    private string GetVariableReplacement(Match match)
    {
        var name = match.Groups["name"];

        var unterminated = match.Index + match.Length == name.Index + name.Length;
        if (unterminated)
            throw new SqlCmdException($"Unterminated reference to SqlCmd variable '{name.Value}'.");

        if (!_variables.TryGetValue(name.Value, out var value))
            throw new SqlCmdException($"SqlCmd variable '{name.Value}' is not defined.");

        return value;
    }

    private StringBuilder InitializeBuilder(int start, int end, int length)
    {
        // Calculate sizes
        length = (end > 0 ? end : length) - start;
        int capacity = GetPreferredBuilderCapacity(length);

        var builder = _builder;
        if (builder == null)
        {
            // Create builder for first time
            _builder = builder = new StringBuilder(capacity);
        }
        else // (builder != null)
        {
            // Reuse builder
            builder.Clear();
            builder.EnsureCapacity(capacity);
        }

        return builder;
    }

    internal static int GetPreferredBuilderCapacity(int length)
    {
        return length < MinimumBuilderCapacity
            ? MinimumBuilderCapacity
            : GetNextPowerOf2Saturating(length);
    }

    internal class Input
    {
        public Input(string name, string text, Input? parent = null)
        {
            Name   = name ?? throw new ArgumentNullException(nameof(name));
            Text   = text ?? throw new ArgumentNullException(nameof(text));
            Parent = parent;
        }

        public string Name   { get; }
        public string Text   { get; }
        public int    Index  { get; private set; }
        public Input? Parent { get; }

        public Match NextToken()
        {
            var match = TokenRegex.Match(Text, Index);

            Index = match.Index + match.Length;

            return match;
        }

        public int Length
            => Text.Length;

        public string Range(int start)
            => Text.Substring(start);

        public string Range(int start, int end)
            => Text.Substring(start, end - start);

        public void AppendRangeTo(StringBuilder builder, int start)
            => builder.Append(Text, start, Text.Length - start);

        public void AppendRangeTo(StringBuilder builder, int start, int end)
            => builder.Append(Text, start, end - start);
    }

    private static readonly Regex TokenRegex = new(
        @"
            '    ( [^']  | ''   )*      ( '     | \z ) |    # string
            \[   ( [^\]] | \]\] )*      ( \]    | \z ) |    # quoted identifier
            --   .*?                    ( \r?\n | \z ) |    # line comment
            /\*  ( .     | \n   )*?     ( \*/   | \z ) |    # block comment
            \$\( (?<name> [\w-]+ )      ( \)    | \z ) |    # variable replacement
            ^GO                         ( \r?\n | \z ) |    # batch separator
            ^:(r|setvar) (?<args>                           # directives
                ( [^""\r\n]
                | \r (?!\n)
                | "" ( [^""] | """" )*  ( ""    | \z )
                )*
            )                           ( \r?\n | \z )
        ",
        Options
    );

    private static readonly Regex VariableRegex = new(
        @"
            \$\( (?<name>\w+) \)
        ",
        Options
    );

    private static readonly Regex IncludeRegex = new(
        @"
            \A [ \t]+

            (?<path> [^"" \t\r\n]+                      # unquoted
            |        "" ( [^""] | """" )* ( "" | \z )   # quoted
            )

            [ \t]* (-- .*)? \z
        ",
        Options
    );

    private static readonly Regex SetVariableRegex = new(
        @"
            \A [ \t]+

            (?<name> [\w-[0-9]] [\w-]* )

            (?> [ \t]+
                (?<value> [^"" \t\r\n]+                     # unquoted
                |         "" ( [^""] | """" )* ( "" | \z )  # quoted
                )
            )?

            [ \t]* (-- .*)? \z
        ",
        Options
    );

    private const RegexOptions Options
        = Multiline
        | IgnoreCase
        | CultureInvariant
        | IgnorePatternWhitespace
        | ExplicitCapture
        | Compiled;

    private const string
        DefaultInputName = "(script)";
}
