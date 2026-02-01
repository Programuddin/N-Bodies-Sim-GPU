using NBodiesSim.Source.Models;

namespace NBodiesSim.Source.Systems;

public class SimulationLogger : IDisposable
{
    private readonly List<Astro> _astros = new List<Astro>();
    private readonly StreamWriter _writer;

    public SimulationLogger(List<Astro> astros)
    {
        _astros = astros;
        // First, I create the file name
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"simulation_log_{timeStamp}.csv";

        // Then I open the file
        _writer = new StreamWriter(fileName, append: false); // append false overwrites the file if already exists

        // Now I write the header

        _writer.WriteLine("SimTime,PhobosX,PhobosY,PhobosVX,PhobosVY,FobosDistToDeimos,FobosDistToMars,FobosDistToSun,FobosDistToJupiter,FobosDistToSaturn,DeimosX,DeimosY,DeimosVX,DeimosVY,DeimosDistToMars,MarsX,MarsY,MarsVX,MarsVY,MarsDistToSun,JupiterX,JupiterY,JupiterVX,JupiterVY,JupiterDistToSun,SaturnX,SaturnY,SaturnVX,SaturnVY,SaturnDistToSun,SunX,SunY,SunVX,SunVY,energy,energyDiff,energyDiffRel,accumulatedEnergDiff");

    }

    // Method to close the file
    public void Dispose()
    {
        _writer?.Close();
        _writer?.Dispose();
        GC.SuppressFinalize(this);
    }

    // Method to log the frame
    public void LogFrame(double simulatedTime, (double energy, double energyDiff, double energyDiffRel, double accumulatedEnergDiff) energyCalc)
    {
        // I create variables of every body being studied
        Astro fobos = _astros.First(a => a.Id == 41);
        Astro deimos = _astros.First(a => a.Id == 42);
        Astro mars = _astros.First(a => a.Id == 4);
        Astro jupiter = _astros.First(a => a.Id == 5);
        Astro saturn = _astros.First(a => a.Id == 6);
        Astro sun = _astros.First(a => a.Id == 0);

        // I calculate distances
        double fobosDistToDeimos = (fobos.Position - deimos.Position).Length();
        double fobosDistToMars = (fobos.Position - mars.Position).Length();
        double fobosDistToSun = (fobos.Position - sun.Position).Length();
        double fobosDistToJupiter = (fobos.Position - jupiter.Position).Length();
        double fobosDistToSaturn = (fobos.Position - saturn.Position).Length();
        double deimosDistToMars = (deimos.Position - mars.Position).Length();
        double marsDistToSun = (mars.Position - sun.Position).Length();
        double jupiterDistToSun = (jupiter.Position - sun.Position).Length();
        double saturnDistToSun = (saturn.Position - sun.Position).Length();

        // Now I write every variable to the file
        _writer.WriteLine($"{simulatedTime}" +
                          $",{fobos.Position.X},{fobos.Position.Y}" +
                          $",{fobos.Velocity.X},{fobos.Velocity.Y}" +
                          $",{fobosDistToDeimos},{fobosDistToMars},{fobosDistToSun}" +
                          $",{fobosDistToJupiter},{fobosDistToSaturn}" +
                          $",{deimos.Position.X},{deimos.Position.Y}" +
                          $",{deimos.Velocity.X},{deimos.Velocity.Y}" +
                          $",{deimosDistToMars}" +
                          $",{mars.Position.X},{mars.Position.Y}" +
                          $",{mars.Velocity.X},{mars.Velocity.Y}" +
                          $",{marsDistToSun}" +
                          $",{jupiter.Position.X},{jupiter.Position.Y}" +
                          $",{jupiter.Velocity.X},{jupiter.Velocity.Y}" +
                          $",{jupiterDistToSun}" +
                          $",{saturn.Position.X},{saturn.Position.Y}" +
                          $",{saturn.Velocity.X},{saturn.Velocity.Y}" +
                          $",{saturnDistToSun}" +
                          $",{sun.Position.X},{sun.Position.Y}" +
                          $",{sun.Velocity.X},{sun.Velocity.Y}" +
                          $",{energyCalc.energy},{energyCalc.energyDiff}" +
                          $",{energyCalc.energyDiffRel},{energyCalc.accumulatedEnergDiff}");

        // Last, we flush the disk
        _writer.Flush();
    }



}