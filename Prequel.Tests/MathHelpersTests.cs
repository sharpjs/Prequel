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
