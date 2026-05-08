using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelChunk : MonoBehaviour
{
    public int sizeX = 32;
    public int sizeY = 32;
    public int sizeZ = 32;

    public bool[,,] voxels;

    private MeshFilter meshFilter;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();

        GenerateTestData();

        Mesh mesh = GreedyMesher.Build(voxels);

        meshFilter.sharedMesh = mesh;
    }
    
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 480, 800));
        
        DrawSaveButtons();
        GUILayout.Space(10);
        
        GUILayout.EndArea();
    }


    void DrawSaveButtons()
    {

        if (GUILayout.Button("导出为 OBJ - Blender (O)", GUILayout.Height(35)))
        {
            ExportAsOBJ();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("导出为 FBX (F)", GUILayout.Height(35)))
        {
            ExportAsFBX();
        }
    }
    
    /// <summary>
    /// 导出为 FBX 格式
    /// </summary>
    public void ExportAsFBX()
    {
        
        MeshSaver.ExportAsFBX(gameObject);
    }

    /// <summary>
    /// 导出为 OBJ 格式（Blender 兼容）
    /// </summary>
    public void ExportAsOBJ()
    {
        MeshSaver.ExportAsOBJ(gameObject);
    }


    void GenerateTestData()
    {
        voxels = new bool[sizeX, sizeY, sizeZ];

        int stairWidth = 4;
        int stairDepth = 8;
        int stairHeight = 1;

        int steps = 8;

        for (int step = 0; step < steps; step++)
        {
            int startY = step * stairHeight;

            for (int x = 0; x < stairWidth; x++)
            for (int y = 0; y <= startY; y++)
            for (int z = step * stairDepth; z < (step + 1) * stairDepth; z++)
            {
                if (x < sizeX &&
                    y < sizeY &&
                    z < sizeZ)
                {
                    voxels[x, y, z] = true;
                }
            }
        }
    }
}