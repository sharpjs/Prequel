/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

using System.Text;

namespace Prequel;

[TestFixture]
public class SqlCmdPreprocessorTests
{
    [Test]
    public void Variables_Initial()
    {
        new SqlCmdPreprocessor()
            .Variables.Should().BeEmpty();
    }

    [Test]
    public void Process_NullText()
    {
        new SqlCmdPreprocessor()
            .Invoking(p => p.Process(null!))
            .Should().Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "text");
    }

    [Test]
    public void Process_EmptyText()
    {
        var preprocessor = new SqlCmdPreprocessor();

        var batches = preprocessor.Process("");

        batches.Should().BeEmpty();
    }

    [Test]
    public void Process_TokenlessText()
    {
        var preprocessor = new SqlCmdPreprocessor();

        var text = "no tokens here";

        var batches = preprocessor.Process(text);

        batches.Should().Equal(text);
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_LineComment(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor();

        var text = Lines(eol, eof, "a", "b -- foo", "c");

        var batches = preprocessor.Process(text);

        batches.Should().Equal(text);
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_BlockComment(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor();

        var text = Lines(eol, eof, "a /* foo", "bar */ b");

        var batches = preprocessor.Process(text);

        batches.Should().Equal(text);
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_BlockComment_Unterminated(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor();

        var text = Lines(eol, eof, "a /* foo");

        var batches = preprocessor.Process(text);

        batches.Should().Equal(text);
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_SingleQuotedString(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor();

        var text = Lines(eol, eof, "a 'foo''bar", "baz' b");

        var batches = preprocessor.Process(text);

        batches.Should().Equal(text);
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_SingleQuotedString_Unterminated(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor();

        var text = Lines(eol, eof, "a 'foo''bar");

        var batches = preprocessor.Process(text);

        batches.Should().Equal(text);
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_BracketQuotedIdentifier(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor();

        var text = Lines(eol, eof, "a [foo]]bar", "baz] b");

        var batches = preprocessor.Process(text);

        batches.Should().Equal(text);
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_BracketQuotedIdentifier_Unterminated(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor();

        var text = Lines(eol, eof, "a [foo]]bar");

        var batches = preprocessor.Process(text);

        batches.Should().Equal(text);
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_BatchSeparator(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor();

        foreach (var separator in BatchSeparators)
        {
            var text = Lines(eol, eof, "a", separator, "b");

            var batches = preprocessor.Process(text);

            batches.Should().Equal("a" + eol, "b" + eof);
        }
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_BatchSeparator_Repeated(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor();

        foreach (var separator in BatchSeparators)
        {
            var text = Lines(eol, eof, "a", separator, separator, "b");

            var batches = preprocessor.Process(text);

            batches.Should().Equal("a" + eol, "b" + eof);
        }
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_BatchSeparator_EmptyBatchAtEnd(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor();

        foreach (var separator in BatchSeparators)
        {
            var text = Lines(eol, eof, "a", separator);

            var batches = preprocessor.Process(text);

            batches.Should().Equal("a" + eol);
        }
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_BatchSeparator_AllEmptyBatches(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor();

        foreach (var separator in BatchSeparators)
        {
            var text = Lines(eol, eof, separator, separator);

            var batches = preprocessor.Process(text);

            batches.Should().BeEmpty();
        }
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_VariableReplacement_Normal(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["foo"] = "bar" }
        };

        var batches = preprocessor.Process(
            Lines(eol, eof, "a $(FOO) b $(foo) c")
        );

        batches.Should().Equal(
            Lines(eol, eof, "a bar b bar c")
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_VariableReplacement_WithComments(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["foo"] = "bar" }
        };

        var batches = preprocessor.Process(
            Lines(eol, eof, "a $(FOO) b /* $(FOO) */ $(FOO) -- $(FOO)")
        );

        batches.Should().Equal(
            Lines(eol, eof, "a bar b /* $(FOO) */ bar -- $(FOO)")
        );
    }

    [Test]
    public void Process_VariableReplacement_Undefined()
    {
        var preprocessor = new SqlCmdPreprocessor { };

        preprocessor
            .Invoking(p => p.Process("$(foo)").Count())
            .Should().Throw<SqlCmdException>()
            .WithMessage("SqlCmd variable 'foo' is not defined.");
    }

    [Test]
    public void Process_VariableReplacement_Unterminated()
    {
        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["foo"] = "bar" }
        };

        preprocessor
            .Invoking(p => p.Process("$(foo").Count())
            .Should().Throw<SqlCmdException>()
            .WithMessage("Unterminated reference to SqlCmd variable 'foo'.");
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_VariableReplacement_InSingleQuotedString(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["foo"] = "bar" }
        };

        var batches = preprocessor.Process(
            Lines(eol, eof, "a 'b '' $(fOO) $(Foo) c' d")
        );

        batches.Should().Equal(
            Lines(eol, eof, "a 'b '' bar bar c' d")
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_VariableReplacement_InBracketQuotedIdentifier(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["foo"] = "bar" }
        };

        var batches = preprocessor.Process(
            Lines(eol, eof, "a [b ]] $(fOo) $(foO) c] d")
        );

        batches.Should().Equal(
            Lines(eol, eof, "a [b ]] bar bar c] d")
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_SetVariable_Unset(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["foo"] = "bar" }
        };

        var text = Lines(eol, eof, ":setvar foo", "$(foo)");

        preprocessor
            .Invoking(p => p.Process(text).Count())
            .Should().Throw<SqlCmdException>()
            .WithMessage("SqlCmd variable 'foo' is not defined.");

        preprocessor.Variables.Should().NotContainKey("foo");
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_SetVariable_Unquoted(string eol, string eof)
    {
        // NOTE: This test contains German, Japanese, and Russian characters.

        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["f0ö_Бар-baß"] = "original value" }
        };

        var batches = preprocessor.Process(
            Lines(eol, eof,
                @"a",
                @":setvar f0ö_Бар-baß qux~`!@#$%^&*()-_=+[{]}\|;:',<.>/?ほげ",
                @"b $(F0ö_Бар-baß) c"
            )
        );

        batches.Should().Equal(
            Batch(
                @"a",                                         eol,
                @"b qux~`!@#$%^&*()-_=+[{]}\|;:',<.>/?ほげ c", eof
            )
        );

        preprocessor.Variables["f0ö_Бар-baß"].Should().Be(
            @"qux~`!@#$%^&*()-_=+[{]}\|;:',<.>/?ほげ"
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_SetVariable_Unquoted_WithComment(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["foo"] = "original value" }
        };

        var batches = preprocessor.Process(
            Lines(eol, eof,
                @"a",
                @":setvar foo bar/**/ -- comment",
                @"b $(FOO) c"
            )
        );

        batches.Should().Equal(
            Batch(
                @"a",           eol,
                @"b bar/**/ c", eof
            )
        );

        preprocessor.Variables["foo"].Should().Be("bar/**/");
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_SetVariable_Unquoted_Invalid(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor { };

        var text = Lines(eol, eof, ":setvar foo bar baz");

        preprocessor
            .Invoking(p => p.Process(text).Count())
            .Should().Throw<SqlCmdException>()
            .WithMessage("Invalid syntax in :setvar directive.");

        preprocessor.Variables.Should().NotContainKey("foo");
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_SetVariable_DoubleQuoted(string eol, string eof)
    {
        // NOTE: This test contains German, Japanese, and Russian characters.

        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["f0ö_Бар-baß"] = "original value" }
        };

        var batches = preprocessor.Process(
            Lines(eol, eof,
                @"a",
                @":setvar f0ö_Бар-baß ""qux ~`!@#$%^&*()-_=+[{]}\|;:'"""",<.>/? corge ",
                @"",
                @"「ほげ」 grault""",
                @"b $(F0ö_Бар-baß) c"
            )
        );

        batches.Should().Equal(
            Lines(eol, eof,
                @"a",
                @"b qux ~`!@#$%^&*()-_=+[{]}\|;:'"",<.>/? corge ",
                @"",
                @"「ほげ」 grault c"
            )
        );

        preprocessor.Variables["f0ö_Бар-baß"].Should().Be(
            Batch(
                @"qux ~`!@#$%^&*()-_=+[{]}\|;:'"",<.>/? corge ", eol,
                @"",                                             eol,
                @"「ほげ」 grault"
            )
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_SetVariable_DoubleQuoted_WithComment(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["foo"] = "original value" }
        };

        var batches = preprocessor.Process(
            Lines(eol, eof,
                @"a",
                @":setvar foo ""bar/**/"" -- comment",
                @"b $(FOO) c"
            )
        );

        batches.Should().Equal(
            Batch(
                @"a",           eol,
                @"b bar/**/ c", eof
            )
        );

        preprocessor.Variables["foo"].Should().Be("bar/**/");
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_SetVariable_DoubleQuoted_Unterminated(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor { };

        var text = Lines(eol, eof, @":setvar foo ""bar");

        preprocessor
            .Invoking(p => p.Process(text).Count())
            .Should().Throw<SqlCmdException>()
            .WithMessage("Unterminated double-quoted string.");

        preprocessor.Variables.Should().NotContainKey("foo");
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_Include_Unquoted(string eol, string eof)
    {
        using var file = new TemporaryFile();

        file.Write(Lines(eol, eof,
            "included"
        ));

        var preprocessor = new SqlCmdPreprocessor { };

        var batches = preprocessor.Process(
            Lines(eol, eof, "a", $":r {file.Path}", "b")
        );

        batches.Should().Equal(
            Batch(
                "a",        eol,
                "included", eof,
                "b",        eof
            )
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_Include_Unquoted_WithComment(string eol, string eof)
    {
        using var file = new TemporaryFile();

        file.Write(Lines(eol, eof,
            "included"
        ));

        var preprocessor = new SqlCmdPreprocessor { };

        var batches = preprocessor.Process(
            Lines(eol, eof, "a", $":r {file.Path} -- comment", "b")
        );

        batches.Should().Equal(
            Batch(
                "a",        eol,
                "included", eof,
                "b",        eof
            )
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_Include_Unquoted_Invalid(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor { };

        var text = Lines(eol, eof, ":r foo bar baz");

        preprocessor
            .Invoking(p => p.Process(text).Count())
            .Should().Throw<SqlCmdException>()
            .WithMessage("Invalid syntax in :r directive.");
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_Include_DoubleQuoted_Normal(string eol, string eof)
    {
        using var file = new TemporaryFile();

        file.Write(Lines(eol, eof,
            "included"
        ));

        var preprocessor = new SqlCmdPreprocessor { };

        var batches = preprocessor.Process(
            Lines(eol, eof, "a", $@":r ""{file.Path}""", "b")
        );

        batches.Should().Equal(
            Batch(
                "a",        eol,
                "included", eof,
                "b",        eof
            )
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_Include_DoubleQuoted_WithComment(string eol, string eof)
    {
        using var file = new TemporaryFile();

        file.Write(Lines(eol, eof,
            "included"
        ));

        var preprocessor = new SqlCmdPreprocessor { };

        var batches = preprocessor.Process(
            Lines(eol, eof, "a", $@":r ""{file.Path}"" -- comment", "b")
        );

        batches.Should().Equal(
            Batch(
                "a",        eol,
                "included", eof,
                "b",        eof
            )
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_Include_DoubleQuoted_Unterminated(string eol, string eof)
    {
        var preprocessor = new SqlCmdPreprocessor { };

        var text = Lines(eol, eof, @":r ""bar");

        preprocessor
            .Invoking(p => p.Process(text).Count())
            .Should().Throw<SqlCmdException>()
            .WithMessage("Unterminated double-quoted string.");
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_Include_Nested(string eol, string eof)
    {
        using var file0 = new TemporaryFile();
        using var file1 = new TemporaryFile();

        file0.Write(Lines(eol, eof,
            "b",
            $":r {file1.Path}",
            "c"
        ));

        file1.Write(Lines(eol, eof,
            "included"
        ));

        var preprocessor = new SqlCmdPreprocessor { };

        var batches = preprocessor.Process(
            Lines(eol, eof, "a", $":r {file0.Path}", "d")
        );

        batches.Should().Equal(
            Batch(
                "a",        eol,
                "b",        eol,
                "included", eof,
                "c",        eof,
                "d",        eof
            )
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_Include_CrossInclusionBatches(string eol, string eof)
    {
        using var file0 = new TemporaryFile();
        using var file1 = new TemporaryFile();

        file0.Write(Lines(eol, eof,
            "file0.a",
            "GO",
            "file0.b"
        ));

        file1.Write(Lines(eol, eof,
            "file1.a",
            "go",
            "file1.b"
        ));

        var preprocessor = new SqlCmdPreprocessor { };

        var batches = preprocessor.Process(Lines(eol, eof,
            "beg",
            $":r {file0.Path}",
            "mid",
            $":r {file1.Path}",
            "end"
        ));

        batches.Should().Equal(
            // Batch begins in top level and ends in included
            Batch(
                "beg",     eol,
                "file0.a", eol
            ),
            // Batch begins in included, continues to top level, and ends in another included
            Batch(
                "file0.b", eof,
                "mid",     eol,
                "file1.a", eol
            ),
            // Batch begins in included and ends in top level
            Batch(
                "file1.b", eof,
                "end",     eof
            )
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_Include_BatchInsideInclude(string eol, string eof)
    {
        using var file = new TemporaryFile();

        file.Write(Lines(eol, eof,
            "file.a",
            "GO",
            "file.b",
            "go",
            "file.c"
        ));

        var preprocessor = new SqlCmdPreprocessor { };

        var batches = preprocessor.Process(Lines(eol, eof,
            "beg",
            $":r {file.Path}",
            "end"
        ));

        batches.Should().Equal(
            // Batch begins in top level and ends in included
            Batch(
                "beg",    eol,
                "file.a", eol
            ),
            // Batch begins in included and ends in same included
            Batch(
                "file.b", eol
            ),
            // Batch begins in included and ends in top level
            Batch(
                "file.c", eof,
                "end",    eof
            )
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_Include_BatchSeparatorBeforeInclusionBoundary(string eol, string eof)
    {
        using var file = new TemporaryFile();

        file.Write(
            Lines(eol, eof,
                "included",
                "GO"
            )
        );

        var preprocessor = new SqlCmdPreprocessor { };

        var batches = preprocessor.Process(
            Lines(eol, eof,
                "GO",
                $":r {file.Path}",
                "main"
            )
        );

        batches.Should().Equal(
            Batch("included", eol),
            Batch("main",     eof)
        );
    }

    [Test]
    [TestCaseSource(nameof(EolEofCases))]
    public void Process_Include_BatchSeparatorAfterInclusionBoundary(string eol, string eof)
    {
        using var file = new TemporaryFile();

        file.Write(
            Lines(eol, eof,
                "GO",
                "included"
            )
        );

        var preprocessor = new SqlCmdPreprocessor { };

        var batches = preprocessor.Process(
            Lines(eol, eof,
                $":r {file.Path}",
                "GO",
                "main"
            )
        );

        batches.Should().Equal(
            Batch("included", eof),
            Batch("main",     eof)
        );
    }

    [Test]
    [TestCase(@"",           @""    )]
    [TestCase(@"a",          @"a"   )]
    [TestCase(@"""""",       @""    )]
    [TestCase(@"""a""",      @"a"   )]
    [TestCase(@"""a""""b""", @"a""b")]
    public void Unquote(string input, string expected)
    {
        SqlCmdPreprocessor.Unquote(input).Should().Be(expected);
    }

    [Test]
    [TestCase(@"""")]
    [TestCase(@"""a")]
    public void Unquote_Unterminated(string input)
    {
        Invoking(() => SqlCmdPreprocessor.Unquote(input))
            .Should().Throw<SqlCmdException>()
            .WithMessage("Unterminated double-quoted string.");
    }

    [Test]
    [TestCase(0)]
    [TestCase(MinimumBuilderCapacity - 1)]
    [TestCase(MinimumBuilderCapacity)]
    public void GetPreferredBuilderCapacity_NotAboveMinimum(int length)
    {
        SqlCmdPreprocessor.GetPreferredBuilderCapacity(length)
            .Should().Be(MinimumBuilderCapacity);
    }

    [Test]
    public void GetPreferredBuilderCapacity_AboveMinimum()
    {
        const int Length = MinimumBuilderCapacity + 1;

        SqlCmdPreprocessor.GetPreferredBuilderCapacity(Length)
            .Should().BeGreaterThanOrEqualTo(Length);
    }

    private const int MinimumBuilderCapacity
        = SqlCmdPreprocessor.MinimumBuilderCapacity;

    private static readonly string[][] EolEofCases =
    {
        //         EOL     EOF
        new[] {   "\n",     "" },
        new[] { "\r\n",     "" },
        new[] {   "\n",   "\n" },
        new[] { "\r\n", "\r\n" }
    };

    private static readonly string[] BatchSeparators =
    {
        "GO", "Go", "gO", "go"
    };

    private static string Lines(string eol, string eof, params string[] lines)
    {
        var text = new StringBuilder(capacity: 64);

        if (lines.Length > 0)
            text.Append(lines[0]);

        for (var i = 1; i < lines.Length; i++)
            text.Append(eol).Append(lines[i]);

        return text.Append(eof).ToString();
    }

    private static string Batch(params string[] args)
        => string.Concat(args);
}
