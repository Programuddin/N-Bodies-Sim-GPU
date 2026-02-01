/*
 * This class generates an object responsible for reading user instructions, processing them, and adjusting the camera
 * values and simulation configuration. Depending on the key the user presses, it returns a configuration from the
 * dictionary containing the settings for each body with its corresponding keyboard key.
 */

using NBodiesSim.Source.Models.DTOs;
using Raylib_cs;

namespace NBodiesSim.Source.Systems;

internal class InputSystems
{
    private readonly Dictionary<KeyboardKey, AstroConfig> _cameraConfigs;
    public InputSystems(Dictionary<KeyboardKey, AstroConfig> cameraConfigs)
    {
        _cameraConfigs = cameraConfigs;
    }

    public AstroConfig? ProcessInput(RenderSystem renderSystem)
    {
        // Set Camera
        int key = Raylib.GetKeyPressed();
        if (key != (int)KeyboardKey.Null && _cameraConfigs.TryGetValue((KeyboardKey)key, out AstroConfig config))
        {
            return config;
        }

        switch (key)
        {
            case (int)KeyboardKey.F1:
                renderSystem.HelpMenuActive = !renderSystem.HelpMenuActive;
                break;
            case (int)KeyboardKey.E:
                renderSystem.EnergyMenuActive = !renderSystem.EnergyMenuActive;
                break;
            case (int)KeyboardKey.T:
                renderSystem.TimeMenuActive = !renderSystem.TimeMenuActive;
                break;
        }

        return null;

    }

}