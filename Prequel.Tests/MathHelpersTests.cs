// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace Prequel;

[TestFixture]
public class MathHelpersTests
{
    [Test]
    [TestCase(-0x8000_0000,           1)]
    [TestCase(           0,           1)]
    [TestCase(           1,           1)]
    [TestCase(           2,           2)]
    [TestCase(           3,           4)]
    [TestCase(           4,           4)]
    [TestCase(           5,           8)]
    [TestCase( 0x3FFF_FFFF, 0x4000_0000)]
    [TestCase( 0x4000_0000, 0x4000_0000)]
    [TestCase( 0x4000_0001, 0x7FFF_FFFF)]
    [TestCase( 0x7FFF_FFFE, 0x7FFF_FFFF)]
    [TestCase( 0x7FFF_FFFF, 0x7FFF_FFFF)]
    public void GetNextPowerOf2Saturating(int input, int expected)
    {
        MathHelpers.GetNextPowerOf2Saturating(input).Should().Be(expected);
    }
}
