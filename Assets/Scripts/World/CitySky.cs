using UnityEngine;
using UnityEngine.Rendering;

namespace PinkApocalypse.World
{
    /// <summary>Sets a soft pink twilight sky and uses it for scene ambient lighting.</summary>
    public static class CitySky
    {
        private static Material runtimeSkybox;

        public static void Apply()
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogWarning("Procedural skybox shader is unavailable; keeping the current sky.");
                return;
            }

            if (runtimeSkybox == null)
            {
                runtimeSkybox = new Material(shader)
                {
                    name = "Pink Apocalypse Twilight Sky"
                };
                runtimeSkybox.SetFloat("_SunDisk", 2f);
                runtimeSkybox.SetFloat("_SunSize", 0.035f);
                runtimeSkybox.SetFloat("_SunSizeConvergence", 6f);
                runtimeSkybox.SetFloat("_AtmosphereThickness", 1.05f);
                runtimeSkybox.SetColor("_SkyTint", new Color(0.86f, 0.56f, 0.78f));
                runtimeSkybox.SetColor("_GroundColor", new Color(0.23f, 0.15f, 0.25f));
                runtimeSkybox.SetFloat("_Exposure", 1.15f);
            }

            RenderSettings.skybox = runtimeSkybox;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.8f;
            RenderSettings.fogColor = new Color(0.34f, 0.22f, 0.34f);

            if (RenderSettings.sun == null)
            {
                foreach (Light sceneLight in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (sceneLight.type != LightType.Directional) continue;
                    RenderSettings.sun = sceneLight;
                    break;
                }
            }

            DynamicGI.UpdateEnvironment();
        }
    }
}
