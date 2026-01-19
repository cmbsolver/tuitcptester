# TUI TCP Tester

A terminal-based utility for testing TCP connections, supporting both client and server roles. Built with .NET and `Terminal.Gui`, this tool is designed for testing firewall connections and network reliability directly from the command line on servers or clients.

## Features

- **Server & Client Modes**: Easily set up a listening TCP server or a connecting TCP client.
- **Auto-Transactions**: 
    - Load a list of messages to be sent automatically upon connection.
    - Support for periodic sending (Interval in ms).
    - **Jitter Support**: Randomize the interval between transactions for more realistic testing.
    - **Sequential Sending**: Option to send the next transaction only upon receiving a response.
- **Manual Messaging**: Send custom data in ASCII, Hex, or Binary (Base64) formats.
- **Control Characters**: Option to append `\r` (Return) and/or `\n` (Newline) to outgoing messages.
- **File Integration**: Load transaction lists directly from text files.
- **Logging**: Real-time logs of all sent and received data, with an option to clear them.
- **Themes**: Multiple UI color schemes including Green Screen (Default), Cyberpunk, Solarized Dark, and more.
- **Ping Tool**: Built-in utility to check network reachability.
- **Persistence**: Save and load connection configurations for quick reuse.

## Keyboard Shortcuts

| Key | Action |
| --- | --- |
| **F2** | New TCP Server |
| **F3** | New TCP Client |
| **F4** | Stop Selected Connection |
| **F5** | Start Selected Connection |
| **F6** | Remove Selected Connection |
| **F7** | Manual Send Message |
| **F8** | Ping IP |
| **F9** | Clear Logs |
| **Ctrl+Q**| Quit Application |

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later.

### Installation & Build

1. Clone the repository.
2. Navigate to the project directory.
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run
   ```

### Command Line Arguments

You can pass a path to a configuration file as an argument to load it automatically on startup:
```bash
dotnet run -- /path/to/config.json
```

## Documentation

The project includes XML documentation comments for all major components. To generate the documentation file, build the project, and find the XML file in the output directory:
`bin/Debug/net10.0/tuitcptester.xml`

This file can be used with tools like Sandcastle to generate comprehensive API documentation.

## Project Structure

- **Logic**: Core TCP connection management and communication handling.
- **Models**: Data structures for configurations, transactions, and logs.
- **UI**: Terminal.Gui implementation, organized into partial classes for better readability.
- **Program.cs**: Application entry point.
