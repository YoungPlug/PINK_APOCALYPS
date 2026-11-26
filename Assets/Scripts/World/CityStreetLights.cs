using UnityEngine;

namespace PinkApocalypse.World
{
    /// <summary>Creates alternating pink and purple street lamps around the city.</summary>
    public static class CityStreetLights
    {
        private const float PoleHeight = 4.6f;
        private const float Margin = 5f;
        private static Material poleMaterial;
        private static Material pinkLampMaterial;
        private static Material purpleLampMaterial;

        public static void CreateAround(GameObject cityRoot)
        {
            if (cityRoot == null || cityRoot.transform.Find("Pink and Purple Street Lights") != null) return;

            Renderer[] renderers = cityRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.x < 10f || bounds.size.z < 10f) return;

            EnsureMaterials();

            Transform group = new GameObject("Pink and Purple Street Lights").transform;
            group.SetParent(cityRoot.transform, true);

            float insetX = Mathf.Min(Margin, bounds.size.x * 0.12f);
            float insetZ = Mathf.Min(Margin, bounds.size.z * 0.12f);
            int lightsAlongX = Mathf.Clamp(Mathf.RoundToInt(bounds.size.x / 24f), 1, 4);
            int lightsAlongZ = Mathf.Clamp(Mathf.RoundToInt(bounds.size.z / 24f), 1, 4);
            int colorIndex = 0;

            AddRow(group, bounds, true, bounds.min.z + insetZ, bounds.min.x + insetX,
                bounds.max.x - insetX, lightsAlongX, ref colorIndex);
            AddRow(group, bounds, true, bounds.max.z - insetZ, bounds.min.x + insetX,
                bounds.max.x - insetX, lightsAlongX, ref colorIndex);
            AddRow(group, bounds, false, bounds.min.x + insetX, bounds.min.z + insetZ,
                bounds.max.z - insetZ, lightsAlongZ, ref colorIndex);
            AddRow(group, bounds, false, bounds.max.x - insetX, bounds.min.z + insetZ,
                bounds.max.z - insetZ, lightsAlongZ, ref colorIndex);
        }

        private static void AddRow(Transform parent, Bounds bounds, bool alongX, float fixedAxis,
            float start, float end, int count, ref int colorIndex)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float variableAxis = Mathf.Lerp(start, end, t);
                Vector3 position = alongX
                    ? new Vector3(variableAxis, bounds.min.y, fixedAxis)
                    : new Vector3(fixedAxis, bounds.min.y, variableAxis);
                bool faceInward = alongX ? fixedAxis < bounds.center.z : fixedAxis < bounds.center.x;
                Color color = colorIndex++ % 2 == 0
                    ? new Color(1f, 0.2f, 0.58f)
                    : new Color(0.56f, 0.24f, 1f);
                Material lampMaterial = colorIndex % 2 == 1 ? pinkLampMaterial : purpleLampMaterial;
                CreateLamp(parent, position, faceInward, lampMaterial, color, i, alongX);
            }
        }

        private static void CreateLamp(Transform parent, Vector3 position, bool faceInward,
            Material lampMaterial, Color lightColor, int index, bool alongX)
        {
            Transform pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            pole.name = $"Street Light Pole {index + 1}";
            pole.SetParent(parent, true);
            pole.position = position + Vector3.up * (PoleHeight * 0.5f);
            pole.localScale = new Vector3(0.09f, PoleHeight * 0.5f, 0.09f);
            Object.Destroy(pole.GetComponent<Collider>());
            pole.GetComponent<Renderer>().sharedMaterial = poleMaterial;

            Vector3 inward = alongX
                ? (faceInward ? Vector3.forward : Vector3.back)
                : (faceInward ? Vector3.right : Vector3.left);
            Vector3 lampPosition = position + Vector3.up * (PoleHeight + 0.05f) + inward * 0.42f;

            Transform arm = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            arm.name = "Lamp Arm";
            arm.SetParent(parent, true);
            arm.position = position + Vector3.up * (PoleHeight - 0.08f) + inward * 0.2f;
            arm.rotation = Quaternion.LookRotation(inward, Vector3.up);
            arm.localScale = new Vector3(0.07f, 0.07f, 0.52f);
            Object.Destroy(arm.GetComponent<Collider>());
            arm.GetComponent<Renderer>().sharedMaterial = poleMaterial;

            Transform bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            bulb.name = "Neon Lamp";
            bulb.SetParent(parent, true);
            bulb.position = lampPosition;
            bulb.localScale = Vector3.one * 0.2f;
            Object.Destroy(bulb.GetComponent<Collider>());
            bulb.GetComponent<Renderer>().sharedMaterial = lampMaterial;

            Light pointLight = new GameObject("Colored Street Light").AddComponent<Light>();
            pointLight.transform.SetParent(parent, true);
            pointLight.transform.position = lampPosition;
            pointLight.type = LightType.Point;
            pointLight.color = lightColor;
            pointLight.intensity = 5f;
            pointLight.range = 15f;
            pointLight.shadows = LightShadows.None;
        }

        private static void EnsureMaterials()
        {
            if (poleMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) return;

                poleMaterial = new Material(shader) { name = "Street Light Charcoal" };
                SetColor(poleMaterial, new Color(0.09f, 0.075f, 0.12f));
                pinkLampMaterial = CreateLampMaterial(shader, "Street Light Neon Pink", new Color(1f, 0.16f, 0.52f));
                purpleLampMaterial = CreateLampMaterial(shader, "Street Light Neon Purple", new Color(0.54f, 0.22f, 1f));
            }
        }

        private static Material CreateLampMaterial(Shader shader, string name, Color color)
        {
            Material material = new Material(shader) { name = name };
            SetColor(material, color);
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 2.5f);
            return material;
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }
    }
}
