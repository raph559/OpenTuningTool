# OpenTuningTool
A high-performance, open-source ECU calibration suite. Features native XDF/BIN support, heuristic map discovery, hardware-accelerated 3D visualization, and integrated Git-based version control. Built on .NET 10 for modern speed and reliability.

## Building and Running

### Prerequisites
- .NET 10.0 SDK or later
- Windows operating system (for running the application)

### Build Instructions
1. Restore dependencies:
   ```bash
   dotnet restore
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

The application will open a Windows Forms window that can be closed using the standard window close button (X) in the title bar.

## Project Structure
- `Program.cs` - Main entry point for the application
- `Form1.cs` - Main window form
- `Form1.Designer.cs` - Designer-generated code for the form
- `OpenTuningTool.csproj` - Project configuration file
