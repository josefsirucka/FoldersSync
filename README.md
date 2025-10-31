# MyFolderSync

A simple command-line folder synchronization tool built with .NET 9 that provides automated, one-way file synchronization between source and target directories.

## Overview

MyFolderSync is a windows platform console application that monitors and synchronizes folders at configurable intervals. It performs intelligent file comparison using MD5 hashing, file size, and modification time to ensure accurate synchronization.

What I would be implementing in the future (know problems/issues):

### Performance & Approaches
- Realtime Watcher would be better solution
- Possibly could be extended to MacOS / Linux
- NTFS privileges are completely ignored (I expect that application will be launched with the correct windows user).

### Possible problems
- Checks if the target folder (disk) has proper free space for that backup
- Unix folders (fodlers that starts with //folder)

### Minor bugs
- Console reporting is not behaving correctly when it is small (automatic calculation if the window's width would be nice)

### Key Features

- **One-way synchronization** from source to target folders
- **Configurable sync intervals** (default: 1 hour)
- **Multiple folder pair support** - sync multiple source-target pairs simultaneously
- **Intelligent file comparison** using MD5 hash, file size, and last modified time
- **Comprehensive logging** with configurable log levels and file output
- **Cross-platform compatibility** (.NET 9)

## Installation

### Prerequisites

- [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) or higher

### Building from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/josefsirucka/FoldersSync.git
   cd FoldersSync
   ```

2. Build the application:
   ```bash
   dotnet build --configuration Release
   ```

## Usage

### Basic Syntax

```bash
MyFolderSync -f "source_folder=>target_folder" [options]
```

### Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--folders` | `-f` | **Required.** Folder pairs for synchronization. Format: `source=>target` | - |
| `--interval` | `-i` | Synchronization interval in seconds | `3600` (1 hour) |
| `--level` | `-l` | Log level (Debug, Information, Warning, Error, Fatal) | `Information` |
| `--path` | `-p` | Log file path and name | `log/FolderSync.log` |

### Examples

#### Single Folder Pair
```bash
MyFolderSync -f "C:\Source\Documents=>D:\Backup\Documents"
```

#### Multiple Folder Pairs
```bash
MyFolderSync -f "C:\Photos=>D:\Backup\Photos" "C:\Videos=>D:\Backup\Videos"
```

#### Custom Configuration
```bash
MyFolderSync -f "C:\Data=>\\NetworkShare\Backup" -i 1800 -l Debug -p "logs\sync.log"
```

#### Sync Every 30 Minutes with Verbose Logging
```bash
MyFolderSync -f "C:\Projects=>D:\ProjectBackups" --interval 1800 --level Debug
```

## How It Works

1. **Validation**: Verifies that source folders exist and target folders are accessible
2. **File Discovery**: Scans both source and target directories
3. **Comparison**: Analyzes files using:
   - MD5 hash comparison
   - File size verification
   - Last modified timestamp
4. **Synchronization**: Performs three operations:
   - **Copy**: New files from source to target
   - **Update**: Modified files in source
   - **Delete**: Files that no longer exist in source
5. **Logging**: Records all operations with timestamps and details

## Logging

The application uses [Serilog](https://serilog.net/) for comprehensive logging:

- **Console Output**: Real-time status updates
- **File Logging**: Detailed logs with daily rolling files
- **Configurable Levels**: From Debug to Fatal
- **Structured Logging**: Machine-readable log format

### Log Levels

- **Debug**: Detailed diagnostic information
- **Information**: General application flow (default)
- **Warning**: Potentially harmful situations
- **Error**: Error events that allow continued execution
- **Fatal**: Critical errors that may cause termination

## Project Structure

```
MyFolderSync/
- Arguments/         # Command-line argument handling
- Commands/          # Sync command implementations
- Extensions/        # Utility extensions (MD5, file operations)
- Helpers/           # Core abstractions (IFile, IFolder, Resolver)
- Services/          # Business logic (SyncService, TimerService)
- MyFolderSync.Tests/  # Few Unit tests
```
## Formatter

I use csharpier to format my code: https://csharpier.com/

## Dependencies

- **CommandLineParser** (2.9.1) - Command-line argument parsing
- **Serilog** (4.3.0) - Structured logging
- **PerfectResult** (1.0.3) - Result pattern implementation

## Safety Features

- **Path Validation**: Prevents synchronization between identical folders
- **Graceful Shutdown**: ESC key stops synchronization safely
- **Error Recovery**: Continues processing other folder pairs if one fails
- **Duplicate Prevention**: Warns about and skips duplicate source folder mappings

## Author

**Josef Širůčka** - [GitHub](https://github.com/josefsirucka)