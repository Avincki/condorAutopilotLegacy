# CondorAutoPilot (Legacy)

> **This is legacy code, kept as a reference only.** It is not maintained, is not expected to build as-is, and will not receive updates or support.

An old experimental autopilot for the [Condor](https://www.condorsoaring.com/) gliding simulator, written in C# / WinForms (.NET Framework 4.7.2), mostly between 2021 and 2022.

## What's in here

- **Simulator link**: reads Condor's UDP telemetry (`CondorUDPData.cs`, `Listener.cs`) and sends control input (`AutoPilot.cs`, `NaviCon*.cs`).
- **Navigation and planning**: flight plans, airports, terrain and path finding (`FlightPlanData.cs`, `FlightPath.cs`, `Airports.cs`, `TerrainMap.cs`, `Dijkstra.cs`, `Planning.cs`).
- **Thermal and wave finding**: `ThermalLocation.cs`, `WaveFinder.cs`, `ThermalML/`, `AI/`.
- **Vision**: YOLO/ONNX object detection (`Yolo/`, `YoloViewer/`) and on-screen digit reading (`ScreenReader/`).
- **UI and maps**: GMap.NET-based map views (`Form1.cs`, `MapCondor.cs`, `InfoWindow/`, `ControlViewer/`).
- **Data**: glider polars and a sample flight plan (`Data/`).

## Not included

To keep the repository small, these are left out:

- the ONNX models (`*.onnx`)
- third-party binaries such as `NaviCon.dll`
- NuGet packages (`packages/`) and build output (`bin/`, `obj/`)

The code references these files, so it won't run without them.

## Configuration

The Google Maps API key was removed and replaced with the placeholder `YOUR_GOOGLE_API_KEY` in `App.config` and `Properties/Settings.settings`.
