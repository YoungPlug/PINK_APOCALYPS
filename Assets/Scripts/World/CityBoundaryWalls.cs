using UnityEngine;

namespace PinkApocalypse.World
{
    /// <summary>Builds invisible collision walls around the imported city bounds.</summary>
    public static class CityBoundaryWalls
    {
        private const float WallThickness = 1f;
        private const float ExtraWallHeight = 30f;

        public static void CreateAround(GameObject cityRoot)
        {
            if (cityRoot == null || cityRoot.transform.Find("Invisible City Boundaries") != null) return;

            Renderer[] renderers = cityRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Transform group = new GameObject("Invisible City Boundaries").transform;
            group.SetParent(cityRoot.transform, false);

            float wallHeight = Mathf.Max(bounds.size.y + ExtraWallHeight, ExtraWallHeight);
            float wallCenterY = bounds.min.y + wallHeight * 0.5f;
            float halfThickness = WallThickness * 0.5f;
            float xSpan = bounds.size.x + WallThickness * 2f;
            float zSpan = bounds.size.z + WallThickness * 2f;

            CreateWall(group, "North", new Vector3(bounds.center.x, wallCenterY, bounds.max.z + halfThickness),
                new Vector3(xSpan, wallHeight, WallThickness));
            CreateWall(group, "South", new Vector3(bounds.center.x, wallCenterY, bounds.min.z - halfThickness),
                new Vector3(xSpan, wallHeight, WallThickness));
            CreateWall(group, "East", new Vector3(bounds.max.x + halfThickness, wallCenterY, bounds.center.z),
                new Vector3(WallThickness, wallHeight, zSpan));
            CreateWall(group, "West", new Vector3(bounds.min.x - halfThickness, wallCenterY, bounds.center.z),
                new Vector3(WallThickness, wallHeight, zSpan));
        }

        private static void CreateWall(Transform parent, string side, Vector3 worldPosition, Vector3 size)
        {
            var wall = new GameObject($"{side} Invisible Wall");
            wall.transform.SetParent(parent, true);
            wall.transform.position = worldPosition;
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = size;
        }
    }
}
