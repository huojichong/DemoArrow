using System.Collections.Generic;
using UnityEngine;

public class SurfaceArrowDemo : MonoBehaviour
{
    public bool buildOnStart = true;
    public bool setupCamera = true;
    public Material arrowMaterial;

    private static readonly SurfaceArrowBox[] ModelBoxes =
    {
        new SurfaceArrowBox { min = new Vector3Int(0, 0, 0), max = new Vector3Int(9, 9, 3) },
        new SurfaceArrowBox { min = new Vector3Int(0, 3, 3), max = new Vector3Int(9, 9, 6) },
        new SurfaceArrowBox { min = new Vector3Int(0, 6, 6), max = new Vector3Int(9, 9, 9) }
    };

    private static readonly ArrowData[] InitialArrows =
    {
        new ArrowData("0", new[] { V(4.5f, 7.5f, 9f), V(7.5f, 7.5f, 9f) }),
        new ArrowData("1", new[] { V(1.5f, 8.5f, 9f), V(1.5f, 6.5f, 9f) }),

        new ArrowData("2", new[] { V(2.5f, 6f, 8.5f), V(2.5f, 6f, 9f), V(2.5f, 7.5f, 9f) }),
        new ArrowData("3", new[] { V(8.5f, 6f, 8.5f), V(8.5f, 6f, 9f), V(8.5f, 7.5f, 9f) }),

        new ArrowData("4", new[] { V(4.5f, 3.5f, 6f), V(4.5f, 5.5f, 6f) }),
        new ArrowData("5", new[] { V(7.5f, 4.5f, 6f), V(5.5f, 4.5f, 6f) }),

        new ArrowData("6", new[] { V(3.5f, 3f, 5.5f), V(3.5f, 3f, 6f), V(3.5f, 3.5f, 6f), V(2.5f, 3.5f, 6f) }),
        new ArrowData("7", new[] { V(8.5f, 3f, 5.5f), V(8.5f, 3f, 6f), V(8.5f, 5.5f, 6f) }),

        new ArrowData("8", new[] { V(1.5f, 0.5f, 3f), V(1.5f, 2.5f, 3f) }),
        new ArrowData("9", new[] { V(2.5f, 1.5f, 3f), V(4.5f, 1.5f, 3f) }),

        new ArrowData("10", new[] { V(4.5f, 0f, 2.5f), V(4.5f, 0f, 3f), V(4.5f, 0.5f, 3f) }),
        new ArrowData("11", new[] { V(2.5f, 0f, 1.5f), V(4.5f, 0f, 1.5f) }),

        new ArrowData("12", new[] { V(9f, 1.5f, 1.5f), V(9f, 0.5f, 1.5f) }),
        new ArrowData("13", new[] { V(9f, 7.5f, 7.5f), V(9f, 7.5f, 8.5f) }),

        new ArrowData("14", new[] { V(0f, 1.5f, 1.5f), V(0f, 2.5f, 1.5f) }),
        new ArrowData("15", new[] { V(0f, 4.5f, 5.5f), V(0f, 4.5f, 4.5f) }),

        new ArrowData("16", new[] { V(5.5f, 9f, 8.5f), V(4.5f, 9f, 8.5f) }),
        new ArrowData("17", new[] { V(7.5f, 9f, 6.5f), V(7.5f, 9f, 7.5f) })
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<SurfaceArrowManager>() != null ||
            FindObjectOfType<SurfaceArrowDemo>() != null)
        {
            return;
        }

        GameObject demoObject = new GameObject("Surface Arrow Demo");
        SurfaceArrowDemo demo = demoObject.AddComponent<SurfaceArrowDemo>();
        demo.buildOnStart = false;
        demo.BuildDemo();
    }

    private void Start()
    {
        if (buildOnStart)
        {
            BuildDemo();
        }
    }

    [ContextMenu("Build Demo")]
    public void BuildDemo()
    {
        ClearChildren();

        SurfaceArrowManager manager = GetComponent<SurfaceArrowManager>();
        if (manager == null)
        {
            manager = gameObject.AddComponent<SurfaceArrowManager>();
        }

        manager.voxelChunk = null;
        manager.coordinateRoot = transform;
        manager.boxes = new List<SurfaceArrowBox>(ModelBoxes);

        bool[,,] voxels = BuildVoxelData(ModelBoxes);
        GameObject model = new GameObject("DemoModel");
        model.transform.SetParent(transform, false);
        VoxelShellEffectBuilder shell = model.AddComponent<VoxelShellEffectBuilder>();
        shell.useVoxelChunkIfAvailable = false;
        shell.edgeWidth = 0.035f;
        shell.dotRadius = 0.045f;
        shell.BuildEffect(voxels);

        GameObject arrowsRoot = new GameObject("DemoArrows");
        arrowsRoot.transform.SetParent(transform, false);

        Material sharedArrowMaterial = arrowMaterial != null ? arrowMaterial : CreateArrowMaterial();
        for (int i = 0; i < InitialArrows.Length; i++)
        {
            CreateArrow(arrowsRoot.transform, manager, InitialArrows[i], sharedArrowMaterial);
        }

        if (setupCamera)
        {
            SetupCamera();
        }
    }

    [ContextMenu("Clear Demo")]
    public void ClearDemo()
    {
        ClearChildren();
    }

    private static void CreateArrow(Transform parent, SurfaceArrowManager manager, ArrowData data, Material sharedMaterial)
    {
        GameObject arrowObject = new GameObject($"Arrow_{data.id}");
        arrowObject.transform.SetParent(parent, false);

        SurfaceArrow arrow = arrowObject.AddComponent<SurfaceArrow>();
        arrow.manager = manager;
        arrow.material = sharedMaterial;
        arrow.path = new List<Vector3>(data.path);
        arrow.segmentWidth = 0.15f;
        arrow.headRadius = 0.25f;
        arrow.surfaceOffset = 0.018f;
        arrow.RebuildMesh();
        manager.Register(arrow);
    }

    private static bool[,,] BuildVoxelData(IReadOnlyList<SurfaceArrowBox> boxes)
    {
        Vector3Int size = Vector3Int.zero;
        for (int i = 0; i < boxes.Count; i++)
        {
            size.x = Mathf.Max(size.x, boxes[i].max.x);
            size.y = Mathf.Max(size.y, boxes[i].max.y);
            size.z = Mathf.Max(size.z, boxes[i].max.z);
        }

        bool[,,] voxels = new bool[size.x, size.y, size.z];
        for (int i = 0; i < boxes.Count; i++)
        {
            SurfaceArrowBox box = boxes[i];
            int minX = Mathf.Min(box.min.x, box.max.x);
            int minY = Mathf.Min(box.min.y, box.max.y);
            int minZ = Mathf.Min(box.min.z, box.max.z);
            int maxX = Mathf.Max(box.min.x, box.max.x);
            int maxY = Mathf.Max(box.min.y, box.max.y);
            int maxZ = Mathf.Max(box.min.z, box.max.z);

            for (int x = minX; x < maxX; x++)
            for (int y = minY; y < maxY; y++)
            for (int z = minZ; z < maxZ; z++)
            {
                voxels[x, y, z] = true;
            }
        }

        return voxels;
    }

    private void SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        Vector3 modelCenter = transform.TransformPoint(GetBoxesCenter(ModelBoxes));
        camera.transform.position = new Vector3(13f, 12f, -15f);
        camera.transform.rotation = Quaternion.LookRotation(modelCenter - camera.transform.position, Vector3.up);
        camera.fieldOfView = 45f;

        SurfaceOrbitCamera orbit = camera.GetComponent<SurfaceOrbitCamera>();
        if (orbit == null)
        {
            orbit = camera.gameObject.AddComponent<SurfaceOrbitCamera>();
        }

        orbit.target = modelCenter;
        orbit.distance = Vector3.Distance(camera.transform.position, modelCenter);
        orbit.minDistance = 7f;
        orbit.maxDistance = 30f;
        orbit.yaw = -38f;
        orbit.pitch = 28f;

        if (FindObjectOfType<Light>() == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }

    private static Vector3 GetBoxesCenter(IReadOnlyList<SurfaceArrowBox> boxes)
    {
        if (boxes == null || boxes.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < boxes.Count; i++)
        {
            SurfaceArrowBox box = boxes[i];
            min = Vector3.Min(min, box.min);
            max = Vector3.Max(max, box.max);
        }

        return (min + max) * 0.5f;
    }

    private Material CreateArrowMaterial()
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = "Demo Arrow Material";
        material.SetColor("_Color", Color.black);
        material.renderQueue = 3120;
        return material;
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private static Vector3 V(float x, float y, float z)
    {
        return new Vector3(x, y, z);
    }

    private readonly struct ArrowData
    {
        public readonly string id;
        public readonly Vector3[] path;

        public ArrowData(string id, Vector3[] path)
        {
            this.id = id;
            this.path = path;
        }
    }
}
