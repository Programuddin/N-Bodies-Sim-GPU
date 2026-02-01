/*
 * This class contains the information used to generate `AstroConfig` objects, with all the parameters needed to adjust
 * the camera, rendering calculations, and the number of calculations performed by the physics engine.
 */
using Raylib_cs;

namespace NBodiesSim.Source.Models.DTOs;

internal readonly struct AstroConfig
{
    public required KeyboardKey Key { get; init; }

    public required string Name { get; init; }

    public required string KeyName { get; init; }

    public required int Id { get; init; }

    public required int TextAlign { get; init; }

    public required double TargetDistanceScale { get; init; }

    public required double TargetRadiusScale { get; init; }

    public required double TargetTimeStep { get; init; }

    public required int TargetN { get; init; }

    public required double InitialLerpSpeed { get; init; }
}