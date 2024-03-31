namespace PTSharpCore
{
    public enum LightMode
    {
        LightModeRandom, // Randomly select any type of light source
        LightModeAll, // Include all types of light sources
        LightModePoint, // Only include point lights
        LightModeDirectional, // Only include directional lights    
        LightModeSpot, // Only include spot lights
        LightModeArea // Only include area lights
    }
}
