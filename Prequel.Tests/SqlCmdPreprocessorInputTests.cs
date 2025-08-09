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
        Invoking(() => new Input(null!, "any"))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Construct_NullText()
    {
        Invoking(() => new Input("any", null!))
            .Should().Throw<ArgumentNullException>();
    }
}
