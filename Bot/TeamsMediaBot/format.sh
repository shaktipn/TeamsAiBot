#!/bin/bash

# Format script for TeamsMediaBot
# This script formats all C# files and removes unused usings

DOTNET="/usr/local/share/dotnet/dotnet"

echo "======================================"
echo "   TeamsMediaBot Code Formatter"
echo "======================================"
echo ""

# Check if dotnet is available
if [ ! -f "$DOTNET" ]; then
    echo "Error: dotnet not found at $DOTNET"
    exit 1
fi

echo "Running dotnet format..."
echo ""

$DOTNET format --verbosity detailed

FORMAT_EXIT_CODE=$?

echo ""
if [ $FORMAT_EXIT_CODE -eq 0 ]; then
    echo "✓ Formatting completed successfully!"
else
    echo "✗ Formatting encountered issues. Exit code: $FORMAT_EXIT_CODE"
    exit $FORMAT_EXIT_CODE
fi

echo ""
echo "======================================"
