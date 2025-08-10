# Prequel

Prequel is a minimal SQLCMD-compatible preprocessor.

- Adds a 'SQLCMD mode' to your project with a couple lines of code.
- Supports `GO`, `$(var)`, `:setvar`, and `:r`.
- Does not require the SQLCMD utility to be present.

## Status

[![Build](https://github.com/sharpjs/Prequel/workflows/Build/badge.svg)](https://github.com/sharpjs/Prequel/actions)
[![NuGet](https://img.shields.io/nuget/v/Prequel.svg)](https://www.nuget.org/packages/Prequel)
[![NuGet](https://img.shields.io/nuget/dt/Prequel.svg)](https://www.nuget.org/packages/Prequel)

- **Stable:**     in public and private use for years with very few reported defects.
- **Tested:**     100% coverage by automated tests.
- **Documented:** IntelliSense on everything.

## Installation

Install [this NuGet Package](https://www.nuget.org/packages/Prequel) in your project.

## Usage

SQL in, preprocessed batches out — it's as simple as that.

```csharp
// Import the namespace
using Prequel;

// Create a preprocessor
var preprocessor = new SqlCmdPreprocessor();

// Optional: set some preprocessor variables
preprocessor.Variables["Foo"] = "Bar";

// Preprocess!
var batches = preprocessor.Process(sql);

// Do something with the batches
foreach (var batch in batches)
{
    // ...
}
```

## SQLCMD Feature Support

Prequel supports a limited subset of
[SQLCMD](https://docs.microsoft.com/en-us/sql/tools/sqlcmd-utility)
preprocessing features.

### `GO` — batch separator

```sql
SELECT * FROM Foo; -- first batch
GO
SELECT * FROM Bar; -- second batch
```

### `$(var)` — preprocessor variable expansion

```sql
SELECT $(Columns) FROM Foo;
```

### `:setvar` — set a preprocessor variable

```sql
:setvar Columns Name        -- this works
:setvar Columns "Id, Name"  -- also works; required if value contains space
```

### `:r` — include a file

```sql
:r OtherFile.sql     -- this works
:r "Other File.sql"  -- also works; required if path contains space
```

### Notes

Preprocessor directives are case-insensitive.

A `GO` batch separator must appear the the beginning of a line.
No other content may appear on that line.

A `:setvar` or `:r` directive must appear at the beginning of a line.
An optional line comment may follow the directive.

`$(…)` may appear anywhere, including inside other preprocessor directives.

<!--
  Copyright Subatomix Research Inc.
  SPDX-License-Identifier: MIT
-->
