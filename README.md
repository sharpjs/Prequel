# Prequel

Prequel is a minimal SQLCMD-compatible preprocessor.

- Adds a 'SQLCMD mode' to your project with a couple lines of code.
- Supports `GO`, `$(var)`, `:setvar`, and `:r`.
- Does not require the SQLCMD utility to be present.

## Status

[![Build](https://github.com/sharpjs/Prequel/workflows/Build/badge.svg)](https://github.com/sharpjs/Prequel/actions)
[![NuGet](https://img.shields.io/nuget/v/Prequel.svg)](https://www.nuget.org/packages/Prequel)
[![NuGet](https://img.shields.io/nuget/dt/Prequel.svg)](https://www.nuget.org/packages/Prequel)

- **Stable:**     in private use for years with very few reported defects.
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
:setvar Columns "Id, Name"  -- this also works
```

### `:r` — include a file

```sql
:r OtherFile.sql     -- this works
:r "Other File.sql"  -- this also works
```

<!--
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
-->
