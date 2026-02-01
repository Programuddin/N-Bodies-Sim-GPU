/*
 * NBodiesSim is a realistic Solar System simulator. Starting from initial position and velocity conditions for the Sun,
 * planets, satellites, and asteroids, it simulates—via interactions between all bodies—the behavior of each body in the
 * Solar System. This simulation uses the Raylib graphics library, though using other libraries in the future is not
 * ruled out.
 *
 * The simulation lets you select all the planets in the Solar System so you can see each planet's most notable
 * satellites as well as its rings, if it has them.
 *
 * **Pending simulator improvements**
 * - Initial usage guide, shown by pressing the F1 key
 * - Custom HUD for each planet, with information on characteristics, satellites, velocities, etc.
 * - Add the Solar System barycenter to base orbit calculations on it.
 * - Ability to load positions and velocities for a chosen date using NASA's API.
 * - Ability to save position states.
 * - Ability to generate a data cache to save time obtaining positions and velocities of each body
 * - Possible simulation of the Voyager 1 and 2 journeys.
 * - Possible visualization of the heliopause.
 * - Interaction between the different components via interfaces.
 */

using NBodiesSim.Source;
using NBodiesSim.Source.Data;
using NBodiesSim.Source.Systems;

namespace NBodiesSim;

internal static class Program
{
    private static void Main()
    {
        // 1. Create dependencies
        PhysicsEngineRk4 physics = new PhysicsEngineRk4();
        RenderSystem render = new RenderSystem();
        DataLoader data = new DataLoader();
        ConfigLoader config = new ConfigLoader();

        // 2. Create the InputSystem, which depends on the configuration
        InputSystems input = new InputSystems(config.CameraConf);
        
        // 3. Create the simulation, passing all systems
        Simulation sim = new Simulation(physics, render, data, input);
        
        // 4. Run the simulation
        sim.Initialize();
        sim.Run();
    }
}
