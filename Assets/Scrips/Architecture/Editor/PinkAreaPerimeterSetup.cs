using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public static class PinkAreaPerimeterSetup
    {
        private const string ScenePath = "Assets/Scenes/PinkArea_Perimeters.unity";
        private const float Thickness = 1f;
        private const float Epsilon = 0.001f;

        // Schematic coordinates derived from the GDD, using the blue corridor as scale reference.
        // This scene is a separate physical space: the shaft is NOT joined to the blue crossing.
        private static readonly (string Name, Rect Bounds)[] Rooms =
        {
            ("01 - Arrival chamber", new Rect(-116, 13, 30, 36)),
            ("02 - Lower connector", new Rect(-104, -1, 8, 14)),
            ("03 - Lower return passage", new Rect(-104, -11, 65, 10)),
            ("04 - Elevator encounter shaft", new Rect(-49, -1, 10, 137)),
            ("05 - Upper left passage", new Rect(-81, 79, 32, 10)),
            ("06 - Salmon side approach", new Rect(-93, 83, 12, 6)),
            ("07 - Right chamber", new Rect(-39, 45, 26, 21)),
            ("08 - Nonlinear connection approach", new Rect(-13, 62, 14, 4))
        };

        // Only the arrival chamber touches blue. The shaft's apparent blue crossing remains solid.
        private static readonly Rect[] Portals =
        {
            new Rect(-86, 13, 2, 14),
            new Rect(-49, 136, 10, 2),
            new Rect(-95, 83, 2, 6)
        };

        [MenuItem("TIC/Setup/Create Or Open Pink Area Perimeters")]
        public static void CreateOrOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath);
                FrameArea();
                return;
            }

            var layer = LayerMask.NameToLayer("Environment");
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (layer < 0 || shader == null)
            {
                Debug.LogError("Pink blockout requires the Environment layer and URP Unlit shader.");
                return;
            }

            const string materialPath = "Assets/Scenes/PinkPerimeterBlockout.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                material.SetColor("_BaseColor", new Color(0.85f, 0.3f, 0.8f));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Pink Area - Perimeters Only").transform;
            foreach (var room in Rooms)
            {
                var group = new GameObject(room.Name).transform;
                group.SetParent(root, false);
                group.position = room.Bounds.position;
                AddEdge(group, room.Bounds, true, false, material, layer);
                AddEdge(group, room.Bounds, true, true, material, layer);
                AddEdge(group, room.Bounds, false, false, material, layer);
                AddEdge(group, room.Bounds, false, true, material, layer);
            }

            var markers = new GameObject("Connections and future encounter markers").transform;
            markers.SetParent(root, false);
            Marker(markers, "Blue corridor connection - arrival only", -86, 20);
            Marker(markers, "Elevator lower terminal - future", -44, 1);
            Marker(markers, "Elevator upper terminal - stops at salmon junction", -44, 136);
            Marker(markers, "Salmon side connection", -93, 86);
            Marker(markers, "Nonlinear return to blue - future", -1, 64);
            Marker(markers, "Blue crossing on reference map - NO CONNECTION", -44, 20);

            var cameraObject = new GameObject("Perimeter overview camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(-57.5f, 62.5f, -10);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 80;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.055f, 0.085f);

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = root.gameObject;
            FrameArea();
            Debug.Log("Pink perimeters saved: eight connected shells, continuous shaft at the blue crossing, and open salmon junctions. No platforms, enemies or elevator behavior added.");
        }

        private static void AddEdge(Transform parent, Rect room, bool horizontal, bool positive,
            Material material, int layer)
        {
            var fixedCoordinate = horizontal
                ? (positive ? room.yMax : room.yMin)
                : (positive ? room.xMax : room.xMin);
            var spans = new List<Vector2>
            {
                horizontal ? new Vector2(room.xMin, room.xMax) : new Vector2(room.yMin, room.yMax)
            };
            foreach (var other in Rooms)
            {
                if (other.Bounds == room) continue;
                CutOpening(spans, other.Bounds, fixedCoordinate, horizontal, positive);
            }
            foreach (var portal in Portals)
                CutOpening(spans, portal, fixedCoordinate, horizontal, positive);

            var outward = positive ? Thickness * 0.5f : -Thickness * 0.5f;
            foreach (var span in spans)
            {
                var length = span.y - span.x;
                if (length <= Epsilon) continue;
                var midpoint = (span.x + span.y) * 0.5f;
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = horizontal ? (positive ? "Ceiling" : "Floor") : (positive ? "East wall" : "West wall");
                wall.layer = layer;
                wall.transform.SetParent(parent, false);
                wall.transform.position = horizontal
                    ? new Vector3(midpoint, fixedCoordinate + outward, 1)
                    : new Vector3(fixedCoordinate + outward, midpoint, 1);
                wall.transform.localScale = horizontal
                    ? new Vector3(length, Thickness, 1) : new Vector3(Thickness, length, 1);
                Object.DestroyImmediate(wall.GetComponent<Collider>());
                wall.AddComponent<BoxCollider2D>();
                wall.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private static void CutOpening(List<Vector2> spans, Rect other, float edge, bool horizontal, bool positive)
        {
            var near = horizontal ? other.yMin : other.xMin;
            var far = horizontal ? other.yMax : other.xMax;
            // Test just outside the edge, so rooms sharing a parallel wall do not erase it.
            var probe = edge + (positive ? Epsilon : -Epsilon);
            if (probe < near || probe > far) return;
            var low = horizontal ? other.xMin : other.yMin;
            var high = horizontal ? other.xMax : other.yMax;
            for (var index = spans.Count - 1; index >= 0; index--)
            {
                var span = spans[index];
                if (high <= span.x || low >= span.y) continue;
                spans.RemoveAt(index);
                if (low > span.x) spans.Add(new Vector2(span.x, Mathf.Min(low, span.y)));
                if (high < span.y) spans.Add(new Vector2(Mathf.Max(high, span.x), span.y));
            }
        }

        private static void Marker(Transform parent, string name, float x, float y)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.position = new Vector3(x, y, 0);
        }

        private static void FrameArea()
        {
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(-57.5f, 62.5f), new Vector3(121, 151, 1)), false);
        }
    }
}
