using System.Collections.Generic;
using UnityEngine;

public static class GreedyMesher
{
    struct MaskCell
    {
        public bool exists;
        public int normal;
    }

    static readonly Vector3[] normals =
    {
        Vector3.right,
        Vector3.left,
        Vector3.up,
        Vector3.down,
        Vector3.forward,
        Vector3.back
    };

    public static Mesh Build(bool[,,] voxels)
    {
        vertexMap.Clear();
        int sizeX = voxels.GetLength(0);
        int sizeY = voxels.GetLength(1);
        int sizeZ = voxels.GetLength(2);

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector3> meshNormals = new();

        int vertexCount = 0;

        // 遍历三个轴
        for (int d = 0; d < 3; d++)
        {
            int u = (d + 1) % 3;
            int v = (d + 2) % 3;

            int[] x = new int[3];
            int[] q = new int[3];
            q[d] = 1;

            int sizeD = GetAxisSize(d, sizeX, sizeY, sizeZ);
            int sizeU = GetAxisSize(u, sizeX, sizeY, sizeZ);
            int sizeV = GetAxisSize(v, sizeX, sizeY, sizeZ);

            MaskCell[,] mask = new MaskCell[sizeU, sizeV];

            for (x[d] = -1; x[d] < sizeD;)
            {
                // 构建 mask
                int n = 0;

                for (x[v] = 0; x[v] < sizeV; x[v]++)
                {
                    for (x[u] = 0; x[u] < sizeU; x[u]++)
                    {
                        bool a = IsSolid(voxels,
                            x[0], x[1], x[2]);

                        bool b = IsSolid(voxels,
                            x[0] + q[0],
                            x[1] + q[1],
                            x[2] + q[2]);

                        if (a != b)
                        {
                            mask[x[u], x[v]] = new MaskCell
                            {
                                exists = true,
                                normal = a ? 1 : -1
                            };
                        }
                        else
                        {
                            mask[x[u], x[v]] = new MaskCell
                            {
                                exists = false
                            };
                        }
                    }
                }

                x[d]++;

                // greedy rectangle merge
                for (int j = 0; j < sizeV; j++)
                {
                    for (int i = 0; i < sizeU;)
                    {
                        var cell = mask[i, j];

                        if (!cell.exists)
                        {
                            i++;
                            continue;
                        }

                        // find width
                        int width = 1;

                        while (i + width < sizeU)
                        {
                            var next = mask[i + width, j];

                            if (!next.exists ||
                                next.normal != cell.normal)
                                break;

                            width++;
                        }

                        // find height
                        int height = 1;
                        bool stop = false;

                        while (j + height < sizeV && !stop)
                        {
                            for (int k = 0; k < width; k++)
                            {
                                var next = mask[i + k, j + height];

                                if (!next.exists ||
                                    next.normal != cell.normal)
                                {
                                    stop = true;
                                    break;
                                }
                            }

                            if (!stop)
                                height++;
                        }

                        // create quad
                        x[u] = i;
                        x[v] = j;

                        int[] du = new int[3];
                        int[] dv = new int[3];

                        du[u] = width;
                        dv[v] = height;

                        Vector3 p0 = new Vector3(x[0], x[1], x[2]);
                        Vector3 p1 = new Vector3(
                            x[0] + du[0],
                            x[1] + du[1],
                            x[2] + du[2]);

                        Vector3 p2 = new Vector3(
                            x[0] + du[0] + dv[0],
                            x[1] + du[1] + dv[1],
                            x[2] + du[2] + dv[2]);

                        Vector3 p3 = new Vector3(
                            x[0] + dv[0],
                            x[1] + dv[1],
                            x[2] + dv[2]);

                        if (cell.normal == 1)
                        {
                            AddQuad(
                                vertices,
                                triangles,
                                meshNormals,
                                p0, p1, p2, p3,
                                normals[d]);
                        }
                        else
                        {
                            AddQuad(
                                vertices,
                                triangles,
                                meshNormals,
                                p3, p2, p1, p0,
                                -normals[d]);
                        }

                        // clear merged area
                        for (int dy = 0; dy < height; dy++)
                        {
                            for (int dx = 0; dx < width; dx++)
                            {
                                mask[i + dx, j + dy].exists = false;
                            }
                        }

                        i += width;
                    }
                }
            }
        }

        Mesh mesh = new Mesh();

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(meshNormals);

        mesh.RecalculateBounds();

        return mesh;
    }
    static Dictionary<Vector3Int, int> vertexMap =
        new Dictionary<Vector3Int, int>();

    static int GetVertex(
        Vector3 v,
        List<Vector3> vertices,
        List<Vector3> normals,
        Vector3 normal)
    {
        Vector3Int key = new Vector3Int(
            Mathf.RoundToInt(v.x * 1000),
            Mathf.RoundToInt(v.y * 1000),
            Mathf.RoundToInt(v.z * 1000));

        if (vertexMap.TryGetValue(key, out int index))
        {
            return index;
        }

        index = vertices.Count;

        vertices.Add(v);
        normals.Add(normal);

        vertexMap[key] = index;

        return index;
    }

    static void AddQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        Vector3 normal)
    {
        int i0 = GetVertex(p0, vertices, normals, normal);
        int i1 = GetVertex(p1, vertices, normals, normal);
        int i2 = GetVertex(p2, vertices, normals, normal);
        int i3 = GetVertex(p3, vertices, normals, normal);

        triangles.Add(i0);
        triangles.Add(i1);
        triangles.Add(i2);

        triangles.Add(i0);
        triangles.Add(i2);
        triangles.Add(i3);
    }

    static bool IsSolid(bool[,,] voxels, int x, int y, int z)
    {
        int sx = voxels.GetLength(0);
        int sy = voxels.GetLength(1);
        int sz = voxels.GetLength(2);

        if (x < 0 || y < 0 || z < 0 ||
            x >= sx || y >= sy || z >= sz)
            return false;

        return voxels[x, y, z];
    }

    static int GetAxisSize(int axis, int x, int y, int z)
    {
        return axis switch
        {
            0 => x,
            1 => y,
            _ => z
        };
    }
}