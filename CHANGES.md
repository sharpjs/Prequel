# Changes in Prequel
This file documents all notable changes.

Most lines should begin with one of these words:
*Add*, *Fix*, *Update*, *Change*, *Deprecate*, *Remove*.

## [Unreleased](https://github.com/sharpjs/Prequel/compare/release/1.0.3..HEAD)
- Change license from ISC to MIT.
- Add .NET 8.0 target framework.
- Add optional variable replacement in comments.  Set the `SqlCmdPreprocessor`
    property `EnableVariableReplacementInComments` to `true` to enable this feature.
- Fix `-` not recognized in `$(…)` syntax.

<!--
## [1.1.0](https://github.com/sharpjs/Prequel/compare/release/1.0.3..release/1.1.0)
-->

## [1.0.3](https://github.com/sharpjs/Prequel/compare/release/1.0.2..release/1.0.3)
- Fix packaging issues:
  - Enable deterministic build.
  - Embed untracked sources in symbols package.

## [1.0.2](https://github.com/sharpjs/Prequel/compare/release/1.0.1..release/1.0.2)
- Fix incorrect result or index-out-of-range exception when multiple variable 
  replacements occur in a quoted string or in a quoted identifier.

## [1.0.1](https://github.com/sharpjs/Prequel/compare/release/1.0.0..release/1.0.1)
- Fix missing IntelliSense.

## [1.0.0](https://github.com/sharpjs/Prequel/tree/release/1.0.0)
Initial release.

<!--
  Copyright Subatomix Research Inc.
  SPDX-License-Identifier: MIT
-->
