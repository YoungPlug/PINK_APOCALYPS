using System.Collections.Generic;
using UnityEngine;

namespace PinkApocalypse.World
{
    /// <summary>Finds the car meshes in the imported city, gives them colliders and traffic motion.</summary>
    public static class CityTraffic
    {
        public static void Configure(GameObject cityRoot)
        {
            if (cityRoot == null) return;

            Transform[] transforms = cityRoot.GetComponentsInChildren<Transform>(true);
            var cars = new List<VehicleName>();
            foreach (Transform item in transforms)
                if (TryParseVehicleName(item, out VehicleName vehicle)) cars.Add(vehicle);

            foreach (VehicleName car in cars)
            {
                if (car.Transform == null || car.Transform.GetComponent<TrafficCarController>() != null) continue;

                AttachVehicleParts(car, transforms);
                Renderer[] renderers = car.Transform.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;

                Bounds worldBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) worldBounds.Encapsulate(renderers[i].bounds);
                Bounds localBounds = ToLocalBounds(car.Transform, worldBounds);
                if (localBounds.size.x < 0.3f || localBounds.size.z < 0.3f) continue;

                foreach (MeshCollider meshCollider in car.Transform.GetComponentsInChildren<MeshCollider>(true))
                {
                    meshCollider.enabled = false;
                    Object.Destroy(meshCollider);
                }

                BoxCollider box = car.Transform.gameObject.AddComponent<BoxCollider>();
                box.center = localBounds.center;
                box.size = new Vector3(localBounds.size.x, Mathf.Max(0.5f, localBounds.size.y * 0.68f), localBounds.size.z);
                box.center = new Vector3(localBounds.center.x,
                    localBounds.min.y + localBounds.size.y * 0.59f, localBounds.center.z);
                car.Transform.gameObject.AddComponent<TrafficCarController>();
            }
        }

        private static void AttachVehicleParts(VehicleName car, Transform[] transforms)
        {
            foreach (Transform item in transforms)
            {
                if (item == null || item == car.Transform || item.IsChildOf(car.Transform)) continue;
                if (!TryParseInstanceSuffix(item.name, out string canonicalName, out string suffix)) continue;
                if (!string.Equals(suffix, car.Suffix, System.StringComparison.Ordinal)) continue;

                bool isWheel = canonicalName.StartsWith(car.CanonicalName + "_Wheel_", System.StringComparison.OrdinalIgnoreCase);
                bool isSpoiler = string.Equals(canonicalName, car.CanonicalName + "_Spoiler", System.StringComparison.OrdinalIgnoreCase);
                if (isWheel || isSpoiler) item.SetParent(car.Transform, true);
            }
        }

        private static bool TryParseVehicleName(Transform item, out VehicleName vehicle)
        {
            vehicle = default;
            string name = item.name;
            if (!TryParseInstanceSuffix(name, out string canonicalName, out string suffix)) return false;
            if (canonicalName.Contains("_Wheel_") || canonicalName.EndsWith("_Spoiler", System.StringComparison.OrdinalIgnoreCase)) return false;

            bool isCar = canonicalName.StartsWith("Car_", System.StringComparison.OrdinalIgnoreCase)
                || canonicalName.StartsWith("Futuristic_Car_", System.StringComparison.OrdinalIgnoreCase);
            bool isVan = string.Equals(canonicalName, "Van", System.StringComparison.OrdinalIgnoreCase);
            if (!isCar && !isVan) return false;

            vehicle = new VehicleName(item, canonicalName, suffix);
            return true;
        }

        private static bool TryParseInstanceSuffix(string name, out string canonicalName, out string suffix)
        {
            canonicalName = name;
            suffix = string.Empty;
            int dot = name.LastIndexOf('.');
            if (dot < 0 || name.Length - dot != 4) return true;
            for (int i = dot + 1; i < name.Length; i++)
                if (!char.IsDigit(name[i])) return true;
            canonicalName = name.Substring(0, dot);
            suffix = name.Substring(dot);
            return true;
        }

        private static Bounds ToLocalBounds(Transform target, Bounds worldBounds)
        {
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 worldPoint = center + Vector3.Scale(extents, new Vector3(x, y, z));
                Vector3 localPoint = target.InverseTransformPoint(worldPoint);
                min = Vector3.Min(min, localPoint);
                max = Vector3.Max(max, localPoint);
            }
            return new Bounds((min + max) * 0.5f, max - min);
        }

        private readonly struct VehicleName
        {
            public readonly Transform Transform;
            public readonly string CanonicalName;
            public readonly string Suffix;

            public VehicleName(Transform transform, string canonicalName, string suffix)
            {
                Transform = transform;
                CanonicalName = canonicalName;
                Suffix = suffix;
            }
        }
    }
}
