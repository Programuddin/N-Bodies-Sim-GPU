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

namespace NBodiesSim.Source.Systems;

internal class PhysicsEngineRk4
{
    private double _energy;
    private double _initialEnergy;

    // Pre-allocated arrays to avoid GC pressure
    private Vector2D[] _initialPos = [];
    private Vector2D[] _hypotheticalPos = [];
    private Vector2D[] _acc = [];
    private Vector2D[] _accK1 = [];
    private Vector2D[] _accK2 = [];
    private Vector2D[] _accK3 = [];
    private Vector2D[] _accK4 = [];
    private Vector2D[] _velK1 = [];
    private Vector2D[] _velK2 = [];
    private Vector2D[] _velK3 = [];
    private Vector2D[] _velK4 = [];

    private void EnsureArraySizes(int count)
    {
        if (_initialPos.Length == count) return;
        _initialPos = new Vector2D[count];
        _hypotheticalPos = new Vector2D[count];
        _acc = new Vector2D[count];
        _accK1 = new Vector2D[count];
        _accK2 = new Vector2D[count];
        _accK3 = new Vector2D[count];
        _accK4 = new Vector2D[count];
        _velK1 = new Vector2D[count];
        _velK2 = new Vector2D[count];
        _velK3 = new Vector2D[count];
        _velK4 = new Vector2D[count];
    }

    // Calculates accelerations from hypothPos and writes them into _acc
    private void CalcAccelerations(List<Astro> astros, Vector2D[] hypothPos)
    {
        int count = astros.Count;
        for (int i = 0; i < count; i++)
        {
            _acc[i] = Vector2D.Zero;
        }

        for (int i = 0; i < count - 1; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                Vector2D rij = hypothPos[j] - hypothPos[i];
                double dist = rij.Length();
                Vector2D acji = PhysicsConstants.G / (dist * dist * dist) * rij;
                _acc[i] += acji * astros[j].Mass;
                _acc[j] -= acji * astros[i].Mass;
            }
        }
    }

    public void UpdateRk4(List<Astro> astros, double dt)
    {
        int count = astros.Count;
        EnsureArraySizes(count);

        double halfDt = dt / 2;

        // Copy initial positions
        for (int i = 0; i < count; i++)
        {
            _initialPos[i] = astros[i].Position;
        }

        // --- K1: evaluate at current state ---
        CalcAccelerations(astros, _initialPos);
        for (int i = 0; i < count; i++)
        {
            _accK1[i] = _acc[i];
            _velK1[i] = astros[i].Velocity;
        }

        // --- K2: evaluate at t + dt/2 using K1 ---
        for (int i = 0; i < count; i++)
        {
            _hypotheticalPos[i] = astros[i].Position + _velK1[i] * halfDt;
        }
        CalcAccelerations(astros, _hypotheticalPos);
        for (int i = 0; i < count; i++)
        {
            _accK2[i] = _acc[i];
            _velK2[i] = astros[i].Velocity + _accK1[i] * halfDt;
        }

        // --- K3: evaluate at t + dt/2 using K2 ---
        for (int i = 0; i < count; i++)
        {
            _hypotheticalPos[i] = astros[i].Position + _velK2[i] * halfDt;
        }
        CalcAccelerations(astros, _hypotheticalPos);
        for (int i = 0; i < count; i++)
        {
            _accK3[i] = _acc[i];
            _velK3[i] = astros[i].Velocity + _accK2[i] * halfDt;
        }

        // --- K4: evaluate at t + dt using K3 ---
        for (int i = 0; i < count; i++)
        {
            _hypotheticalPos[i] = astros[i].Position + _velK3[i] * dt;
        }
        CalcAccelerations(astros, _hypotheticalPos);
        for (int i = 0; i < count; i++)
        {
            _accK4[i] = _acc[i];
            _velK4[i] = astros[i].Velocity + _accK3[i] * dt;
        }

        // --- Final RK4 weighted update ---
        for (int i = 0; i < count; i++)
        {
            astros[i].Velocity += (_accK1[i] + 2 * _accK2[i] + 2 * _accK3[i] + _accK4[i]) * dt / 6;
            astros[i].Position += (_velK1[i] + 2 * _velK2[i] + 2 * _velK3[i] + _velK4[i]) * dt / 6;
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
