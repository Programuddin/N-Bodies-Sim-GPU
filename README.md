# N-Bodies Simulator 🌍

A realistic Solar System simulator built with C# and Raylib, featuring accurate N-body gravitational physics using the Runge-Kutta 4th order integration method.

## 📖 Overview

NBodiesSim simulates the gravitational interactions between celestial bodies in our Solar System. Starting from real initial position and velocity data, it calculates the motion of the Sun, planets, moons, and other bodies through accurate physics simulations. The simulation allows you to explore different viewpoints and observe orbital mechanics in real-time.

## ✨ Features

- **Accurate Physics**: Implementation of the Runge-Kutta 4th order (RK4) method for precise orbital mechanics
- **Full N-body Simulation**: All celestial bodies interact gravitationally with each other
- **Real Solar System Data**: Initial conditions based on actual planetary positions and velocities
- **Multiple Camera Views**: Focus on any planet to see its satellites and rings
- **Per-Body HUD**: Dynamic panel showing mass, velocity, position and radius of the selected body, with satellite data
- **Smooth Camera Transitions**: Interpolated camera movement between different viewpoints
- **Orbital Trails**: Visualize the path of celestial bodies over time
- **Planetary Rings**: Saturn, Uranus, and Neptune rendered with their characteristic rings
- **10,000 Background Stars**: Procedurally generated starfield for immersion
- **Frame-Rate Independent Physics**: Physics calculations run at fixed timestep regardless of rendering FPS
- **Energy Conservation Monitoring**: Real-time display of energy drift and cumulative error to verify simulation accuracy
- **Cross-Platform**: Runs on Windows, Linux, and macOS

## 🔥 Recent Performance Improvements

### Trail Rendering Optimization (v1.1)
- **Fixed critical bug**: Trail queues previously grew indefinitely, causing FPS degradation from 160→20 over time
- **Memory efficiency**: Trails now properly maintain size limits (1.5-3 orbits worth of points)
- **Stable performance**: 60 FPS maintained even after hours of simulation
- **Visual quality**: Optimized trail lengths preserve ability to observe orbital perturbations and precession

### Mars Satellite Corrections (v1.2)
- **Fixed position data**: Corrected Phobos and Deimos positions (were 10x too close to Mars)
- **Accurate orbits**: Satellites now maintain stable orbits using full N-body physics
- **Performance**: Mars view maintains 60 FPS with proper timestep subdivisions (TargetN=300)
- **Energy monitoring**: Added cumulative energy error tracking for long-term stability verification

### Timestep Optimization & Long-Term Stability (v1.3)
- **Reduced default timestep**: From 86400s (1 day) to 43200s (12 hours) for improved long-term stability
- **Phobos orbital stability**: Achieved 1,765+ simulated years of stable orbits (vs 468 years with previous timestep)
- **3.7× improvement**: Simulation now runs 3.7 times longer before any numerical artifacts appear
- **Validated physics accuracy**: Orbital velocities within 0.4% of theoretical values, energy conservation at 10⁻⁸ precision
- **Characterized decay rates**: Phobos decays at only ~69 m/year, requiring ~47,000 years to reach Roche limit
- **Deimos ultra-stable**: Maintains ±486 meter orbital variation over 1,765 years (154× more stable than Phobos)

## 🎮 Controls

| Key | Action |
|-----|--------|
| **Space** | Wide system view (all planets visible) |
| **0** | Focus on the Sun |
| **1** | Focus on Mercury |
| **2** | Focus on Venus |
| **3** | Focus on Earth |
| **4** | Focus on Mars |
| **5** | Focus on Jupiter |
| **6** | Focus on Saturn |
| **7** | Focus on Uranus |
| **8** | Focus on Neptune |
| **9** | Easter Egg |
| **F1** | Toggle help menu |
| **T** | Toggle simulated time display |
| **E** | Toggle energy conservation display |
| **H** | Toggle body HUD (with satellite data) |
| **Esc** | Exit simulation |

## 🔧 Requirements

- **.NET 10.0 SDK** or higher
- **Raylib-cs 7.0.2** (automatically installed via NuGet)
- Operating System: Windows, Linux, or macOS

## 🚀 Installation

### Clone the Repository

```bash
git clone https://github.com/phanaur/n-bodies-sim.git
cd n-bodies-sim
```

### Build and Run

```bash
cd NBodiesSim
dotnet build
dotnet run
```

### Build Release Version

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

Replace `linux-x64` with `win-x64` (Windows) or `osx-x64` (macOS) as needed.

## 📐 Physics Implementation

### Runge-Kutta 4th Order (RK4)

The simulator uses RK4, a numerical integration method that provides high accuracy for solving differential equations. For each timestep, RK4 evaluates the gravitational forces at four different points:

1. **K1**: Current state (t)
2. **K2**: Midpoint using K1 slopes (t + dt/2)
3. **K3**: Midpoint using K2 slopes (t + dt/2)
4. **K4**: Endpoint using K3 slopes (t + dt)

Final position and velocity are calculated using a weighted average:

```
Δvelocity = (K1 + 2×K2 + 2×K3 + K4) × dt / 6
Δposition = (V1 + 2×V2 + 2×V3 + V4) × dt / 6
```

### Gravitational Force Calculation

Forces between all pairs of bodies are calculated using Newton's law of universal gravitation:

```
F = G × (m₁ × m₂) / r²
```

Where:
- **G** = 6.674 × 10⁻¹¹ m³ kg⁻¹ s⁻² (gravitational constant)
- **m₁, m₂** = masses of the two bodies
- **r** = distance between the bodies

The simulation uses an O(n²) all-pairs algorithm, suitable for the ~17 bodies in the Solar System.

### Frame-Rate Independence

Physics calculations run at a fixed timestep (typically 1/60 second) independent of the rendering frame rate. This is achieved using an accumulator pattern:

```
Physics updates: Fixed timestep (e.g., 86400 seconds/day ÷ n subdivisions)
Rendering: Variable framerate (monitor refresh rate)
Interpolation: Smooth rendering between physics states
```

## 📂 Project Structure

```
NBodiesSim/
├── Source/
│   ├── Core/                  # Core utilities
│   │   ├── Vector2D.cs       # 2D vector mathematics
│   │   └── Camera.cs         # Camera system with lerp interpolation
│   ├── Data/                  # Data loading
│   │   ├── DataLoader.cs     # Loads celestial body data from JSON
│   │   └── ConfigLoader.cs   # Loads camera configurations
│   ├── Models/                # Domain models
│   │   ├── Astro.cs          # Celestial body representation
│   │   ├── *Constants.cs     # Physics, rendering, and simulation constants
│   │   └── DTOs/             # Data transfer objects for JSON deserialization
│   ├── Systems/               # Application systems
│   │   ├── PhysicsEngineRK4.cs    # Runge-Kutta 4 physics engine
│   │   ├── RenderSystem.cs        # Rendering logic
│   │   ├── InputSystems.cs        # Keyboard input handling
│   │   └── Stars.cs               # Background starfield
│   └── Simulation.cs          # Main simulation orchestrator
├── Data/                      # Configuration files
│   ├── astrosData.json       # Celestial body properties
│   └── astrosConfig.json     # Camera and timestep configurations
├── Program.cs                 # Application entry point
└── NBodiesSim.csproj         # Project file
```

## ⚙️ Configuration

### Celestial Bodies (`Data/astrosData.json`)

Defines properties for each celestial body:

```json
{
  "id": 0,
  "name": "Sun",
  "mass": 1.989e30,
  "radius": 696340000,
  "position": [0, 0],
  "velocity": [0, 0],
  "color": [255, 255, 0, 255],
  "desiredTrailTime": 2592000,
  "hasRings": false
}
```

**Properties:**
- `id`: Unique identifier
- `name`: Display name
- `mass`: Mass in kilograms
- `radius`: Radius in meters
- `position`: [x, y] coordinates in meters
- `velocity`: [vx, vy] in meters/second
- `color`: [R, G, B, A] values (0-255)
- `desiredTrailTime`: Trail duration in seconds
- `hasRings`: Boolean for ring rendering
- `parentId` (optional): For moons, ID of parent planet

### Camera Configurations (`Data/astrosConfig.json`)

Defines camera behavior for each viewpoint:

```json
{
  "key": "Digit3",
  "id": 3,
  "keyName": "Earth",
  "targetDistanceScale": 2e10,
  "targetRadiusScale": 15,
  "targetTimeStep": 3600,
  "targetN": 200,
  "textAlign": 0
}
```

**Properties:**
- `key`: Keyboard key (e.g., "Digit3" for key 3)
- `id`: Celestial body ID to focus on
- `targetDistanceScale`: Zoom level (distance scale in meters)
- `targetRadiusScale`: Body size rendering scale
- `targetTimeStep`: Physics simulation timestep in seconds
- `targetN`: Number of subdivisions for RK4 (higher = more accurate)
- `textAlign`: Text alignment for UI elements

## 🏗️ Architecture

### Design Patterns

- **Dependency Injection**: Manual DI in `Program.cs` for system composition
- **Layered Architecture**: Clear separation between Core, Data, Models, and Systems
- **Data-Driven Configuration**: All celestial body and camera data externalized to JSON
- **Fixed Timestep Loop**: Physics decoupled from rendering framerate

### Key Components

1. **PhysicsEngineRK4**: Implements RK4 physics engine
   - `CalcAccelerations()`: Computes gravitational forces using G*rij/r^3 (O(n²))
   - `UpdateRk4()`: Main physics update loop with inline K1-K4 stages and pre-allocated arrays
   - `CalculateEnergy()`: Monitors energy conservation with cumulative error tracking

2. **RenderSystem**: Handles all visualization
   - Celestial bodies with clamped radii (2-40 pixels)
   - Orbital trails with alpha gradient fade
   - Off-screen indicators (triangular pointers)
   - Planetary rings (Saturn, Uranus, Neptune)
   - 10,000-star background (batch-rendered for performance)

3. **Camera**: Smooth interpolated camera movement
   - Lerp-based position and scale transitions
   - FPS-independent animation
   - Multiple preset configurations

4. **DataLoader/ConfigLoader**: JSON deserialization with validation
   - File existence checks
   - Structure validation
   - Data range validation (mass > 0, array lengths, color ranges)

## 🔬 Technical Details

### Optimizations

- **Pre-Allocated Arrays**: All RK4 intermediate arrays pre-allocated as class fields, eliminating per-frame GC pressure
- **Batch Rendering**: Stars and orbital trails rendered using Raylib's batching system for optimal GPU performance
- **Spatial Culling**: Off-screen objects skipped during rendering
- **Fixed Timestep**: Predictable physics regardless of hardware
- **Adaptive Timesteps**: Different camera views use appropriate timesteps (1 day for outer planets, hours/minutes for inner planets and satellites)
- **Trail Memory Management**: Efficient queue-based trail system with automatic length limiting

### Precision

- **Double-precision** floating-point throughout (`double` in C#)
- Position units: meters
- Velocity units: meters/second
- Mass units: kilograms
- Time units: seconds

## 🛣️ Future Improvements

Planned features (as documented in `Program.cs`):

- [x] **F1 Help Menu**: Initial usage guide accessible in-app
- [x] **Planet-Specific HUD**: Dynamic information panels showing characteristics, satellites, velocities
- [ ] **Solar System Barycenter**: Center orbital calculations on the system's center of mass
- [ ] **NASA API Integration**: Load real positions and velocities for any chosen date
- [ ] **State Persistence**: Save/load simulation snapshots
- [ ] **Position Cache**: Pre-compute trajectories for faster playback
- [ ] **Voyager Missions**: Simulate the Voyager 1 and 2 journeys
- [ ] **Heliopause Visualization**: Render the boundary of the solar wind
- [ ] **Interface-Based Architecture**: Refactor components to use interfaces for better testability

## 📊 Performance

Current performance with 25+ celestial bodies:
- **FPS**: 60 (stable, vsync-limited across all views)
- **Physics Complexity**: O(n²) for all bodies (full N-body simulation)
- **RK4 Subdivisions**: 300 per timestep (configurable per camera view)
- **Energy Conservation**: Cumulative error oscillates ±1.89×10⁻⁷, mean drift +3.96×10⁻⁸ (excellent)
- **Long-term Stability**: Validated over 1,765 simulated years with stable orbits
- **Orbital Accuracy**:
  - Phobos velocity: 2,132.84 m/s (theoretical: 2,125 m/s, error: 0.37%)
  - Deimos velocity: 1,350.03 m/s (theoretical: 1,348 m/s, error: 0.15%)
  - Phobos period: 27,689 seconds (theoretical: 27,540 seconds, error: 0.54%)
- **Simulation Speed**: With 43200s timestep, Earth completes one orbit in ~12 seconds of real-time

## ⚠️ Known Limitations

### Long-Term Orbital Decay
- **Symptom**: Very slow orbital decay observed in inner moons (Phobos: ~69 m/year)
- **Cause**: Accumulation of numerical errors over extended simulation times + non-realistic initial conditions (all bodies aligned)
- **Impact**: Minimal - would take ~47,000 simulated years for Phobos to reach Roche limit
- **Mitigation**: Using real ephemeris data (NASA JPL Horizons) would eliminate this artifact
- **Status**: Well-characterized and predictable; does not affect short/medium-term accuracy

### Initial Conditions
- **Current**: All bodies initialized in horizontal alignment (not physically realistic)
- **Effect**: Creates artificial perturbations not present in real Solar System
- **Future**: Integration with NASA JPL Horizons API for realistic starting positions planned

## 🧪 Testing

Currently, the project does not include automated tests. Future improvements may add:
- Unit tests for `Vector2D` operations
- Physics accuracy tests (conservation of energy/momentum)
- Integration tests for data loading

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

Copyright (c) 2026 phanaur

## 🙏 Credits

- **Physics**: Runge-Kutta 4th order integration method
- **Graphics**: [Raylib](https://www.raylib.com/) and [Raylib-cs](https://github.com/ChrisDill/Raylib-cs)
- **Data**: Initial Solar System conditions based on real astronomical data
- **Development**: Built with .NET 10.0 and C# 14

---

## 📸 Screenshots

_Coming soon_

## 🌟 Acknowledgments

This project was developed as an educational exploration of N-body gravitational physics and numerical integration methods. Special thanks to the Raylib community for the excellent graphics library.

---

**Enjoy exploring the cosmos!** 🚀✨