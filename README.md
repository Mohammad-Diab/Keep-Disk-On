# Keep Disk On

A small Windows tool that keeps a USB flash drive or an external hard disk awake, so it doesn't go to sleep or spin down while you use it. Written in C# with WPF on .NET Framework 4.5.2.

## Features

- Lists the ready disks (fixed, removable and RAM disks); the **Refresh** button reads the list again.
- **Start** creates a hidden file with a random name in the root of the selected disk and writes 1 MB to it again and again, so the disk never stays idle.
- **Update Every** sets how often it writes: a number followed by `s`, `m`, `h` or `d` (seconds, minutes, hours, days), for example `30s`, `1m` or `1.5h`. The default is `1m`. The text next to it shows the period in seconds; it can be from 1 second to 1 day.
- **Pause** stops writing; **Start** goes on again.
- Closing the app stops writing and deletes the hidden file.

## Requirements

- Windows with .NET Framework 4.5.2 or later. Windows 10 and 11 already include it.
- The app writes to the root of the disk you pick. A USB or external disk needs nothing more; the system disk (`C:\`) needs administrator rights.

## Usage

Download the latest `Keep-Disk-On-v<version>.7z` from [Releases](https://github.com/Mohammad-Diab/Keep-Disk-On/releases), extract it, and run `Keep Disk On.exe`. There is no installer.

The exe is not signed, so Windows SmartScreen may warn the first time you run it: click **More info**, then **Run anyway**.

## Building

```
git clone https://github.com/Mohammad-Diab/Keep-Disk-On.git
```

Open `Keep Disk On.sln` in Visual Studio and build, or run:

```
MSBuild "Keep Disk On.sln" /p:Configuration=Release
```

The exe is written to `Keep Disk On\bin\Release`.

The project targets .NET Framework 4.5.2. Recent Visual Studio versions don't ship its reference assemblies; if the build stops with `MSB3644`, extract the [Microsoft.NETFramework.ReferenceAssemblies.net452](https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies.net452) NuGet package and point MSBuild at its `build` folder:

```
MSBuild "Keep Disk On.sln" /p:Configuration=Release /p:TargetFrameworkRootPath=<package>\build
```

## Author

Mohammad Diab

## License

[Apache 2.0](LICENSE)
