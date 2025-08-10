// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace Prequel;

using Input = SqlCmdPreprocessor.Input;

[TestFixture]
public class SqlCmdPreprocessorInputTests
{
    [Test]
    public void Construct_NullName()
    {
        Should.Throw<ArgumentNullException>(
            () => new Input(null!, "any")
        );
    }

    [Test]
    public void Construct_NullText()
    {
        Should.Throw<ArgumentNullException>(
            () => new Input("any", null!)
        );
    }

    [Test]
    public void Name()
    {
        new Input("Foo", "any")
            .Name.Should().Be("Foo");
    }
}
