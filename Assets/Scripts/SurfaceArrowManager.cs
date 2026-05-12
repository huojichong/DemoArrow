using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SurfaceArrowBox
{
    public Vector3Int min;
    public Vector3Int max;
}

public class SurfaceArrowManager : MonoBehaviour
{
    public Transform coordinateRoot;
    public VoxelChunk voxelChunk;
    public List<SurfaceArrowBox> boxes = new List<SurfaceArrowBox>();

    private readonly List<SurfaceArrow> arrows = new List<SurfaceArrow>();

    public void Register(SurfaceArrow arrow)
    {
        if (arrow != null && !arrows.Contains(arrow))
        {
            arrows.Add(arrow);
        }
    }

    public void Unregister(SurfaceArrow arrow)
    {
        arrows.Remove(arrow);
    }

    public bool IsSolidPoint(Vector3 point)
    {
        Vector3 localPoint = WorldToLocalPoint(point);
        return IsSolidLocal(localPoint.x, localPoint.y, localPoint.z);
    }

    public bool IsSolidPoint(float x, float y, float z)
    {
        return IsSolidPoint(new Vector3(x, y, z));
    }

    public Vector3 LocalToWorldPoint(Vector3 localPoint)
    {
        Transform root = ResolveCoordinateRoot();
        return root != null ? root.TransformPoint(localPoint) : localPoint;
    }

    public Vector3 LocalToWorldDirection(Vector3 localDirection)
    {
        Transform root = ResolveCoordinateRoot();
        return root != null ? root.TransformDirection(localDirection).normalized : localDirection.normalized;
    }

    public Vector3 WorldToLocalPoint(Vector3 worldPoint)
    {
        Transform root = ResolveCoordinateRoot();
        return root != null ? root.InverseTransformPoint(worldPoint) : worldPoint;
    }

    public Vector3 WorldToLocalDirection(Vector3 worldDirection)
    {
        Transform root = ResolveCoordinateRoot();
        return root != null ? root.InverseTransformDirection(worldDirection).normalized : worldDirection.normalized;
    }

    private bool IsSolidLocal(float x, float y, float z)
    {
        if (voxelChunk != null && voxelChunk.voxels != null)
        {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            int iz = Mathf.FloorToInt(z);

            if (ix < 0 || iy < 0 || iz < 0 ||
                ix >= voxelChunk.voxels.GetLength(0) ||
                iy >= voxelChunk.voxels.GetLength(1) ||
                iz >= voxelChunk.voxels.GetLength(2))
            {
                return false;
            }

            return voxelChunk.voxels[ix, iy, iz];
        }

        for (int i = 0; i < boxes.Count; i++)
        {
            SurfaceArrowBox box = boxes[i];
            int minX = Mathf.Min(box.min.x, box.max.x);
            int minY = Mathf.Min(box.min.y, box.max.y);
            int minZ = Mathf.Min(box.min.z, box.max.z);
            int maxX = Mathf.Max(box.min.x, box.max.x);
            int maxY = Mathf.Max(box.min.y, box.max.y);
            int maxZ = Mathf.Max(box.min.z, box.max.z);

            if (x >= minX && x < maxX &&
                y >= minY && y < maxY &&
                z >= minZ && z < maxZ)
            {
                return true;
            }
        }

        return false;
    }

    public Vector3 GetSurfaceNormal(Vector3 point, Vector3 headDir)
    {
        Vector3 localPoint = WorldToLocalPoint(point);
        Vector3 localHeadDir = WorldToLocalDirection(headDir);
        Vector3[] candidates =
        {
            Vector3.right,
            Vector3.left,
            Vector3.up,
            Vector3.down,
            Vector3.forward,
            Vector3.back
        };

        Vector3 bestNormal = Vector3.up;
        float minDot = float.PositiveInfinity;

        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 normal = candidates[i];
            if (!IsNearInteger(Vector3.Dot(localPoint, normal)))
            {
                continue;
            }

            Vector3 inside = localPoint - normal * 0.1f;
            Vector3 outside = localPoint + normal * 0.1f;

            if (IsSolidLocal(inside.x, inside.y, inside.z) && !IsSolidLocal(outside.x, outside.y, outside.z))
            {
                float dot = Mathf.Abs(Vector3.Dot(normal, localHeadDir));
                if (dot < minDot)
                {
                    minDot = dot;
                    bestNormal = normal;
                }
            }
        }

        return LocalToWorldDirection(bestNormal);
    }

    public bool HitsOtherArrow(SurfaceArrow self, Vector3 point, float margin)
    {
        for (int i = 0; i < arrows.Count; i++)
        {
            SurfaceArrow other = arrows[i];
            if (other == null || other == self || other.Status == SurfaceArrowStatus.Moving)
            {
                continue;
            }

            IReadOnlyList<Vector3> path = other.GetWorldPath();
            for (int j = 0; j < path.Count - 1; j++)
            {
                Vector3 a = path[j];
                Vector3 b = path[j + 1];
                Bounds bounds = new Bounds((a + b) * 0.5f, new Vector3(
                    Mathf.Abs(a.x - b.x) + margin * 2f,
                    Mathf.Abs(a.y - b.y) + margin * 2f,
                    Mathf.Abs(a.z - b.z) + margin * 2f));

                if (bounds.Contains(point))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsNearInteger(float value)
    {
        return Mathf.Abs(value - Mathf.Round(value)) < 0.05f;
    }

    private Transform ResolveCoordinateRoot()
    {
        if (coordinateRoot != null)
        {
            return coordinateRoot;
        }

        if (voxelChunk != null)
        {
            return voxelChunk.transform;
        }

        return transform;
    }
}
