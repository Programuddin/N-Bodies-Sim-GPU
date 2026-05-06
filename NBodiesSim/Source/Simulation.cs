using NBodiesSim.Source.Core;
using NBodiesSim.Source.Data;
using NBodiesSim.Source.Models;
using NBodiesSim.Source.Systems;
using Raylib_cs;

namespace NBodiesSim.Source;

internal class Simulation
{
  private string _keyName = "Null";

  // 4. Initial configuration setup
  private double _timeStep = 43200; // Each simulation frame represents one Earth day in real time.
  private int _n = 300; // Number of calculations into which timeStep will be divided. Higher means better precision.
  private double _subStep = 43200.0 / 300; // Pre-calculated _timeStep / _n
  private int _textAlign = SimulationConstants.DefaultTextAlign;

  private readonly StarList _stars = new StarList();
  private readonly Camera _camera = new Camera();
  private Astro? _selectedAstro;

  private readonly PhysicsEngineRk4 _physicsEngine;
  private readonly RenderSystem _renderSystem;
  private readonly DataLoader _dataLoader;
  private readonly InputSystems _inputSystems;
  private double _simulatedTime;
  private (double, double, double, double) _lastEnergyCalc = (0, 0, 0, 0);

    private Thread _physicsThread;
    private volatile bool _running = true;
    private readonly object _physicsLock = new object();
    private List<(double x, double y, double vx, double vy)> _latestPositions = new();
    private bool _physicsResultReady = false;


    public Simulation(
      PhysicsEngineRk4 physicsEngine,
      RenderSystem renderSystem,
      DataLoader dataLoader,
      InputSystems inputSystems
  )
  {
    _physicsEngine = physicsEngine;
    _renderSystem = renderSystem;
    _dataLoader = dataLoader;
    _inputSystems = inputSystems;
  }

  public void Initialize()
  {

    // 1. Create the Raylib window
    Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
    Raylib.InitWindow(_camera.Width, _camera.Height, "N-Bodies Simulator");

    // Generate initial stars (only once)
    _stars.GenerateStars();
  }


    public void Run()
    {
        List<Astro> astros = _dataLoader.Astros;
        _selectedAstro = astros.First(a => a.Id == _camera.TargetId);

        // Warm-up
        _physicsEngine.UpdateRk4(astros, _subStep, 1);


        _physicsThread = new Thread(() =>
        {
            while (_running)
            {
                List<Astro> workingCopy;
                lock (_physicsLock)
                {
                    workingCopy = astros.Select(a => a.Clone()).ToList();
                }

                _physicsEngine.UpdateRk4(workingCopy, _timeStep, _n);

                lock (_physicsLock)
                {
                    for (int i = 0; i < astros.Count; i++)
                    {
                        astros[i].Position = workingCopy[i].Position;
                        astros[i].Velocity = workingCopy[i].Velocity;
                    }
                    RenderSystem.SaveTrail(astros, _timeStep);
                    _simulatedTime += _timeStep;
                    _lastEnergyCalc = _physicsEngine.CalculateEnergy(astros);
                    _physicsResultReady = true;
                }
            }
        });
        _physicsThread.IsBackground = true;
        _physicsThread.Start();

        while (!Raylib.WindowShouldClose())
        {
            double dt = Raylib.GetFrameTime();
            if (dt > 0.1) dt = 0.1;

            var newConfig = _inputSystems.ProcessInput(_renderSystem);
            if (newConfig != null)
            {
                _camera.TargetId = newConfig.Value.Id;
                _camera.TargetDistanceScale = newConfig.Value.TargetDistanceScale;
                _camera.TargetRadiusScale = newConfig.Value.TargetRadiusScale;
                _timeStep = newConfig.Value.TargetTimeStep;
                _n = newConfig.Value.TargetN;
                _subStep = _timeStep / _n;
                _camera.ResetLerp();
                _textAlign = newConfig.Value.TextAlign;
                _keyName = newConfig.Value.KeyName;
            }

            List<Astro> renderSnapshot;
            double simTime;
            lock (_physicsLock)
            {
                renderSnapshot = astros.Select(a => a.Clone()).ToList();
                simTime = _simulatedTime;
            }

            _selectedAstro = renderSnapshot.First(a => a.Id == _camera.TargetId);
            _camera.Update(dt, _selectedAstro.Position);

            _renderSystem.Draw(
                renderSnapshot, _camera, _selectedAstro,
                SimulationConstants.CrossSideLength, _textAlign,
                _stars, _keyName, _lastEnergyCalc, simTime
            );
        }

        _running = false;
        _physicsThread.Join();
        Raylib.CloseWindow();
    }
}
