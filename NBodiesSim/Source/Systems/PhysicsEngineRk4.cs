/*
 * This class handles the simulation's physics calculations using the RUnge-Kutta 4 algorithm
 *
 * It consists of three four main calculation methods: CalculateK1, CalculateK2, CalculateK3 and CalculateK4. Each one
 * of them calculates the hypothetical velocity and acceleration of the bodies for fractions of a given time, dt:
 *
 * - CalculateK1: Updates the astros acceleration parameters and returns the current values of velocity and position in t = 0
 * - CalculateK2: Calculates the acceleration and velocity of the bodies for t = dt / 2, using the results of K1
 * - CalculateK3: Calculates the acceleration and velocity of the bodies for t = dt / 2, using the results of K2
 * - CalculateK4: Calculates the acceleration and velocity of the bodies for t = dt, using the results of K3
 *
 * WARNING: Vector2D arrays hypotheticalPos and HypotheticalVel are used in CalculateK2, CalculateK3 and CalculateK4.
 *          Before this iteration of the RK4 implementation, those variables were created inside each method, separately.
 *          To reduce the CPU time in case of calculating thousands of body-to-body interactions, the variables were
 *          created in UpdateRk4 and passed to the different methods.
 *
 *          The first iteration of RK4 that included this feature didn't take into account what pass by reference and
 *          pass by value was, so every method returned hypotheticalVel after modifying the array. That implied that
 *          velK2, velK3 and velK4 were references to hypotheticalVel and so, they had the same value when updating the
 *          position and velocity of each body.
 *
 *          Now, each method returns hypotheticalVel.ToArray() to create a copy of that array status into a new variable,
 *          solving the problem. In the case of hypotheticalPos, there was no problem. Each index of the array
 *          was updated with the astro[i].Position + velKN * (dt / 2) --or just dt, in the case of CalculateK4--, so no
 *          error "reference problem" happened there.
 *
 * Subsequently, there is a method accessed by the simulation that encompasses the entire physics calculation. For that,
 * it calculates a weighted average of the results of K1, K2, K3 and K4 to update the position and velocity of the bodies.
 */
using NBodiesSim.Source.Core;
using NBodiesSim.Source.Models;
using System.Runtime.InteropServices;

namespace NBodiesSim.Source.Systems;

internal class PhysicsEngineRk4
{
    private double _energy;
    private double _initialEnergy;

    [DllImport("CudaPhysics.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void UpdatePhysicsCUDA(double[] posX, double[] posY, double[] velX, double[] velY, double[] mass, int numBodies, double dt, int subSteps);

    [DllImport("CudaPhysics.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InitCudaMemory(int numBodies);


    private double[] _posX = [];
    private double[] _posY = [];
    private double[] _velX = [];
    private double[] _velY = [];
    private double[] _mass = [];

    private void EnsureArraySizes(int count)
    {
        if (_posX.Length == count) return;
        _posX = new double[count];
        _posY = new double[count];
        _velX = new double[count];
        _velY = new double[count];
        _mass = new double[count];

        InitCudaMemory(count);
    }
    public void UpdateRk4(List<Astro> astros, double dt, int subSteps)
    {
        int count = astros.Count;
        EnsureArraySizes(count);

        for (int i = 0; i < count; i++)
        {
            _posX[i] = astros[i].Position.X;
            _posY[i] = astros[i].Position.Y;
            _velX[i] = astros[i].Velocity.X;
            _velY[i] = astros[i].Velocity.Y;
            _mass[i] = astros[i].Mass;
        }

        UpdatePhysicsCUDA(_posX, _posY, _velX, _velY, _mass, count, dt, subSteps);

        for (int i = 0; i < count; i++)
        {
            astros[i].Position = new Vector2D(_posX[i], _posY[i]);
            astros[i].Velocity = new Vector2D(_velX[i], _velY[i]);
        }
    }

    public (double, double, double, double) CalculateEnergy(List<Astro> astros)
    {
        double kinetic = 0;
        double potential = 0;

        for (int i = 0; i < astros.Count; i++)
        {
            kinetic += 0.5 * astros[i].Mass * astros[i].Velocity.LengthSquared();
        }
        for (int i = 0; i < astros.Count - 1; i++)
        {
            for (int j = i + 1; j < astros.Count; j++)
            {
                Vector2D rij = astros[j].Position - astros[i].Position;
                potential -= PhysicsConstants.G * astros[i].Mass * astros[j].Mass / rij.Length();
            }
        }
        double totalEnergy = kinetic + potential;

        if (_energy == 0)
        {
            _energy = totalEnergy;
            _initialEnergy = totalEnergy;
            return (0, 0, 0, 0);
        }

        double energyDiff = totalEnergy - _energy;
        double energyDiffRel = energyDiff / _energy;
        double accumulatedEnergDiff = (totalEnergy - _initialEnergy) / _initialEnergy;
        _energy = totalEnergy;
        return (_energy, energyDiff, energyDiffRel, accumulatedEnergDiff);
    }
}
