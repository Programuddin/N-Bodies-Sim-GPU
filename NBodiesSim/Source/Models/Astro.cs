/*
 * This is the Astro class. It is the heart of all bodies involved in the simulation. Each simulated body contains
 * real information about some of its characteristics, such as its name, identification, body radius, position,
 * velocity, etc.
 */
using System.ComponentModel.DataAnnotations;
using Raylib_cs;
using NBodiesSim.Source.Core;

namespace NBodiesSim.Source.Models;

public class Astro
{
    // Fixed variables
    public required string Name { get; init; }

    public required int Id { get; init; }

    public int? ParentId { get; init; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Mass must be greater than 0")]
    public required double Mass { get; init; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Radius must be greater than 0")]
    public required double Radius { get; init; }

    // Color
    public required Color Color { get; init; }

    // In the case of having rings (entirely done by AI)
    public bool HasRings { get; init; }

    public double InnerRingRadius { get; init; } // in meters

    public double OuterRingRadius { get; init; } // in meters

    public Color? RingColor { get; init; } // [r, g, b, a]

    // Orbit trail
    public required Queue<Vector2D> Trail { get; init; } = new Queue<Vector2D>();

    public double DesiredTrailTime { get; init; } = 86400 * 30; // Time in seconds we want to see (e.g., 30 days)

    // Non-fixed variables
    public required Vector2D Position { get; set; }

    public required Vector2D Velocity { get; set; }

    public required Vector2D Acceleration { get; set; }
}