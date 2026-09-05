using System.Collections.Generic;
using NeonBreach.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonBreach
{
    public sealed class ArenaWorld
    {
        public const int WorldLayer = 10;
        public static readonly Color Cyan = new Color(0.10f, 0.94f, 1f);
        public static readonly Color Orange = new Color(1f, 0.35f, 0.10f);
        public Transform Root { get; private set; }
        public GridPathfinder Navigation { get; private set; }
        public Material Dark, Metal, Concrete, Teal, Amber, Red, White, Floor;
        public readonly List<Vector3> SpawnPoints = new List<Vector3>();
        readonly List<Object> resources = new List<Object>();
        public Vector3 PlayerSpawn => new Vector3(0, 0.15f, -20);

        public Material Material(string name, Color color, bool glow = false, float metallic = 0.2f)
        {
            var mat = new Material(Shader.Find("Standard")) { name = name, color = color };
            mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Glossiness", 0.45f);
            if (glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 2f); }
            resources.Add(mat); return mat;
        }

        public void Build(Transform parent)
        {
            Root = new GameObject("ARENA // Sector 07").transform; Root.SetParent(parent);
            Navigation = new GridPathfinder(50, 50);
            Dark = Material("Obsidian alloy", new Color(0.055f, 0.075f, 0.10f), false, 0.7f);
            Metal = Material("Brushed blue steel", new Color(0.16f, 0.23f, 0.29f), false, 0.65f);
            Concrete = Material("Slate concrete", new Color(0.105f, 0.145f, 0.18f));
            Teal = Material("Ion cyan", Cyan, true); Amber = Material("Warning amber", Orange, true);
            Red = Material("Hostile signal", new Color(1f, 0.06f, 0.15f), true);
            White = Material("Cold white", new Color(0.72f, 0.91f, 1f), true);
            Floor = Material("Technical grid", new Color(0.22f, 0.3f, 0.35f), false, 0.5f);
            var texture = new Texture2D(128, 128) { name = "Procedural floor grid", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float v = x < 2 || y < 2 ? 0.60f : 0.25f;
                if ((x > 57 && x < 70 && y > 62 && y < 65) || (y > 57 && y < 70 && x > 62 && x < 65)) v = 0.45f;
                texture.SetPixel(x, y, new Color(v, v, v));
            }
            texture.Apply(); resources.Add(texture); Floor.mainTexture = texture; Floor.mainTextureScale = new Vector2(27, 27);
            Box("Floor", new Vector3(0, -0.3f, 0), new Vector3(54, 0.6f, 54), Floor);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Boundary", new Vector3(side * 26.5f, 3, 0), new Vector3(1, 6, 54), Concrete);
                Box("Boundary", new Vector3(0, 3, side * 26.5f), new Vector3(54, 6, 1), Concrete);
                Box("Perimeter light", new Vector3(side * 25.95f, 0.2f, 0), new Vector3(0.08f, 0.1f, 52), Teal, false);
                Box("Perimeter light", new Vector3(0, 0.2f, side * 25.95f), new Vector3(52, 0.1f, 0.08f), Teal, false);
                for (int i = -24; i <= 24; i += 6)
                {
                    Box("Wall rib", new Vector3(i, 3.2f, side * 25.8f), new Vector3(0.35f, 6.4f, 0.7f), Metal);
                    Box("Wall rib", new Vector3(side * 25.8f, 3.2f, i), new Vector3(0.7f, 6.4f, 0.35f), Metal);
                    Box("Status bar", new Vector3(i, 4.2f, side * 25.38f), new Vector3(2.6f, 0.12f, 0.08f), Teal, false);
                }
            }
            // Obstacles are registered in the same grid used by enemy pathfinding.
            Cover(new Vector3(-10, 0, -12), new Vector3(7, 2.8f, 3.5f));
            Cover(new Vector3(10, 0, -12), new Vector3(7, 2.8f, 3.5f));
            Cover(new Vector3(-10, 0, 12), new Vector3(7, 2.8f, 3.5f));
            Cover(new Vector3(10, 0, 12), new Vector3(7, 2.8f, 3.5f));
            Cover(new Vector3(-17, 0, 0), new Vector3(3, 3.8f, 7));
            Cover(new Vector3(17, 0, 0), new Vector3(3, 3.8f, 7));
            Cover(new Vector3(-6, 0, -3), new Vector3(3, 1.25f, 2.5f));
            Cover(new Vector3(6, 0, 3), new Vector3(3, 1.25f, 2.5f));
            // Secondary lanes and elevated sightline structures create alternate flanking routes.
            Cover(new Vector3(-20, 0, 11), new Vector3(4, 2.2f, 2.5f));
            Cover(new Vector3(20, 0, -11), new Vector3(4, 2.2f, 2.5f));
            Cover(new Vector3(-2, 0, 20), new Vector3(8, 1.8f, 2.2f));
            Cover(new Vector3(2, 0, -20), new Vector3(8, 1.8f, 2.2f));
            Box("North catwalk", new Vector3(0, 4.4f, 15), new Vector3(12, 0.35f, 2.4f), Metal);
            Box("South catwalk", new Vector3(0, 4.4f, -15), new Vector3(12, 0.35f, 2.4f), Metal);
            Box("Catwalk rail", new Vector3(-6, 5.1f, 15), new Vector3(0.18f, 1.2f, 2.4f), Teal, false);
            Box("Catwalk rail", new Vector3(6, 5.1f, -15), new Vector3(0.18f, 1.2f, 2.4f), Amber, false);
            Cover(Vector3.zero, new Vector3(3, 1.1f, 3));
            Box("Reactor housing", new Vector3(0, 2.6f, 0), new Vector3(1.5f, 3, 1.5f), Dark);
            Box("Ion core", new Vector3(0, 3, 0), new Vector3(0.72f, 4.4f, 0.72f), Teal, false);
            for (int i = 0; i < 5; i++) Box("Reactor fins", new Vector3(0, 1.4f + i * 0.8f, 0), new Vector3(2.3f, 0.15f, 2.3f), Metal, false);
            PointLight(new Vector3(0, 4, 0), Cyan, 3, 12);
            foreach (var p in new[] { new Vector3(-22, 0, 22), new Vector3(22, 0, 22), new Vector3(-22, 0, -22), new Vector3(22, 0, -22), new Vector3(0, 0, 23) })
            {
                SpawnPoints.Add(p + Vector3.up * 0.15f);
                Box("Deployment pad", p + Vector3.up * 0.035f, new Vector3(3.4f, 0.07f, 3.4f), Dark, false);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("Pad glow", p + new Vector3(side * 1.5f, 0.09f, 0), new Vector3(0.07f, 0.06f, 3), Amber, false);
                    Box("Pad glow", p + new Vector3(0, 0.09f, side * 1.5f), new Vector3(3, 0.06f, 0.07f), Amber, false);
                }
            }
            // Upper gantries and distant buildings give the arena a full 3D silhouette.
            for (int s = -1; s <= 1; s += 2)
            {
                Box("Gantry", new Vector3(s * 18, 8.5f, 0), new Vector3(0.5f, 0.5f, 54), Dark, false);
                Box("Gantry strip", new Vector3(s * 18, 8.2f, 0), new Vector3(0.14f, 0.08f, 48), White, false);
                for (int z = -18; z <= 18; z += 12) Box("Gantry leg", new Vector3(s * 25.5f, 4, z), new Vector3(0.4f, 8, 0.4f), Metal);
                for (int x = -34; x <= 34; x += 8)
                {
                    float h = 8 + Mathf.Abs((x * 13 + s * 7) % 17);
                    Box("Skyline", new Vector3(x, h / 2, s * 38), new Vector3(5, h, 6), Dark, false);
                    Box("Skyline beacon", new Vector3(x, h + 0.1f, s * 38), new Vector3(3.5f, 0.12f, 3.5f), Teal, false);
                }
            }
            Label("S E C T O R   /   0 7", new Vector3(0, 4.1f, 25.88f), 0.19f, Cyan);
            Label("N E O N   B R E A C H", new Vector3(-12, 3.8f, 25.88f), 0.085f, Color.white);
            var sun = new GameObject("Moonlight").AddComponent<Light>(); sun.transform.SetParent(Root);
            sun.type = LightType.Directional; sun.color = new Color(0.48f, 0.70f, 0.92f); sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(48, -32, 0); sun.shadows = LightShadows.Soft;
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                PointLight(new Vector3(x * 13, 5, z * 15), z > 0 ? Cyan : Orange, 2.4f, 16);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.32f, 0.42f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.018f, 0.035f, 0.062f);
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = 0.015f;
            QualitySettings.shadowDistance = 65;
            QualitySettings.shadows = ShadowQuality.All; QualitySettings.antiAliasing = 4;
        }

        void Cover(Vector3 position, Vector3 size)
        {
            Box("Armored cover", position + Vector3.up * size.y / 2, size, Metal);
            Box("Cover top", position + Vector3.up * (size.y + 0.045f), new Vector3(size.x + 0.1f, 0.09f, size.z + 0.1f), Dark);
            Box("Cover identification", position + new Vector3(0, size.y * 0.75f, -size.z / 2 - 0.012f), new Vector3(size.x * 0.6f, 0.085f, 0.025f), Teal, false);
            for (int x = 0; x < 50; x++) for (int z = 0; z < 50; z++)
            {
                Vector3 c = ToWorld(new Cell(x, z));
                if (Mathf.Abs(c.x - position.x) <= size.x / 2 + 0.55f && Mathf.Abs(c.z - position.z) <= size.z / 2 + 0.55f) Navigation.Block(x, z);
            }
        }
        public static Cell ToCell(Vector3 p) { return new Cell(Mathf.Clamp(Mathf.FloorToInt(p.x + 25), 0, 49), Mathf.Clamp(Mathf.FloorToInt(p.z + 25), 0, 49)); }
        public static Vector3 ToWorld(Cell c) { return new Vector3(c.X - 24.5f, 0, c.Z - 24.5f); }
        public GameObject Box(string name, Vector3 position, Vector3 scale, Material material, bool collider = true)
        {
            return Shape(PrimitiveType.Cube, name, Root, position, scale, material, collider);
        }
        public static GameObject Shape(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale, Material mat, bool collider = false, int layer = WorldLayer)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.layer = layer;
            go.transform.SetParent(parent, false); go.transform.localPosition = localPosition; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) { var c = go.GetComponent<Collider>(); c.enabled = false; Object.Destroy(c); }
            return go;
        }
        void Label(string text, Vector3 position, float size, Color color)
        {
            var go = new GameObject(text); go.transform.SetParent(Root); go.transform.position = position;
            var mesh = go.AddComponent<TextMesh>(); mesh.text = text; mesh.characterSize = size; mesh.fontSize = 64;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.color = color;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
        }
        void PointLight(Vector3 position, Color color, float intensity, float range)
        {
            var light = new GameObject("Area light").AddComponent<Light>(); light.transform.SetParent(Root);
            light.transform.position = position; light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range;
        }
        public void Dispose() { foreach (var resource in resources) if (resource != null) Object.Destroy(resource); resources.Clear(); }
    }
}
