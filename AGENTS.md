# AGENTS.md - Gazer4.0 Development Guide

This document provides guidelines and commands for agentic coding agents working on the Gazer4.0 project.

## Project Overview

Gazer4.0 is a WPF image viewer application built with .NET 9.0. It features camera shake effects, pulse effects, and playlist management. The project is written in C# with XAML for UI.

## Build Commands

### Build the Project
```bash
dotnet build Gazer4.0/Gazer4.0.csproj -c Release
dotnet build Gazer4.0/Gazer4.0.csproj -c Debug
```

### Build Self-Contained Executable
```bash
dotnet build Gazer4.0/Gazer4.0.csproj -c Release -r win-x64 --self-contained
```

### Clean Build Artifacts
```bash
dotnet clean Gazer4.0/Gazer4.0.csproj
```

### Restore Dependencies
```bash
dotnet restore Gazer4.0/Gazer4.0.csproj
```

## Linting and Formatting

This project does not have explicit linting configured. When adding new features:
- Use `dotnet format` if installed to auto-format code
- Ensure consistent indentation (4 spaces)
- Remove unused using statements before committing

## Testing

**Note:** This project currently has no unit tests. If tests are added in the future:
```bash
dotnet test                    # Run all tests
dotnet test --filter "TestName" # Run single test
dotnet test --verbosity normal  # Detailed output
```

## Code Style Guidelines

### General Principles
- Write code that is clear and maintainable
- Add Chinese comments for public APIs and complex logic (matching existing codebase style)
- Keep methods focused and under 100 lines when possible

### Naming Conventions
- **Classes/Public Members**: PascalCase (e.g., `MainWindow`, `AppSettings`, `CurrentLanguage`)
- **Private Fields**: camelCase with underscore prefix for complex state (e.g., `_imageStates`, `autoPlayTimer`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `PLAYLIST_EXTENSION`, `REGISTRY_KEY`)
- **Interfaces**: Prefix with `I` (e.g., `IImageLoader`)
- **XAML Controls**: camelCase (e.g., `imgDisplay`, `notificationText`)

### File Organization
- One public class per file (matching existing pattern)
- File name matches class name exactly
- Keep related functionality together (e.g., playlist management in `PlaylistManager.cs`)

### Language Features
- **Nullable**: Enabled - use `?` for nullable reference types
- **Implicit Usings**: Enabled - do not include redundant `using` statements
- **Target Framework**: net9.0-windows
- **Use `var`**: Prefer `var` when type is obvious from right side
- **Target-Typed `new`**: Prefer `new()` when type is known from context

### Property Declaration
```csharp
// Preferred style in this codebase
public bool AutoSizeWindow { get; set; } = true;
public double ShakeAmount { get; set; } = 20;

// Avoid long property blocks; keep them concise
```

### Error Handling
- Use try-catch blocks for file I/O and serialization operations
- Log errors appropriately using `System.Diagnostics.Debug.WriteLine`
- Handle null states gracefully (the codebase uses nullable enable)
- Example pattern from Settings.cs:
```csharp
try
{
    // Operation that might fail
}
catch
{
    // Handle or log error; empty catch is acceptable for non-critical operations
}
```

### Import Organization
Organize imports in the following order:
1. System namespaces
2. System.Windows/PresentationCore namespaces
3. Third-party libraries (if any)
4. Microsoft namespaces

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
```

### XAML Guidelines
- Use meaningful names for XAML elements
- Keep XAML readable with proper indentation
- Use StaticResource for shared resources
- Match control naming convention: `camelCase` for x:Name

### Documentation Comments
- Add XML documentation for public classes and methods
- Use Chinese comments for user-facing strings (matching existing i18n pattern)
- Document parameters and return values for complex methods

### WPF Specific Patterns
- Use DispatcherTimer for UI updates (as seen in `autoPlayTimer`, `notificationTimer`)
- Keep UI logic in code-behind for simple scenarios
- Use data binding for MVVM patterns when appropriate
- Handle DPI awareness for image rendering

### Internationalization
- All user-facing strings go through `LanguageManager.GetText()`
- Do not hardcode strings in UI; use resource keys
- Supported languages: Chinese, Japanese, English

## Project Structure

```
Gazer4.0/
├── MainWindow.xaml          # Main application window
├── MainWindow.xaml.cs       # Main window logic (84KB)
├── Settings.cs              # Application settings management
├── PlaylistManager.cs       # Playlist save/load operations
├── ShakePreset.cs           # Camera shake preset handling
├── LanguageManager.cs       # Multi-language support
├── App.xaml                 # Application resources
├── AssemblyInfo.cs          # Assembly metadata
├── Gazer4.0.csproj          # Project file
└── favicon.ico              # Application icon
```

## Common Development Tasks

### Running the Application
```bash
dotnet run --project Gazer4.0/Gazer4.0.csproj
```
Or open `Gazer4.0.sln` in Visual Studio 2022 and press F5.

### Modifying Settings
Settings are persisted to `%APPDATA%/ImageViewer/settings.xml` using XML serialization.

### Modifying Playlists
Playlists use `.gzpl` extension and JSON format with `PlaylistFile`, `PlaylistItem`, and related classes.

## Important Notes

- This is a Windows-only WPF application
- No automated tests exist - manual testing required
- The application uses file system paths extensively; handle path edge cases
- Images can be in various formats: JPG, PNG, BMP, GIF, TIFF
- Camera effects (shake, pulse) operate on TransformGroups in the UI
