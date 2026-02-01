/*
 * This class contains the information for the generation of AstroData objects with all parameters. From objects generated
 * with this class, Astro objects are constructed, which will be used in the simulation.
 */
namespace NBodiesSim.Source.Models.DTOs;

internal class AstroData
{
    public required int Id { get; set; }

    public int? ParentId { get; set; }

    public required string Name { get; set; }

    public required double Mass { get; set; }

    public required double Radius { get; set; }

    public required int[] Color { get; set; } // [r, g, b, a]

    public required double[] Position { get; set; } // [x, y]

    public required double[] Velocity { get; set; } // [x, y]

    public double DesiredTrailTime { get; set; } = 86400 * 30; // Time in seconds (default: 30 days)

    // If the body has rings:
    public bool HasRings { get; set; } = false;

    public double InnerRingRadius { get; set; } // in meters

    public double OuterRingRadius { get; set; } // in meters

    public int[]? RingColor { get; set; } // [r, g, b, a]
}