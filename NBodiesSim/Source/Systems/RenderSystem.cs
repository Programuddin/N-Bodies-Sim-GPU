/*
 * This class contains the simulation rendering engine. It calculates scale conversions for each body's positions,
 * determines whether the body should appear on screen or not, draws the star background, and the planet's rings (if any).
 * It also sets a condition based on the distanceScale and radiusScale factors so that any planet whose orbit is near
 * or inside the Sun's outer radius is not drawn.
 */
using System.Numerics;
using NBodiesSim.Source.Core;
using NBodiesSim.Source.Models;
using Raylib_cs;

namespace NBodiesSim.Source.Systems;

internal class RenderSystem
{
    private int _energyDiffAccumulator;
    private (double _energy, double _energyDiff, double _energyDiffRel, double _accumulatedEnergyDiffRel) _diff = (0f, 0f, 0f, 0f);
    private double _timePassed;
    internal bool HelpMenuActive = false;
    internal bool EnergyMenuActive = false;
    internal bool TimeMenuActive = false;
    internal bool ShowBodyHud = false;
    private Astro? _sun;


    private (string label, string? value)[] _astroData = [];

    private List<(string label, string? value)[]> _satelliteData = [];

    // Get the Sun's radius on screen (in pixels)
    private static float GetSunRadius(Astro sun, double radiusScale)
    {
        float sunRadius = (float)(sun.Radius / radiusScale);

        // Set conditions for the maximum and minimum size of the Sun.
        return sunRadius switch
        {
            < RenderConstants.MinBodyRadius => RenderConstants.MinBodyRadius,
            > RenderConstants.MaxBodyRadius => RenderConstants.MaxBodyRadius,
            _ => sunRadius,
        };
    }

    // Get the Sun's position on screen (in pixels)
    private static Vector2 GetSunPositionScreen(Astro sun, Camera camera) => camera.WorldToScreen(sun.Position);

    // Generate coordinates to position the triangle representing a planet that is not visible on screen.
    private static (Vector2[], Vector2) GetTriangle(Vector2 screenPos, Vector2 center, int width, int height)
    {
        Vector2[] triangle = new Vector2[3];
        // 1. Calculate the direction from the center of the screen toward the body
        Vector2 direction = screenPos - center;

        // 2. Normalize the direction
        float length = (float)Math.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y));
        if (length > 0)
        {
            direction.X /= length;
            direction.Y /= length;
        }

        // 3. Calculate the angle of the direction
        float angle = (float)Math.Atan2(direction.Y, direction.X);

        // 4. Place the triangle at the edge of the screen
        Vector2 trianPos = center;

        // Calculate which edge it touches first
        float xDist = float.MaxValue;
        float yDist = float.MaxValue;

        if (Math.Abs(direction.X) > 0.001f)
        {
            float sideX = direction.X > 0 ? width - RenderConstants.Margin : RenderConstants.Margin;
            xDist = Math.Abs((sideX - center.X) / direction.X);
        }

        if (Math.Abs(direction.Y) > 0.001f)
        {
            float sideY = direction.Y > 0 ? height - RenderConstants.Margin : RenderConstants.Margin;
            yDist = Math.Abs((sideY - center.Y) / direction.Y);
        }

        float distance = Math.Min(xDist, yDist);
        trianPos.X = center.X + (direction.X * distance);
        trianPos.Y = center.Y + (direction.Y * distance);

        // 5. Draw the triangle pointing toward the body
        // Main vertex (tip) - points in the direction of the body
        triangle[0] =
            trianPos
            + new Vector2(
                (float)Math.Cos(angle) * RenderConstants.TriangleSize,
                (float)Math.Sin(angle) * RenderConstants.TriangleSize
            );

        // Triangle base (two perpendicular vertices)
        float baseAngle1 = angle + (MathF.PI / 2); // 90 degrees to the right
        float baseAngle2 = angle - (MathF.PI / 2); // 90 degrees to the left
        float baseWidth = RenderConstants.TriangleSize * 0.5f;

        triangle[1] =
            trianPos + new Vector2((float)Math.Cos(baseAngle1) * baseWidth, (float)Math.Sin(baseAngle1) * baseWidth);

        triangle[2] =
            trianPos + new Vector2((float)Math.Cos(baseAngle2) * baseWidth, (float)Math.Sin(baseAngle2) * baseWidth);

        return (triangle, trianPos);
    }

    public static void SaveTrail(List<Astro> astros, double timeStep)
    {
        foreach (Astro astro in astros)
        {
            astro.Trail.Enqueue(astro.Position);

            int effectiveTrailLength = (int)(astro.DesiredTrailTime / timeStep);

            if (effectiveTrailLength < 100)
                effectiveTrailLength = 100;

            if (effectiveTrailLength > 100) 
                effectiveTrailLength = 100;

            while (astro.Trail.Count > effectiveTrailLength)
                astro.Trail.Dequeue();
        }
    }

    private static void DrawTriangles(Vector2 screenPos, Camera camera, Astro astro)
    {
        (Vector2[], Vector2) triangle = GetTriangle(screenPos, camera.Center, camera.Width, camera.Height);

        // Draw the triangle (ensuring correct order)
        Raylib.DrawTriangle(triangle.Item1[0], triangle.Item1[2], triangle.Item1[1], astro.Color);

        // Optional: Draw the body's name near the triangle
        Raylib.DrawText(astro.Name, (int)triangle.Item2.X - 15, (int)triangle.Item2.Y + 15, 12, astro.Color);
    }

    private static void DrawTrail(Astro astro, Camera camera)
    {
        if (astro.Trail.Count < 2)
        {
            return;
        }

        // Use Rlgl to draw all lines in a single call (Batching)
        // This is much faster than calling DrawLineV thousands of times
        Rlgl.Begin(DrawMode.Lines);
        Rlgl.SetTexture(0); // Use the default white texture

        Vector2? p1 = null;
        int i = 0;
        int totalPoints = astro.Trail.Count;

        // Iterate directly over the Queue without converting to Array (avoids memory allocation)
        foreach (var worldPos in astro.Trail)
        {
            Vector2 p2 = camera.WorldToScreen(worldPos);

            if (p1.HasValue)
            {
                // Calculate alpha based on position in the queue
                float alpha = (float)i / totalPoints;
                byte alphaByte = (byte)(50 + alpha * 200);

                // Assign color to the current vertex
                Rlgl.Color4ub(astro.Color.R, astro.Color.G, astro.Color.B, alphaByte);

                // Draw the segment p.1 -> p.2
                Rlgl.Vertex2f(p1.Value.X, p1.Value.Y);
                Rlgl.Vertex2f(p2.X, p2.Y);
            }

            p1 = p2;
            i++;
        }

        Rlgl.End();
    }

    private static void DrawAstro(Astro astro, Vector2 screenPos, Camera camera, int textAlign)
    {
        float bodyRadius = (float)(astro.Radius / camera.RadiusScale);

        if (bodyRadius < RenderConstants.MinBodyRadius)
        {
            bodyRadius = RenderConstants.MinBodyRadius;
        }

        if (bodyRadius > RenderConstants.MaxBodyRadius)
        {
            bodyRadius = RenderConstants.MaxBodyRadius;
        }

        // Draw the trail only if there are two or more positions
        if (astro.Trail.Count > 1)
            DrawTrail(astro, camera);

        Raylib.DrawCircleV(center: screenPos, radius: bodyRadius, color: astro.Color);

        // Optional: Draw the body's name near the triangle
        Raylib.DrawText(
            astro.Name,
            (int)screenPos.X - textAlign,
            (int)screenPos.Y + textAlign,
            RenderConstants.BodyNameFontSize,
            astro.Color
        );
    }

    private static void DrawRings(Astro selectedAstro, Camera camera)
    {
        if (selectedAstro is { HasRings: true, RingColor: not null })
        {
            Vector2 screenPos = camera.WorldToScreen(selectedAstro.Position);

            double ringScale = camera.DistanceScale * 0.8;
            float innerRadius = (float)(selectedAstro.InnerRingRadius / ringScale);
            float outerRadius = (float)(selectedAstro.OuterRingRadius / ringScale);

            Raylib.DrawRing(
                center: screenPos,
                innerRadius,
                outerRadius,
                startAngle: 0,
                endAngle: 360,
                segments: 50,
                selectedAstro.RingColor.Value
            );
        }
    }

    private static void DrawAsteroidBelt(Vector2 sunScreenPos, Camera camera, string keyName)
    {
        // Asteroid belt (between Mars and Jupiter)
        float asteroidBeltInner = (float)(3.3e11 / camera.DistanceScale);
        float asteroidBeltOuter = (float)(4.9e11 / camera.DistanceScale);

        // Only draw if the belt can be visible on screen
        if (
            !(sunScreenPos.X + asteroidBeltOuter > 0)
            || !(sunScreenPos.X - asteroidBeltOuter < camera.Width)
            || !(sunScreenPos.Y + asteroidBeltOuter > 0)
            || !(sunScreenPos.Y - asteroidBeltOuter < camera.Height)
        )
        {
            return;
        }

        Raylib.DrawRing(
            sunScreenPos,
            asteroidBeltInner,
            asteroidBeltOuter,
            0,
            360,
            RenderConstants.BeltRingSegments,
            RenderConstants.AsteroidBeltColor
        );

        // Draw small asteroids in the belt
        Random rnd = new Random(42); // Fixed seed so they always appear in the same place
        for (int i = 0; i < RenderConstants.AsteroidNum; i++)
        {
            double angle = rnd.NextDouble() * 2 * Math.PI;
            double asteroidRadius = asteroidBeltInner + (rnd.NextDouble() * (asteroidBeltOuter - asteroidBeltInner));

            float xAsteroid = sunScreenPos.X + (float)(Math.Cos(angle) * asteroidRadius);
            float yAsteroid = sunScreenPos.Y + (float)(Math.Sin(angle) * asteroidRadius);

            // Only draw if inside the screen
            if (!(xAsteroid >= 0) || !(xAsteroid <= camera.Width) || !(yAsteroid >= 0) || !(yAsteroid <= camera.Height))
            {
                continue;
            }

            float size = 1.0f + ((float)rnd.NextDouble() * 1.5f);
            Raylib.DrawCircleV(new Vector2(xAsteroid, yAsteroid), size, RenderConstants.AsteroidColor);
        }

        // Text: if zoomed out, outside the belt; otherwise, between the Sun and the ring
        float textRadius = keyName == "Space" ? asteroidBeltOuter * 2f : asteroidBeltInner * 0.9f;
        int textWidth = Raylib.MeasureText("Asteroid belt", 20);
        Raylib.DrawText(
            "Asteroid belt",
            (int)(sunScreenPos.X - ((float)textWidth / 2)),
            (int)(sunScreenPos.Y - textRadius),
            20,
            new Color(139, 125, 107, 150)
        );
    }

    private static void DrawKuiperBelt(Vector2 sunPosScreen, Camera camera)
    {
        float kuiperBeltInner = (float)(5.2e12 / camera.DistanceScale);
        float kuiperBeltOuter = (float)(7.5e12 / camera.DistanceScale);

        // Only draw if the belt can be visible on screen
        if (
            !(sunPosScreen.X + kuiperBeltOuter > 0)
            || !(sunPosScreen.X - kuiperBeltOuter < camera.Width)
            || !(sunPosScreen.Y + kuiperBeltOuter > 0)
            || !(sunPosScreen.Y - kuiperBeltOuter < camera.Height)
        )
        {
            return;
        }

        Raylib.DrawRing(
            sunPosScreen,
            kuiperBeltInner,
            kuiperBeltOuter,
            0,
            360,
            RenderConstants.BeltRingSegments,
            RenderConstants.KuiperBeltColor
        );

        // Draw small objects in the Kuiper belt
        Random rnd = new Random(123); // Different seed for different distribution
        for (int i = 0; i < RenderConstants.NumObjectsKuiper; i++)
        {
            double angle = rnd.NextDouble() * 2 * Math.PI;
            double objRadius = kuiperBeltInner + (rnd.NextDouble() * (kuiperBeltOuter - kuiperBeltInner));

            float xObj = sunPosScreen.X + (float)(Math.Cos(angle) * objRadius);
            float yObj = sunPosScreen.Y + (float)(Math.Sin(angle) * objRadius);

            // Only draw if inside the screen
            if (!(xObj >= 0) || !(xObj <= camera.Width) || !(yObj >= 0) || !(yObj <= camera.Height))
                continue;

            float size = 1.0f + ((float)rnd.NextDouble() * 1.5f);
            Raylib.DrawCircleV(new Vector2(xObj, yObj), size, RenderConstants.KuiperObjectColor);
        }

        // Text inside the ring, between the Sun and the inner edge
        float textRadius = kuiperBeltInner * 0.9f; // 70% of the inner radius
        int textWidth = Raylib.MeasureText("Kuiper belt", 20);
        Raylib.DrawText(
            "Kuiper belt",
            (int)(sunPosScreen.X - ((float)textWidth / 2)),
            (int)(sunPosScreen.Y - textRadius),
            20,
            new Color(100, 100, 120, 150)
        );
    }

    private static void DrawCross(float crossSide, Astro selectedAstro, Camera camera)
    {
        double distCamAstro = Vector2D.Distance(camera.Position, selectedAstro.Position);
        bool fixedObj = distCamAstro < 1e7;

        Color crossColor;
        // Get the center of the screen
        Vector2 center = camera.Center;

        if (fixedObj)
        {
            // Smooth oscillation (Sine gives values between -1 and 1)
            float oscillation = (float)Math.Sin(Raylib.GetTime() * RenderConstants.CrossPulseSpeed);
            float expansion = oscillation * RenderConstants.CrossExpansionAmplitude;
            float finalSide = crossSide + expansion;

            // 2. Convert range [-1, 1] to [0.2, 1.0] so it never fully disappears
            float opacityFactor = (oscillation * 0.4f) + 0.6f;

            // 3. Apply opacity to the body's original color
            crossColor = new Color(
                Color.DarkGray.R,
                Color.DarkGray.G,
                Color.DarkGray.B,
                a: (byte)(opacityFactor * 255)
            );
            Raylib.DrawLineV(center + new Vector2(finalSide, 0), center + new Vector2(-finalSide, 0), crossColor);
            Raylib.DrawLineV(center + new Vector2(0, finalSide), center + new Vector2(0, -finalSide), crossColor);
        }
        else
        {
            crossColor = Color.DarkGray;
            Raylib.DrawLineV(center + new Vector2(crossSide, 0), center + new Vector2(-crossSide, 0), crossColor);
            Raylib.DrawLineV(center + new Vector2(0, crossSide), center + new Vector2(0, -crossSide), crossColor);
        }
    }

    private static void DrawStars(Camera camera, StarList stars)
    {
        // Calculate visible area in world coordinates for early culling
        double worldWidth = camera.Width * camera.DistanceScale;
        double worldHeight = camera.Height * camera.DistanceScale;
        double margin = Math.Max(worldWidth, worldHeight) * 0.1; // 10% margin

        double minX = camera.Position.X - (worldWidth / 2) - margin;
        double maxX = camera.Position.X + (worldWidth / 2) + margin;
        double minY = camera.Position.Y - (worldHeight / 2) - margin;
        double maxY = camera.Position.Y + (worldHeight / 2) + margin;

        // Use Rlgl to draw all stars in a single batch (Batching)
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.SetTexture(0); // Default white texture

        foreach (var star in stars.Stars)
        {
            // Culling in world coordinates
            double starX = star.Position.X;
            double starY = star.Position.Y;

            if (starX < minX || starX > maxX || starY < minY || starY > maxY)
            {
                continue;
            }

            // Transform from world coordinates to screen coordinates
            Vector2 screenPos = camera.WorldToScreen(star.Position);

            byte brightness = (byte)(star.Brightness * 255);
            Rlgl.Color4ub(brightness, brightness, brightness, 255);

            float r = star.Size;

            // Draw a Quad (square) centered at the position
            Rlgl.Vertex2f(screenPos.X - r, screenPos.Y - r);
            Rlgl.Vertex2f(screenPos.X - r, screenPos.Y + r);
            Rlgl.Vertex2f(screenPos.X + r, screenPos.Y + r);
            Rlgl.Vertex2f(screenPos.X + r, screenPos.Y - r);
        }

        Rlgl.End();
    }

    private void DrawInfoText(Camera camera)
    {
        Raylib.DrawText(
            text: "N-Bodies Simulator | Press Escape (Esc) to exit | Press F1 to open Help Menu",
            posX: 10,
            posY: 10,
            fontSize: 20,
            color: Color.White
        );

        Raylib.DrawText(
            text: $"FPS {Raylib.GetFPS()}",
            posX: camera.Width - 200,
            posY: 30,
            fontSize: 20,
            color: Color.Green
        );
    }

    private static void DrawBodies(
        List<Astro> astros,
        Camera camera,
        Astro selectedAstro,
        string keyName,
        float sunRadiusAtScale,
        Vector2 sunPosScreen,
        int textAlign
    )
    {
        Rectangle screenBounds = new Rectangle(0, 0, camera.Width, camera.Height);

        foreach (Astro astro in astros)
        {
            // If it's a satellite and its parent planet is not selected, skip
            if (astro.ParentId.HasValue && astro.ParentId.Value != selectedAstro.Id)
            {
                continue;
            }

            Vector2 screenPos = camera.WorldToScreen(astro.Position);

            // Calculate distance on screen from the Sun to the body (in pixels)
            float distFromSun = Vector2.Distance(sunPosScreen, screenPos);

            // If we are in wide view or viewing the Sun, and the body is inside the solar disk, hide it
            if ((keyName == "Space" || selectedAstro.Id == 0) && astro.Id != 0 && distFromSun < sunRadiusAtScale)
            {
                continue;
            }

            bool onScreen = Raylib.CheckCollisionPointRec(screenPos, screenBounds);

            if (!onScreen)
            {
                RenderSystem.DrawTriangles(screenPos, camera, astro);
            }
            else
            {
                RenderSystem.DrawAstro(astro, screenPos, camera, textAlign);
            }
        }
    }

    private static void DrawAsteroids(Camera camera, string keyName, Vector2 sunPosScreen)
    {
        // Asteroid belts (centered on the Sun)

        // Asteroid belt
        DrawAsteroidBelt(sunPosScreen, camera, keyName);

        // Kuiper belt (beyond Neptune, from 35 AU to 50 AU)
        DrawKuiperBelt(sunPosScreen, camera);
    }

    private (float sunRadiusAtScale, Vector2 sunPosScreen) GetSun(List<Astro> astros, Camera camera)
    {
        _sun ??= astros.First(static a => a.Id == 0);

        float sunRadiusAtScale = GetSunRadius(_sun, camera.RadiusScale);
        Vector2 sunPosScreen = GetSunPositionScreen(_sun, camera);

        return (sunRadiusAtScale, sunPosScreen);
    }

    private static void HelpMenu(Camera camera, List<Astro> astros)
    {
        Color backColor = new Color(0, 0, 0, 200);
        // Rectangle dimensions
        int initialPosX = (int)(camera.Width * 0.15);
        int initialPosY = (int)(camera.Height * 0.15);
        int menuWidth = camera.Width - 2 * initialPosX;
        int menuHeight = camera.Height - 2 * initialPosY;

        // Margin
        const int margin = 30;

        // Back rectangle for the help menu
        Raylib.DrawRectangle(
            posX: initialPosX,
            posY: initialPosY,
            width: menuWidth,
            height: menuHeight,
            color: backColor);

        Raylib.DrawRectangleLines(
            posX: initialPosX,
            posY: initialPosY,
            width: menuWidth,
            height: menuHeight,
            color: Color.White);

        // Vertical line
        Raylib.DrawLine(
            startPosX: camera.Width / 2,
            startPosY: initialPosY + 2 * margin,
            endPosX: camera.Width / 2,
            endPosY: menuHeight + initialPosY - 2 * margin,
            color: Color.White);

        // Title
        const string title = "Simulator Controls";
        int titleWidth = Raylib.MeasureText(title, 20);
        int posXCentered = (camera.Width - titleWidth) / 2 ;

        Raylib.DrawText(
            text: title,
            posX: posXCentered,
            posY: initialPosY + margin,
            fontSize: 20,
            color: Color.White);
        const string exitHelp = "Press F1 to hide the Help Menu";
        int exitHelpWidth = Raylib.MeasureText(exitHelp, 20);
        int posXexitHelp = initialPosX + menuWidth - exitHelpWidth - 2 * margin;

        Raylib.DrawText(
            text: exitHelp,
            posX: posXexitHelp,
            posY: initialPosY + margin,
            fontSize: 20,
            color: Color.White);

        // Layout constants
        const int lineHeight = 50;
        const int itemsPerColumn = 5;
        int contentStartY = initialPosY + 3 * margin;

        // --- Left side: planet keys ---
        int leftColX = initialPosX + margin;
        int rightColX = initialPosX + menuWidth / 4 + margin;

        Raylib.DrawText(
            text: "Keys to select the view:",
            posX: leftColX,
            posY: contentStartY,
            fontSize: 20,
            color: Color.White);

        int astroStartY = contentStartY + lineHeight;

        for (int i = 0; i < 9; i++)
        {
            Astro astro = astros.First(a => a.Id == i);
            int col = i / itemsPerColumn;
            int row = i % itemsPerColumn;
            int posX = col == 0 ? leftColX : rightColX;
            int posY = astroStartY + row * lineHeight;

            Raylib.DrawText(
                text: $"{astro.Id}: {astro.Name}",
                posX: posX,
                posY: posY,
                fontSize: 20,
                color: Color.White);
        }

        Raylib.DrawText(
            text: "9: Easter Egg",
            posX: rightColX,
            posY: astroStartY + 4 * lineHeight,
            fontSize: 20,
            color: Color.White);

        int nextY = astroStartY + itemsPerColumn * lineHeight;

        Raylib.DrawText(
            text: "Space: Wide View of the Solar System",
            posX: leftColX,
            posY: nextY,
            fontSize: 20,
            color: Color.White);

        // --- Right side: contextual menus ---
        int rightSideX = camera.Width / 2 + margin;

        Raylib.DrawText(
            text: "Toggle Time Menu: T",
            posX: rightSideX,
            posY: contentStartY,
            fontSize: 20,
            color: Color.White);
        Raylib.DrawText(
            text: "Toggle Energy Menu: E",
            posX: rightSideX,
            posY: contentStartY + lineHeight,
            fontSize: 20,
            color: Color.White);
        Raylib.DrawText(
            text: "Toggle Body HUD: H",
            posX: rightSideX,
            posY: contentStartY + 2 * lineHeight,
            fontSize: 20,
            color: Color.White);
    }

    private void EnergyMenu(Camera camera)
    {
        const int lineHeight = 30;
        const int startY = 60;
        int posX = camera.Width - 450;

        string[] labels =
        [
            $"Energy = {_diff._energy:E3}",
            $"EnergyDiff = {_diff._energyDiff:E3}",
            $"EnergyDiffRel = {_diff._energyDiffRel:E3}",
            $"Accumulated EnergyDiffRel = {_diff._accumulatedEnergyDiffRel:E3}"
        ];

        for (int i = 0; i < labels.Length; i++)
        {
            Raylib.DrawText(
                text: labels[i],
                posX: posX,
                posY: startY + i * lineHeight,
                fontSize: 20,
                color: Color.White
            );
        }
    }

    private void UpdateDisplayValues((double, double, double, double) lastEnergyCalc, double simulatedTime, Astro selectedAstro, List<Astro> astros)
    {
        if (_energyDiffAccumulator >= Raylib.GetFPS())
        {
            _timePassed = simulatedTime / PhysicsConstants.TerrestialYear;
            _diff = lastEnergyCalc;
            _astroData = [
                ("Name", selectedAstro.Name),
                ("Mass", selectedAstro.Mass.ToString("E3") + " kg"),
                ("Velocity", selectedAstro.Velocity.Length().ToString("E3") + " m/s"),
                ("Position", selectedAstro.Position.Length().ToString("E3") + " m"),
                ("Radius", selectedAstro.Radius.ToString("E3") + " m")
            ];
            _satelliteData.Clear();
            foreach (Astro satellite in astros.Where(a => a.ParentId == selectedAstro.Id))
            {
                _satelliteData.Add(
                [("Name", satellite.Name),
                    ("Mass", satellite.Mass.ToString("E3") + " kg"),
                    ("Velocity", satellite.Velocity.Length().ToString("E3") + " m/s"),
                    ("Position", satellite.Position.Length().ToString("E3") + " m"),
                    ("Radius", satellite.Radius.ToString("E3") + " m")]);
            }
            _energyDiffAccumulator = 0;
        }
        _energyDiffAccumulator++;
    }

    private void TimeMenu()
    {
        Raylib.DrawText(
            text: $"Simulated time: {_timePassed:F3} years",
            posX: 10,
            posY: 40,
            fontSize: 20,
            color: Color.White
        );
    }

    private void DrawHud(Camera camera)
    {
        const int margin = 20;
        const int lineHeight = 50;
        const int hudWidth = 400;
        int hudHeight = _astroData.Length * lineHeight + 2 * margin;

        int initialPosX = camera.Width - hudWidth - margin;
        int initialPosY = camera.Height - hudHeight - margin;

        Raylib.DrawRectangle(initialPosX, initialPosY, hudWidth, hudHeight, new Color(0, 0, 0, 200));
        Raylib.DrawRectangleLines(initialPosX, initialPosY, hudWidth, hudHeight, Color.White);

        for (int i = 0; i < _astroData.Length; i++)
        {
            Raylib.DrawText(
                text: $"{_astroData[i].label}: {_astroData[i].value}",
                posX: initialPosX + margin,
                posY: initialPosY + i * lineHeight + margin,
                fontSize: 20,
                color: Color.White);
        }

        if (_satelliteData.Count == 0) return;
        int satelliteHudHeight = (_satelliteData.Count * _satelliteData[0].Length) * lineHeight + margin;
        int satelliteHudYStart = camera.Height - satelliteHudHeight - margin;
        Raylib.DrawRectangle(margin, satelliteHudYStart, hudWidth, satelliteHudHeight, new Color(0, 0, 0, 200));
        Raylib.DrawRectangleLines(margin, satelliteHudYStart, hudWidth, satelliteHudHeight, Color.White);

        for(int i = 0; i < _satelliteData.Count; i++)
        {
            (string label, string? value)[] satData = _satelliteData[i];

            int basePosY = satelliteHudYStart + margin + i * satData.Length * lineHeight;

            if (i > 0)
            {
                int actualPosY = satelliteHudYStart + i * (satData.Length * lineHeight);
                Raylib.DrawLine(2 * margin, actualPosY, hudWidth, actualPosY, Color.White);
            }

            for (int j = 0; j < satData.Length; j++)
            {
                Raylib.DrawText(
                    text: $"{satData[j].label}: {satData[j].value}",
                    posX: 2 * margin,
                    posY: basePosY + j * lineHeight,
                    fontSize: 20,
                    color: Color.White);
            }
        }
    }
    
    public void Draw(
        List<Astro> astros,
        Camera camera,
        Astro selectedAstro,
        float crossSideLength,
        int textAlign,
        StarList stars,
        string keyName,
        (double, double, double, double) lastEnergyCalc,
        double simulatedTime
    )
    {
        UpdateDisplayValues(lastEnergyCalc, simulatedTime, selectedAstro, astros);

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        //Background: stars
        DrawStars(camera, stars);

        // Draw Text info
        DrawInfoText(camera);

        // Calculate sun radius and position in Screen before drawing objects
        (float sunRadiusAtScale, Vector2 sunPosScreen) = GetSun(astros, camera);

        // Asteroid belts
        DrawAsteroids(camera, keyName, sunPosScreen);

        // Drawing the different bodies of the Solar System
        DrawBodies(astros, camera, selectedAstro, keyName, sunRadiusAtScale, sunPosScreen, textAlign);

        // Draw rings if it has them
        DrawRings(selectedAstro, camera);

        // Satellite Cross
        DrawCross(crossSideLength, selectedAstro, camera);

        // Energy menu
        if (EnergyMenuActive) EnergyMenu(camera);

        // Time menu
        if (TimeMenuActive) TimeMenu();

        // Planet HUD
        if (ShowBodyHud) DrawHud(camera);

        // Help menu
        if (HelpMenuActive) HelpMenu(camera, astros);

        Raylib.EndDrawing();
    }
}
