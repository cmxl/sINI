# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```powershell
# Build entire solution
dotnet build Zini.slnx

# Run all tests
dotnet test Zini.slnx

# Run a single test by name
dotnet test tests/Zini.Tests --filter "Parse_SimpleSection_ReturnsKeyValues"

# Run samples
dotnet run --project samples/Zini.Sample.Parser/Zini.Sample.Parser.csproj
dotnet run --project samples/Zini.Sample.Configuration/Zini.Sample.Configuration.csproj

# Run benchmarks (Release mode required)
dotnet run -c Release --project benchmarks/Zini.Benchmarks/Zini.Benchmarks.csproj
```

## Solution Structure

```
src/
  Zini/                          — Core parser library (zero dependencies)
  Zini.Configuration/            — IConfigurationBuilder extension (AddConfigFile)
tests/
  Zini.Tests/                    — xUnit tests for ConfigParser
samples/
  Zini.Sample.Parser/            — Console app using ConfigParser directly
  Zini.Sample.Configuration/     — Console app using ConfigurationBuilder
benchmarks/
  Zini.Benchmarks/               — BenchmarkDotNet: Zini vs ini-parser
docs/
  config-format-spec.md          — Full format specification (comments, quoting, merging rules)
```

Uses `.slnx` (XML solution format). No `global.json`, `Directory.Build.props`, or `Directory.Packages.props` yet.

## Architecture

.NET 10 INI-style config file parser with a `Microsoft.Extensions.Configuration` integration.

All types in the core library are in the `Zini` namespace. Internal types (`State`, `SpecialChars`) are exposed to `Zini.Tests` via `InternalsVisibleTo`.

### Core parser (`src/Zini`)

**`ConfigParser.cs`** — Static `Parse(string)` and `Parse(ReadOnlySpan<char>)` overloads driving a state machine inside a `ref struct ParseContext`. Each parser state (`Data`, `ConfigSectionOpen`, `ConfigSectionClose`, `Key`, `Value`, `Comment`) has its own handler block in a `switch` over `State`. The design is zero-allocation on the hot path — all scanning uses `SearchValues<char>` for SIMD-accelerated bulk character search (`IndexOfAny`, `IndexOfAnyExcept`). Returns `FrozenDictionary` instances for true immutability. Uses `Dictionary.GetAlternateLookup<ReadOnlySpan<char>>()` to avoid string allocations on section cache hits.

**Key behaviors:**
- Sections are case-insensitive and merge on duplicate names (last-write-wins for keys)
- Global keys (before any section) go under the empty-string key `""`; the global section is created on demand and omitted if no global keys exist
- Quoted values (`"..."`) preserve whitespace and comment chars; `""` inside quotes escapes to `"`
- Inline comments (`#` or `;`) are stripped from unquoted values
- `FormatException` thrown for: `#`/`;` inside section names, mid-value `"`, unterminated quoted values, unclosed section brackets, and content after closing quote

**Output type:** `IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>` — outer key is section name, inner dict is key-value pairs. Both levels use `OrdinalIgnoreCase`.

### Configuration provider (`src/Zini.Configuration`)

Bridges `ConfigParser` to `Microsoft.Extensions.Configuration`. The provider flattens the two-level dict into `ConfigurationPath.Combine(section, key)` format (colon-delimited, e.g. `Server:port`). Global keys (empty section) map directly without a prefix.

- **`ConfigFileConfigurationExtensions`** — `AddConfigFile()` overloads on `IConfigurationBuilder`
- **`ConfigFileConfigurationProvider`** — `FileConfigurationProvider` subclass; reads stream → string → `ConfigParser.Parse`
- **`ConfigFileConfigurationSource`** — `FileConfigurationSource` subclass
