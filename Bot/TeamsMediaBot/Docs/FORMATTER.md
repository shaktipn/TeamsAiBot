# Code Formatter Setup

This project uses **dotnet format** with comprehensive .editorconfig rules for consistent code style.

## Quick Start

### Format all files:
```bash
./format.sh
```

Or directly:
```bash
/usr/local/share/dotnet/dotnet format
```

### Check formatting without making changes:
```bash
/usr/local/share/dotnet/dotnet format --verify-no-changes
```

## What Gets Formatted

- Code style (indentation, spacing, braces)
- Naming conventions (PascalCase, camelCase, _privateFields)
- Unused using directives are automatically removed
- Import ordering (System.* namespaces first)
- Modern C# patterns and best practices
