/*
 * This class is responsible for loading the information of all bodies displayed in the Solar System, collecting the
 * information from the Data/astrosData.json file, located at the project root. It generates a list of Astro objects with
 * the necessary information.
 */
using System.Text.Json;
using NBodiesSim.Source.Core;
using NBodiesSim.Source.Models;
using NBodiesSim.Source.Models.DTOs;
using Raylib_cs;

namespace NBodiesSim.Source.Data;

internal class DataLoader
{
    // List of astros
    public readonly List<Astro> Astros = new List<Astro>();

    // Constructor
    public DataLoader()
    {
        try
        {
            const string filePath = "Data/astrosData.json";

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            // Generate astros
            string jsonAstroData = File.ReadAllText(filePath);
            WrapperAstroData? wrapperAstroData = JsonSerializer.Deserialize<WrapperAstroData>(jsonAstroData);

            // Check deserialization
            if (wrapperAstroData?.Astros == null)
            {
                throw new Exception("Error loading JSON file or it is empty");
            }

            // Iterate over the deserialized astro list
            foreach (AstroData data in wrapperAstroData.Astros)
            {
                double desiredTrailTime = data.DesiredTrailTime;

                // If it's a satellite, inherit DesiredTrailTime from its parent planet
                if (data.ParentId != null)
                {
                    AstroData? parentData = wrapperAstroData.Astros.FirstOrDefault(p => p.Id == data.ParentId);
                    if (parentData != null) desiredTrailTime = parentData.DesiredTrailTime;
                }

                if (data.Mass < 0.0001)
                    throw new Exception($"La masa del cuerpo {data.Name} es negativa: {data.Mass}");

                if (data.Radius < 0.0001)
                    throw new Exception($"El radio del cuerpo {data.Name} es negativo: {data.Radius}");

                if (data.Position.Length != 2)
                {
                    throw new Exception(
                        $"Los datos de la posición del cuerpo {data.Name} no son correctos: {data.Position}");
                }

                if (data.Velocity.Length != 2)
                {
                    throw new Exception(
                        $"Los datos de la velocidad del cuerpo {data.Name} no son correctos: {data.Velocity}");
                }

                if (data.Color.Length != 4)
                    throw new Exception($"Los datos de color del cuerpo {data.Name} no son correctos: {data.Color}");

                const int colorLength = 4;

                for (int i = 0; i < colorLength; i++)
                {
                    if (data.Color[i] is < 0 or > 255)
                    {
                        throw new Exception(
                            $"Los valores (r, g, b, a) del cuerpo {data.Name} no son correctos: {data.Color}");
                    }
                }

                // Check if it has rings
                Color? ringColor = null;

                if (data.HasRings)
                {
                    if (data.InnerRingRadius < 0 || data.OuterRingRadius < 0)
                    {
                        throw new Exception(
                            $"Los datos de los radios de los anillos del cuerpo {data.Name} no son correctos:" +
                            $" InnerRadius = {data.InnerRingRadius}, OuterRadius = {data.OuterRingRadius}");
                    }

                    if (data.InnerRingRadius > data.OuterRingRadius)
                    {
                        throw new Exception(
                            $"Los datos de los radios de los anillos del cuerpo {data.Name} no son correctos. " +
                            $"{data.InnerRingRadius} > {data.OuterRingRadius}");
                    }

                    if (data.RingColor == null)
                    {
                        throw new Exception(
                            $"El cuerpo {data.Name} no tiene el color de sus anillos: {data.RingColor}");
                    }

                    if (data.RingColor.Length != 4)
                    {
                        throw new Exception(
                            $"Los datos de color de los anillos del cuerpo {data.Name} no son correctos: {data.RingColor}");
                    }

                    for (int i = 0; i < colorLength; i++)
                    {
                        if (data.RingColor[i] is < 0 or > 255)
                        {
                            throw new Exception(
                                $"Los valores (r, g, b, a) de los anillos del cuerpo {data.Name} no son correctos: {data.RingColor}");
                        }
                    }

                    ringColor = new Color(data.RingColor[0], data.RingColor[1], data.RingColor[2], data.RingColor[3]);
                }

                // Create the Astro object for each body
                var astro = new Astro
                {
                    Id = data.Id,
                    ParentId = data.ParentId,
                    Name = data.Name,
                    Mass = data.Mass,
                    Radius = data.Radius,
                    Color = new Color(data.Color[0], data.Color[1], data.Color[2], data.Color[3]),
                    Position = new Vector2D(data.Position[0], data.Position[1]),
                    Velocity = new Vector2D(data.Velocity[0], data.Velocity[1]),
                    Acceleration = Vector2D.Zero,
                    DesiredTrailTime = desiredTrailTime,
                    Trail = new Queue<Vector2D>(),
                    HasRings = data.HasRings,
                    InnerRingRadius = data.InnerRingRadius,
                    OuterRingRadius = data.OuterRingRadius,
                    RingColor = ringColor,
                };
                Astros.Add(astro); // Add the astro to the list
            }
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine(e);
            throw;
        }
        catch (JsonException e)
        {
            Console.WriteLine(e);
            throw;
        }
        catch (IOException e)
        {
            Console.WriteLine(e);
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}