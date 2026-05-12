using System.Collections.Generic;
using UnityEngine;

public enum SurfaceArrowStatus
{
    Idle,
    Moving,
    Hitting,
    Shaking,
    Retreating
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class SurfaceArrow : MonoBehaviour
{
    public SurfaceArrowManager manager;
    public List<Vector3> path = new List<Vector3>
    {
        new Vector3(2f, 1f, 0f),
        new Vector3(1f, 1f, 0f),
        new Vector3(0f, 1f, 0f)
    };

    [Header("Visual")]
    public Material material;
    public Color idleColor = Color.black;
    public Color hitColor = new Color(1f, 0.25f, 0.25f, 1f);
    [Min(0.01f)]
    public float segmentWidth = 0.15f;
    [Min(0.01f)]
    public float headRadius = 0.25f;
    [Min(0f)]
    public float surfaceOffset = 0.012f;

    [Header("Movement")]
    public bool blockClicksThroughModel = true;
    public SurfaceArrowStatus status = SurfaceArrowStatus.Idle;
    public float speed = 15f;
    public float shakeSpeed = 40f;
    public float maxTravelDistance = 20f;
    public float hitDistance;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh mesh;
    private float progress;
    private float shakeProgress;
    private List<Vector3> visualPathBase = new List<Vector3>();
    private List<Vector3> animatedPath = new List<Vector3>();

    public SurfaceArrowStatus Status => status;
    public IReadOnlyList<Vector3> Path => path;
    public IReadOnlyList<Vector3> GetWorldPath()
    {
        if (visualPathBase == null || visualPathBase.Count < 2)
        {
            RebuildBasePath();
        }

        return visualPathBase;
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        ResolveManager();
        manager?.Register(this);
        RebuildBasePath();
        RebuildMesh();
    }

    private void OnDisable()
    {
        manager?.Unregister(this);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            Initialize();
            RebuildBasePath();
            RebuildMesh();
        }
    }

    private void Update()
    {
        if (path == null || path.Count < 2)
        {
            return;
        }

        if (status == SurfaceArrowStatus.Moving)
        {
            progress += Time.deltaTime * speed;
            if (progress > maxTravelDistance)
            {
                Destroy(gameObject);
                return;
            }

            RebuildMesh();
        }
        else if (status == SurfaceArrowStatus.Hitting)
        {
            progress += Time.deltaTime * speed;
            if (progress > hitDistance)
            {
                progress = hitDistance;
                status = SurfaceArrowStatus.Shaking;
            }

            RebuildMesh();
        }
        else if (status == SurfaceArrowStatus.Shaking)
        {
            shakeProgress += Time.deltaTime * shakeSpeed;
            if (shakeProgress > Mathf.PI * 4f)
            {
                shakeProgress = 0f;
                status = SurfaceArrowStatus.Retreating;
            }

            RebuildMesh();
        }
        else if (status == SurfaceArrowStatus.Retreating)
        {
            progress -= Time.deltaTime * speed;
            if (progress <= 0f)
            {
                progress = 0f;
                status = SurfaceArrowStatus.Idle;
            }

            RebuildMesh();
        }
    }

    private void OnMouseDown()
    {
        if (blockClicksThroughModel && IsClickBlockedByModel())
        {
            return;
        }

        TryActivate();
    }

    [ContextMenu("Rebuild Arrow Mesh")]
    public void RebuildMesh()
    {
        Initialize();
        ResolveManager();
        RebuildBasePath();

        if (visualPathBase.Count < 2)
        {
            mesh.Clear();
            meshCollider.sharedMesh = null;
            return;
        }

        animatedPath = status == SurfaceArrowStatus.Idle
            ? new List<Vector3>(visualPathBase)
            : GetAnimatedPath(visualPathBase, progress);

        Vector3 headDir = GetHeadDirection(animatedPath);
        float shakeOffset = status == SurfaceArrowStatus.Shaking ? -Mathf.Sin(shakeProgress) * 0.1f : 0f;
        Vector3 groupOffset = headDir * shakeOffset;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < animatedPath.Count - 1; i++)
        {
            Vector3 a = animatedPath[i] + groupOffset;
            Vector3 b = animatedPath[i + 1] + groupOffset;

            if (!TryGetBasis(a, b, out ArrowBasis basis))
            {
                continue;
            }

            AddSegmentQuad(vertices, triangles, basis.center, basis.xAxis, basis.forward, basis.normal, segmentWidth, basis.length + 0.01f);

            if (i > 0 && TryGetBasis(animatedPath[i - 1] + groupOffset, a, out ArrowBasis previous))
            {
                if (Mathf.Abs(Vector3.Dot(previous.normal, basis.normal)) > 0.9f)
                {
                    AddSegmentQuad(vertices, triangles, a, basis.xAxis, basis.forward, basis.normal, segmentWidth, segmentWidth);
                }
            }
        }

        Vector3 head = animatedPath[0] + groupOffset;
        if (TryGetBasis(animatedPath[0] + groupOffset, animatedPath[1] + groupOffset, out ArrowBasis headBasis))
        {
            AddHeadTriangle(vertices, triangles, head, headBasis.xAxis, GetHeadDirection(animatedPath), headBasis.normal, headRadius);
        }

        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;

        if (meshRenderer.sharedMaterial != null && meshRenderer.sharedMaterial.HasProperty("_Color"))
        {
            meshRenderer.sharedMaterial.color = status == SurfaceArrowStatus.Shaking ? hitColor : idleColor;
        }
    }

    public void TryActivate()
    {
        if (status != SurfaceArrowStatus.Idle || path == null || path.Count < 2)
        {
            return;
        }

        ResolveManager();
        if (manager == null)
        {
            Debug.LogWarning("SurfaceArrow: no SurfaceArrowManager found.");
            return;
        }

        Vector3 localDirection = ResolveMoveDirection(path[0], path[1]);
        Vector3 direction = manager.LocalToWorldDirection(localDirection);
        if (direction.sqrMagnitude < 0.5f)
        {
            return;
        }

        Vector3 origin = manager.LocalToWorldPoint(path[0]);
        Vector3 normal = manager.GetSurfaceNormal(origin, direction);
        bool isFlying = false;
        bool hit = false;
        float resolvedHitDistance = 0f;

        for (int k = 1; k <= Mathf.CeilToInt(maxTravelDistance); k++)
        {
            Vector3 current = origin + direction * k;
            Vector3 previous = origin + direction * (k - 1);

            if (manager.HitsOtherArrow(this, current, 0.05f))
            {
                hit = true;
                resolvedHitDistance = Mathf.Max(0f, k - 0.4f);
                break;
            }

            if (!isFlying)
            {
                Vector3 solidProbe = previous + direction * 0.6f + normal * 0.1f;
                if (manager.IsSolidPoint(solidProbe))
                {
                    hit = true;
                    resolvedHitDistance = Mathf.Max(0f, k - 0.75f);
                    break;
                }

                Vector3 supportProbe = current - normal * 0.1f;
                if (!manager.IsSolidPoint(supportProbe))
                {
                    isFlying = true;
                }
            }
        }

        progress = 0f;
        shakeProgress = 0f;
        if (hit)
        {
            hitDistance = resolvedHitDistance;
            status = SurfaceArrowStatus.Hitting;
        }
        else
        {
            status = SurfaceArrowStatus.Moving;
        }

        RebuildMesh();
    }

    private void Initialize()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (meshCollider == null)
        {
            meshCollider = GetComponent<MeshCollider>();
        }

        if (mesh == null)
        {
            mesh = new Mesh { name = "SurfaceArrowMesh" };
        }

        if (meshFilter != null)
        {
            meshFilter.sharedMesh = mesh;
        }

        if (meshRenderer != null && meshRenderer.sharedMaterial == null)
        {
            meshRenderer.sharedMaterial = material != null ? material : CreateDefaultMaterial();
        }
    }

    private void ResolveManager()
    {
        if (manager == null)
        {
            manager = FindObjectOfType<SurfaceArrowManager>();
        }
    }

    private bool IsClickBlockedByModel()
    {
        ResolveManager();
        if (manager == null || meshCollider == null)
        {
            return false;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (!meshCollider.Raycast(ray, out RaycastHit hit, 1000f))
        {
            return false;
        }

        const float step = 0.05f;
        const float endPadding = 0.03f;
        float maxDistance = Mathf.Max(0f, hit.distance - endPadding);

        for (float distance = step; distance < maxDistance; distance += step)
        {
            if (manager.IsSolidPoint(ray.GetPoint(distance)))
            {
                return true;
            }
        }

        return false;
    }

    private Material CreateDefaultMaterial()
    {
        Material created = new Material(Shader.Find("Standard"));
        created.name = "Surface Arrow Material";
        created.SetColor("_Color", idleColor);
        created.renderQueue = 3120;
        return created;
    }

    private void RebuildBasePath()
    {
        List<Vector3> localVisualPath = BuildVisualPath(path);
        visualPathBase = ToWorldPath(localVisualPath);
    }

    private List<Vector3> ToWorldPath(IReadOnlyList<Vector3> localPath)
    {
        List<Vector3> worldPath = new List<Vector3>();
        if (localPath == null)
        {
            return worldPath;
        }

        ResolveManager();
        for (int i = 0; i < localPath.Count; i++)
        {
            worldPath.Add(manager != null ? manager.LocalToWorldPoint(localPath[i]) : transform.TransformPoint(localPath[i]));
        }

        return worldPath;
    }

    private static List<Vector3> BuildVisualPath(IReadOnlyList<Vector3> points)
    {
        List<Vector3> visualPath = new List<Vector3>();
        if (points == null || points.Count == 0)
        {
            return visualPath;
        }

        visualPath.Add(points[0]);
        for (int i = 1; i < points.Count; i++)
        {
            Vector3 a = points[i - 1];
            Vector3 b = points[i];
            float distSq = (a - b).sqrMagnitude;
            if (distSq < 0.6f && distSq > 0.1f)
            {
                visualPath.Add(GetEdgePoint(a, b));
            }

            visualPath.Add(b);
        }

        return visualPath;
    }

    private static Vector3 GetEdgePoint(Vector3 a, Vector3 b)
    {
        return new Vector3(
            Mathf.Approximately(a.x, b.x) ? a.x : Mathf.Round((a.x + b.x) * 0.5f),
            Mathf.Approximately(a.y, b.y) ? a.y : Mathf.Round((a.y + b.y) * 0.5f),
            Mathf.Approximately(a.z, b.z) ? a.z : Mathf.Round((a.z + b.z) * 0.5f));
    }

    private static List<Vector3> GetAnimatedPath(IReadOnlyList<Vector3> visualPath, float travel)
    {
        if (travel <= 0f || visualPath.Count < 2)
        {
            return new List<Vector3>(visualPath);
        }

        Vector3 head = visualPath[0];
        Vector3 neck = visualPath[1];
        Vector3 direction = (head - neck).normalized;
        Vector3 newHead = head + direction * travel;

        float totalLength = 0f;
        List<float> segmentLengths = new List<float>();
        for (int i = 0; i < visualPath.Count - 1; i++)
        {
            float d = Vector3.Distance(visualPath[i], visualPath[i + 1]);
            totalLength += d;
            segmentLengths.Add(d);
        }

        float targetBodyLength = totalLength - travel;
        List<Vector3> newPath = new List<Vector3> { newHead };

        if (targetBodyLength <= 0f)
        {
            newPath.Add(newHead - direction * totalLength);
            return newPath;
        }

        float travelLeft = travel;
        List<Vector3> keptPoints = new List<Vector3>();
        for (int i = visualPath.Count - 1; i > 0; i--)
        {
            float d = segmentLengths[i - 1];
            if (travelLeft >= d)
            {
                travelLeft -= d;
            }
            else
            {
                Vector3 dst = visualPath[i];
                Vector3 src = visualPath[i - 1];
                float ratio = (d - travelLeft) / d;
                keptPoints.Add(Vector3.Lerp(src, dst, ratio));

                for (int j = i - 1; j >= 1; j--)
                {
                    keptPoints.Add(visualPath[j]);
                }

                break;
            }
        }

        keptPoints.Reverse();
        newPath.AddRange(keptPoints);
        return newPath;
    }

    private Vector3 ResolveMoveDirection(Vector3 head, Vector3 neck)
    {
        Vector3 raw = head - neck;
        bool isOnX = IsNearInteger(head.x);
        bool isOnY = IsNearInteger(head.y);
        bool isOnZ = IsNearInteger(head.z);

        AxisChoice[] choices =
        {
            new AxisChoice(Vector3.right, raw.x, isOnX),
            new AxisChoice(Vector3.up, raw.y, isOnY),
            new AxisChoice(Vector3.forward, raw.z, isOnZ)
        };

        AxisChoice? fallback = null;
        for (int i = 0; i < choices.Length; i++)
        {
            AxisChoice choice = choices[i];
            if (Mathf.Abs(choice.value) <= 0.1f)
            {
                continue;
            }

            if (!fallback.HasValue)
            {
                fallback = choice;
            }

            if (!choice.isFacePlane)
            {
                return choice.axis * Mathf.Sign(choice.value);
            }
        }

        return fallback.HasValue ? fallback.Value.axis * Mathf.Sign(fallback.Value.value) : Vector3.zero;
    }

    private bool TryGetBasis(Vector3 a, Vector3 b, out ArrowBasis basis)
    {
        Vector3 delta = b - a;
        float length = delta.magnitude;
        if (length < 0.001f)
        {
            basis = default;
            return false;
        }

        Vector3 forward = delta / length;
        Vector3 center = (a + b) * 0.5f;
        Vector3 normal = manager != null ? manager.GetSurfaceNormal(center, forward) : Vector3.up;

        if (Mathf.Abs(Vector3.Dot(forward, normal)) > 0.9f)
        {
            normal = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up;
        }

        Vector3 xAxis = Vector3.Cross(forward, normal).normalized;
        normal = Vector3.Cross(xAxis, forward).normalized;
        basis = new ArrowBasis(length, center, xAxis, forward, normal);
        return true;
    }

    private static Vector3 GetHeadDirection(IReadOnlyList<Vector3> points)
    {
        if (points.Count < 2)
        {
            return Vector3.up;
        }

        Vector3 direction = points[0] - points[1];
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
    }

    private void AddSegmentQuad(
        List<Vector3> vertices,
        List<int> triangles,
        Vector3 center,
        Vector3 xAxis,
        Vector3 yAxis,
        Vector3 zAxis,
        float width,
        float length)
    {
        Vector3 outwardCenter = center + zAxis * surfaceOffset;
        Vector3 x = xAxis * (width * 0.5f);
        Vector3 y = yAxis * (length * 0.5f);

        int baseIndex = vertices.Count;
        vertices.Add(transform.InverseTransformPoint(outwardCenter - x - y));
        vertices.Add(transform.InverseTransformPoint(outwardCenter + x - y));
        vertices.Add(transform.InverseTransformPoint(outwardCenter + x + y));
        vertices.Add(transform.InverseTransformPoint(outwardCenter - x + y));

        AddQuad(triangles, baseIndex + 0, baseIndex + 1, baseIndex + 2, baseIndex + 3);
    }

    private void AddHeadTriangle(
        List<Vector3> vertices,
        List<int> triangles,
        Vector3 center,
        Vector3 xAxis,
        Vector3 forward,
        Vector3 normal,
        float radius)
    {
        Vector3 outwardCenter = center + normal * surfaceOffset;
        Vector3 tip = outwardCenter + forward * radius;
        Vector3 left = outwardCenter - forward * radius * 0.55f - xAxis * radius * 0.86f;
        Vector3 right = outwardCenter - forward * radius * 0.55f + xAxis * radius * 0.86f;

        int baseIndex = vertices.Count;
        vertices.Add(transform.InverseTransformPoint(tip));
        vertices.Add(transform.InverseTransformPoint(left));
        vertices.Add(transform.InverseTransformPoint(right));
        triangles.Add(baseIndex);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 1);
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

    private static bool IsNearInteger(float value)
    {
        return Mathf.Abs(value - Mathf.Round(value)) < 0.05f;
    }

    private readonly struct AxisChoice
    {
        public readonly Vector3 axis;
        public readonly float value;
        public readonly bool isFacePlane;

        public AxisChoice(Vector3 axis, float value, bool isFacePlane)
        {
            this.axis = axis;
            this.value = value;
            this.isFacePlane = isFacePlane;
        }
    }

    private readonly struct ArrowBasis
    {
        public readonly float length;
        public readonly Vector3 center;
        public readonly Vector3 xAxis;
        public readonly Vector3 forward;
        public readonly Vector3 normal;

        public ArrowBasis(float length, Vector3 center, Vector3 xAxis, Vector3 forward, Vector3 normal)
        {
            this.length = length;
            this.center = center;
            this.xAxis = xAxis;
            this.forward = forward;
            this.normal = normal;
        }
    }
}
