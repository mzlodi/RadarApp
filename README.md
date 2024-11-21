
# RadarApp
[![AGPL License](https://img.shields.io/badge/License-GNU_AGPL_v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)

A desktop app made for visually displaying detected targets simulated by Node.js script.
## Tech Stack

**Simulation:** Node.js (v22.9.0)

**Desktop application:** C# (.NET 8)

## Run Locally
ℹ️ node v22.9.0

Clone the project

```bash
  git clone https://github.com/mzlodi/RadarApp.git
```

Change into the RadarApp directory

```bash
  cd .\RadarApp\
```

Build the app

```bash
  dotnet build
```

Run the simulator script from the project's directory

For example:
```bash
  node simulator.js 8001 line 10 60 1
```

Start the app

```bash
   > RadarApp > bin > Debug > net8.0-windows > RadarApp.exe
```
