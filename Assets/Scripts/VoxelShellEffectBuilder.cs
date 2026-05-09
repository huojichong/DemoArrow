using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class VoxelShellEffectBuilder : MonoBehaviour
{
    [Header("Voxel Source")]
    public bool useVoxelChunkIfAvailable = true;
    public int sizeX = 16;
    public int sizeY = 8;
    public int sizeZ = 16;

    [Header("Surface")]
    public Material surfaceMaterial;
    public Color surfaceColor = new Color(0.7f, 0.9f, 1f, 0.25f);

    [Header("Edges")]
    public Material edgeMaterial;
    public Color edgeColor = Color.black;
    [Min(0.001f)]
    public float edgeWidth = 0.04f;
    [Min(0f)]
    public float edgeOffset = 0.004f;

    [Header("Dots")]
    public Material dotMaterial;
    public Color dotColor = Color.black;
    [Min(0.001f)]
    public float dotRadius = 0.055f;
    [Min(0f)]
    public float dotOffset = 0.008f;

    private const string SurfaceName = "ShellSurface";
    private const string EdgesName = "ShellEdges";
    private const string DotsName = "SurfaceDots";

    [ContextMenu("Build Effect")]
    public void BuildEffect()
    {
        bool[,,] voxels = ResolveVoxels();
        BuildEffect(voxels);
    }

    public void BuildEffect(bool[,,] voxels)
    {
        if (voxels == null)
        {
            Debug.LogWarning("VoxelShellEffectBuilder: voxels is null.");
            return;
        }

        ClearGenerated();
        EnsureMaterials();
        ApplyMaterialSettings();

        Mesh surfaceMesh = GreedyMesher.Build(voxels);
        surfaceMesh.name = "VoxelShell_Surface";

        GameObject surface = CreateChild(SurfaceName);
        surface.AddComponent<MeshFilter>().sharedMesh = surfaceMesh;
        surface.AddComponent<MeshRenderer>().sharedMaterial = surfaceMaterial;

        Mesh edgeMesh = BuildEdgeMesh(voxels);
        edgeMesh.name = "VoxelShell_Edges";

        GameObject edges = CreateChild(EdgesName);
        edges.AddComponent<MeshFilter>().sharedMesh = edgeMesh;
        edges.AddComponent<MeshRenderer>().sharedMaterial = edgeMaterial;

        Mesh dotMesh = BuildDotMesh(voxels);
        dotMesh.name = "VoxelShell_Dots";

        GameObject dots = CreateChild(DotsName);
        dots.AddComponent<MeshFilter>().sharedMesh = dotMesh;
        dots.AddComponent<MeshRenderer>().sharedMaterial = dotMaterial;
    }

    [ContextMenu("Clear Generated")]
    public void ClearGenerated()
    {
        DestroyChild(SurfaceName);
        DestroyChild(EdgesName);
        DestroyChild(DotsName);
    }

#if UNITY_EDITOR
    [ContextMenu("Save Generated Prefab")]
    public void SaveGeneratedPrefab()
    {
        string folder = "Assets/SavedMeshes";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "SavedMeshes");
        }

        SaveGeneratedAssets(folder);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}_VoxelShell.prefab");
        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path, InteractionMode.UserAction);
        AssetDatabase.SaveAssets();
        Debug.Log($"Voxel shell prefab saved: {path}");
    }

    private void SaveGeneratedAssets(string folder)
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < meshFilters.Length; i++)
        {
            Mesh mesh = meshFilters[i].sharedMesh;
            if (mesh == null || EditorUtility.IsPersistent(mesh))
            {
                continue;
            }

            string meshPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}_{mesh.name}.asset");
            Mesh savedMesh = Instantiate(mesh);
            AssetDatabase.CreateAsset(savedMesh, meshPath);
            meshFilters[i].sharedMesh = savedMesh;
        }

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].sharedMaterial;
            if (material == null || EditorUtility.IsPersistent(material))
            {
                continue;
            }

            string materialPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}_{material.name}.mat");
            Material savedMaterial = Instantiate(material);
            AssetDatabase.CreateAsset(savedMaterial, materialPath);
            renderers[i].sharedMaterial = savedMaterial;

            if (material == surfaceMaterial)
            {
                surfaceMaterial = savedMaterial;
            }
            else if (material == edgeMaterial)
            {
                edgeMaterial = savedMaterial;
            }
            else if (material == dotMaterial)
            {
                dotMaterial = savedMaterial;
            }
        }
    }
#endif

    private bool[,,] ResolveVoxels()
    {
        if (useVoxelChunkIfAvailable && TryGetComponent(out VoxelChunk chunk) && chunk.voxels != null)
        {
            return chunk.voxels;
        }

        return GenerateStairTestData();
    }

    private bool[,,] GenerateStairTestData()
    {
        bool[,,] voxels = new bool[sizeX, sizeY, sizeZ];
        int stairWidth = Mathf.Max(1, Mathf.Min(4, sizeX));
        int steps = Mathf.Max(1, Mathf.Min(8, sizeZ));
        int stairDepth = Mathf.Max(1, sizeZ / steps);

        for (int step = 0; step < steps; step++)
        {
            int topY = Mathf.Min(step, sizeY - 1);
            int startZ = step * stairDepth;
            int endZ = step == steps - 1 ? sizeZ : Mathf.Min(sizeZ, (step + 1) * stairDepth);

            for (int x = 0; x < stairWidth; x++)
            for (int y = 0; y <= topY; y++)
            for (int z = startZ; z < endZ; z++)
            {
                voxels[x, y, z] = true;
            }
        }

        return voxels;
    }

    private Mesh BuildEdgeMesh(bool[,,] voxels)
    {
        Dictionary<EdgeKey, EdgeInfo> edges = new Dictionary<EdgeKey, EdgeInfo>();
        int sx = voxels.GetLength(0);
        int sy = voxels.GetLength(1);
        int sz = voxels.GetLength(2);

        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
        for (int z = 0; z < sz; z++)
        {
            if (!voxels[x, y, z])
            {
                continue;
            }

            AddFaceEdges(voxels, edges, x, y, z, Vector3Int.right);
            AddFaceEdges(voxels, edges, x, y, z, Vector3Int.left);
            AddFaceEdges(voxels, edges, x, y, z, Vector3Int.up);
            AddFaceEdges(voxels, edges, x, y, z, Vector3Int.down);
            AddFaceEdges(voxels, edges, x, y, z, Vector3Int.forward);
            AddFaceEdges(voxels, edges, x, y, z, Vector3Int.back);
        }

        List<Vector3> meshVertices = new List<Vector3>();
        List<int> meshTriangles = new List<int>();

        foreach (EdgeInfo edge in edges.Values)
        {
            if (!edge.IsVisible)
            {
                continue;
            }

            Vector3 offsetNormal = edge.AverageNormal.normalized;
            if (offsetNormal.sqrMagnitude < 0.0001f)
            {
                offsetNormal = Vector3.up;
            }

            AddEdgePrism(
                meshVertices,
                meshTriangles,
                edge.Start,
                edge.End,
                offsetNormal,
                edgeWidth,
                edgeOffset);
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(meshVertices);
        mesh.SetTriangles(meshTriangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void AddFaceEdges(
        bool[,,] voxels,
        Dictionary<EdgeKey, EdgeInfo> edges,
        int x,
        int y,
        int z,
        Vector3Int normal)
    {
        if (IsSolid(voxels, x + normal.x, y + normal.y, z + normal.z))
        {
            return;
        }

        Vector3[] corners = GetFaceCorners(x, y, z, normal);
        AddEdge(edges, corners[0], corners[1], normal);
        AddEdge(edges, corners[1], corners[2], normal);
        AddEdge(edges, corners[2], corners[3], normal);
        AddEdge(edges, corners[3], corners[0], normal);
    }

    private Mesh BuildDotMesh(bool[,,] voxels)
    {
        Dictionary<Vector3Int, Vector3> points = new Dictionary<Vector3Int, Vector3>();
        int sx = voxels.GetLength(0);
        int sy = voxels.GetLength(1);
        int sz = voxels.GetLength(2);

        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
        for (int z = 0; z < sz; z++)
        {
            if (!voxels[x, y, z])
            {
                continue;
            }

            AddFaceDots(voxels, points, x, y, z, Vector3Int.right);
            AddFaceDots(voxels, points, x, y, z, Vector3Int.left);
            AddFaceDots(voxels, points, x, y, z, Vector3Int.up);
            AddFaceDots(voxels, points, x, y, z, Vector3Int.down);
            AddFaceDots(voxels, points, x, y, z, Vector3Int.forward);
            AddFaceDots(voxels, points, x, y, z, Vector3Int.back);
        }

        List<Vector3> meshVertices = new List<Vector3>();
        List<int> meshTriangles = new List<int>();

        foreach (Vector3 point in points.Values)
        {
            AddOctahedron(meshVertices, meshTriangles, point, dotRadius);
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(meshVertices);
        mesh.SetTriangles(meshTriangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void AddFaceDots(
        bool[,,] voxels,
        Dictionary<Vector3Int, Vector3> points,
        int x,
        int y,
        int z,
        Vector3Int normal)
    {
        if (IsSolid(voxels, x + normal.x, y + normal.y, z + normal.z))
        {
            return;
        }

        Vector3[] corners = GetFaceCorners(x, y, z, normal);
        Vector3 offset = (Vector3)normal * dotOffset;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 corner = corners[i];
            Vector3Int key = new Vector3Int(
                Mathf.RoundToInt(corner.x * 1000f),
                Mathf.RoundToInt(corner.y * 1000f),
                Mathf.RoundToInt(corner.z * 1000f));

            if (!points.ContainsKey(key))
            {
                points.Add(key, corner + offset);
            }
        }
    }

    private static Vector3[] GetFaceCorners(int x, int y, int z, Vector3Int normal)
    {
        float x0 = x;
        float y0 = y;
        float z0 = z;
        float x1 = x + 1;
        float y1 = y + 1;
        float z1 = z + 1;

        if (normal == Vector3Int.right)
        {
            return new[] { new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1) };
        }

        if (normal == Vector3Int.left)
        {
            return new[] { new Vector3(x0, y0, z0), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0) };
        }

        if (normal == Vector3Int.up)
        {
            return new[] { new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0) };
        }

        if (normal == Vector3Int.down)
        {
            return new[] { new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), new Vector3(x0, y0, z1) };
        }

        if (normal == Vector3Int.forward)
        {
            return new[] { new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1) };
        }

        return new[] { new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y0, z0) };
    }

    private static void AddEdge(Dictionary<EdgeKey, EdgeInfo> edges, Vector3 start, Vector3 end, Vector3Int normal)
    {
        EdgeKey key = new EdgeKey(start, end);
        if (!edges.TryGetValue(key, out EdgeInfo info))
        {
            info = new EdgeInfo(key.Start, key.End);
        }

        info.AddNormal(normal);
        edges[key] = info;
    }

    private static void AddEdgePrism(
        List<Vector3> vertices,
        List<int> triangles,
        Vector3 start,
        Vector3 end,
        Vector3 offsetNormal,
        float width,
        float offset)
    {
        Vector3 direction = end - start;
        if (direction.sqrMagnitude < 0.000001f)
        {
            return;
        }

        direction.Normalize();
        Vector3 up = Vector3.ProjectOnPlane(offsetNormal, direction).normalized;
        if (up.sqrMagnitude < 0.0001f)
        {
            up = Vector3.Cross(direction, Vector3.up).normalized;
            if (up.sqrMagnitude < 0.0001f)
            {
                up = Vector3.Cross(direction, Vector3.right).normalized;
            }
        }

        Vector3 right = Vector3.Cross(direction, up).normalized;
        float half = width * 0.5f;
        Vector3 outward = offsetNormal.normalized * offset;

        int baseIndex = vertices.Count;
        vertices.Add(start + outward + up * half + right * half);
        vertices.Add(start + outward + up * half - right * half);
        vertices.Add(start + outward - up * half - right * half);
        vertices.Add(start + outward - up * half + right * half);
        vertices.Add(end + outward + up * half + right * half);
        vertices.Add(end + outward + up * half - right * half);
        vertices.Add(end + outward - up * half - right * half);
        vertices.Add(end + outward - up * half + right * half);

        AddQuad(triangles, baseIndex + 0, baseIndex + 4, baseIndex + 5, baseIndex + 1);
        AddQuad(triangles, baseIndex + 1, baseIndex + 5, baseIndex + 6, baseIndex + 2);
        AddQuad(triangles, baseIndex + 2, baseIndex + 6, baseIndex + 7, baseIndex + 3);
        AddQuad(triangles, baseIndex + 3, baseIndex + 7, baseIndex + 4, baseIndex + 0);
        AddQuad(triangles, baseIndex + 0, baseIndex + 1, baseIndex + 2, baseIndex + 3);
        AddQuad(triangles, baseIndex + 4, baseIndex + 7, baseIndex + 6, baseIndex + 5);
    }

    private static void AddOctahedron(List<Vector3> vertices, List<int> triangles, Vector3 center, float radius)
    {
        int baseIndex = vertices.Count;
        vertices.Add(center + Vector3.up * radius);
        vertices.Add(center + Vector3.right * radius);
        vertices.Add(center + Vector3.forward * radius);
        vertices.Add(center + Vector3.left * radius);
        vertices.Add(center + Vector3.back * radius);
        vertices.Add(center + Vector3.down * radius);

        AddTriangle(triangles, baseIndex + 0, baseIndex + 1, baseIndex + 2);
        AddTriangle(triangles, baseIndex + 0, baseIndex + 2, baseIndex + 3);
        AddTriangle(triangles, baseIndex + 0, baseIndex + 3, baseIndex + 4);
        AddTriangle(triangles, baseIndex + 0, baseIndex + 4, baseIndex + 1);
        AddTriangle(triangles, baseIndex + 5, baseIndex + 2, baseIndex + 1);
        AddTriangle(triangles, baseIndex + 5, baseIndex + 3, baseIndex + 2);
        AddTriangle(triangles, baseIndex + 5, baseIndex + 4, baseIndex + 3);
        AddTriangle(triangles, baseIndex + 5, baseIndex + 1, baseIndex + 4);
    }

    private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);
    }

    private static void AddTriangle(List<int> triangles, int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    private static bool IsSolid(bool[,,] voxels, int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 ||
            x >= voxels.GetLength(0) ||
            y >= voxels.GetLength(1) ||
            z >= voxels.GetLength(2))
        {
            return false;
        }

        return voxels[x, y, z];
    }

    private void EnsureMaterials()
    {
        if (surfaceMaterial == null)
        {
            surfaceMaterial = CreateSurfaceMaterial();
        }

        if (edgeMaterial == null)
        {
            edgeMaterial = CreateOpaqueMaterial("Voxel Shell Edge", edgeColor, 3100);
        }

        if (dotMaterial == null)
        {
            dotMaterial = CreateOpaqueMaterial("Voxel Shell Dot", dotColor, 3110);
        }
    }

    private void ApplyMaterialSettings()
    {
        if (surfaceMaterial != null && surfaceMaterial.HasProperty("_Color"))
        {
            surfaceMaterial.SetColor("_Color", surfaceColor);
        }

        if (edgeMaterial != null && edgeMaterial.HasProperty("_Color"))
        {
            edgeMaterial.SetColor("_Color", edgeColor);
        }

        if (dotMaterial != null && dotMaterial.HasProperty("_Color"))
        {
            dotMaterial.SetColor("_Color", dotColor);
        }
    }

    private Material CreateSurfaceMaterial()
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = "Voxel Shell Surface";
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        material.SetColor("_Color", surfaceColor);
        material.SetFloat("_Glossiness", 0.85f);
        return material;
    }

    private static Material CreateOpaqueMaterial(string materialName, Color color, int renderQueue)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = materialName;
        material.SetColor("_Color", color);
        material.renderQueue = renderQueue;
        return material;
    }

    private GameObject CreateChild(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        return child;
    }

    private void DestroyChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(child.gameObject);
        }
        else
        {
            DestroyImmediate(child.gameObject);
        }
    }

    private readonly struct EdgeKey
    {
        private readonly int ax;
        private readonly int ay;
        private readonly int az;
        private readonly int bx;
        private readonly int by;
        private readonly int bz;

        public Vector3 Start => new Vector3(ax / 1000f, ay / 1000f, az / 1000f);
        public Vector3 End => new Vector3(bx / 1000f, by / 1000f, bz / 1000f);

        public EdgeKey(Vector3 start, Vector3 end)
        {
            int sax = Mathf.RoundToInt(start.x * 1000f);
            int say = Mathf.RoundToInt(start.y * 1000f);
            int saz = Mathf.RoundToInt(start.z * 1000f);
            int sbx = Mathf.RoundToInt(end.x * 1000f);
            int sby = Mathf.RoundToInt(end.y * 1000f);
            int sbz = Mathf.RoundToInt(end.z * 1000f);

            bool swap = sax > sbx ||
                        (sax == sbx && say > sby) ||
                        (sax == sbx && say == sby && saz > sbz);

            if (swap)
            {
                ax = sbx;
                ay = sby;
                az = sbz;
                bx = sax;
                by = say;
                bz = saz;
            }
            else
            {
                ax = sax;
                ay = say;
                az = saz;
                bx = sbx;
                by = sby;
                bz = sbz;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EdgeKey other))
            {
                return false;
            }

            return ax == other.ax &&
                   ay == other.ay &&
                   az == other.az &&
                   bx == other.bx &&
                   by == other.by &&
                   bz == other.bz;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ax;
                hash = hash * 31 + ay;
                hash = hash * 31 + az;
                hash = hash * 31 + bx;
                hash = hash * 31 + by;
                hash = hash * 31 + bz;
                return hash;
            }
        }
    }

    private struct EdgeInfo
    {
        public readonly Vector3 Start;
        public readonly Vector3 End;
        private Vector3 normalA;
        private int normalMask;
        private int normalCount;

        public EdgeInfo(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
            normalA = Vector3.zero;
            normalMask = 0;
            normalCount = 0;
        }

        public Vector3 AverageNormal => normalA;
        public bool IsVisible => normalCount == 1 || HasMultipleNormals;

        public void AddNormal(Vector3Int normal)
        {
            int bit = NormalBit(normal);
            if ((normalMask & bit) == 0)
            {
                normalA += normal;
                normalMask |= bit;
            }

            normalCount++;
        }

        private bool HasMultipleNormals
        {
            get
            {
                return (normalMask & (normalMask - 1)) != 0;
            }
        }

        private static int NormalBit(Vector3Int normal)
        {
            if (normal == Vector3Int.right) return 1 << 0;
            if (normal == Vector3Int.left) return 1 << 1;
            if (normal == Vector3Int.up) return 1 << 2;
            if (normal == Vector3Int.down) return 1 << 3;
            if (normal == Vector3Int.forward) return 1 << 4;
            return 1 << 5;
        }
    }
}
