# bat2exe

A small, self-contained **Batch-to-EXE wrapper**. Takes a `.bat` file, embeds it inside a tiny Windows PE stub, and produces a single standalone `.exe` that runs the batch script when executed.

Cross-builds on macOS or Linux via [Mono](https://www.mono-project.com/). The resulting `.exe` runs on any Windows install with .NET Framework 4.x (i.e. Windows 7 and newer).

## How it works

1. `Bat2Exe.cs` reads your `.bat` file byte-for-byte.
2. It compiles a small C# stub with the batch embedded as an assembly resource named `payload.bin`.
3. The resulting `.exe` — when run — extracts `payload.bin` to `%TEMP%`, runs it via `cmd.exe /c`, waits for exit, deletes the temp file, and propagates the exit code.

No installer, no runtime dependency at run time (the .NET Framework is preinstalled on every modern Windows).

## Requirements

**To build the compiler:**

- macOS / Linux: [Mono](https://www.mono-project.com/download/stable/) — `brew install mono` on macOS
- Windows: `.NET Framework 4.x` (ships with Windows; `csc.exe` is in `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\`)

**To run the generated `.exe`:**

- Windows 7 or newer (any version with .NET Framework 4.x)

## Build

```bash
chmod +x build.sh
./build.sh
