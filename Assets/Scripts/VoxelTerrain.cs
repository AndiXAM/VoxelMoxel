using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class VoxelTerrain : MonoBehaviour
{
    public int width = 50;
    public int height = 20;
    public int depth = 50;
    public float noiseScale = 0.1f;
    public float heightMultiplier = 10f;
    public Material voxelMaterial; 
    

    private void Start()
    {
        GenerateTerrain();
    }

    void GenerateTerrain() // Генерация карты высот
    {
        bool[,,] voxelMap = new bool[width, height, depth];

        
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float noise = Mathf.PerlinNoise(x * noiseScale, z * noiseScale);
                int terrainHeight = Mathf.FloorToInt(noise * heightMultiplier);
                
                for (int y = 0; y < terrainHeight && y < height; y++)
                {
                    voxelMap[x, y, z] = true;
                }
            }
        }

        // Создание меша
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (voxelMap[x, y, z])
                    {
                        bool[] faces = new bool[6]
                        {
                            y < height - 1 && !voxelMap[x, y + 1, z], // Top
                            y > 0 && !voxelMap[x, y - 1, z],           // Bottom
                            z < depth - 1 && !voxelMap[x, y, z + 1],   // Front
                            z > 0 && !voxelMap[x, y, z - 1],           // Back
                            x < width - 1 && !voxelMap[x + 1, y, z],   // Right
                            x > 0 && !voxelMap[x - 1, y, z]            // Left
                        };

                        AddFaces(x, y, z, faces, vertices, triangles, uvs);
                    }
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        
        // Назначаем материал
        if(voxelMaterial != null)
        {
            GetComponent<MeshRenderer>().material = voxelMaterial;
        }
        else
        {
            Debug.LogWarning("Не назначен материал! Создайте материал в папке Materials и перетащите его в инспектор");
        }
    }

    void AddFaces(int x, int y, int z, bool[] faces, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        for (int i = 0; i < 6; i++)
        {
            if (faces[i])
            {
                int vCount = vertices.Count;
                Vector3[] faceVertices = GetFaceVertices(x, y, z, i);
                
                // Добавляем вершины
                vertices.AddRange(faceVertices);

                // Добавляем треугольники (правильный порядок по часовой стрелке)
                triangles.Add(vCount);
                triangles.Add(vCount + 1);
                triangles.Add(vCount + 2);
                triangles.Add(vCount + 2);
                triangles.Add(vCount + 3);
                triangles.Add(vCount);

                // Добавляем UV с учётом ориентации грани
                AddFaceUVs(i, uvs);
            }
        }
    }

    void AddFaceUVs(int faceIndex, List<Vector2> uvs)
{
    switch (faceIndex)
    {
        case 0: // Top (исправленные UV)
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
            break;
            case 1: // Bottom
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(0, 0));
                break;
            case 2: // Front
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
                break;
            case 3: // Back
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                break;
            case 4: // Right
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(0, 0));
                break;
            case 5: // Left
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));
                break;
        }
    }

    Vector3[] GetFaceVertices(int x, int y, int z, int faceIndex)
{
    Vector3[] vertices = new Vector3[4];
    
    switch (faceIndex)
    {
        case 0: // Top 
            vertices[0] = new Vector3(x, y + 1, z);
            vertices[1] = new Vector3(x, y + 1, z + 1); 
            vertices[2] = new Vector3(x + 1, y + 1, z + 1);
            vertices[3] = new Vector3(x + 1, y + 1, z);
            break;
            case 1: // Bottom
                vertices[0] = new Vector3(x, y, z);
                vertices[1] = new Vector3(x + 1, y, z);
                vertices[2] = new Vector3(x + 1, y, z + 1);
                vertices[3] = new Vector3(x, y, z + 1);
                break;
            case 2: // Front
                vertices[0] = new Vector3(x, y, z + 1);
                vertices[1] = new Vector3(x + 1, y, z + 1);
                vertices[2] = new Vector3(x + 1, y + 1, z + 1);
                vertices[3] = new Vector3(x, y + 1, z + 1);
                break;
            case 3: // Back
                vertices[0] = new Vector3(x + 1, y, z);
                vertices[1] = new Vector3(x, y, z);
                vertices[2] = new Vector3(x, y + 1, z);
                vertices[3] = new Vector3(x + 1, y + 1, z);
                break;
            case 4: // Right
                vertices[0] = new Vector3(x + 1, y, z);
                vertices[1] = new Vector3(x + 1, y, z + 1);
                vertices[2] = new Vector3(x + 1, y + 1, z + 1);
                vertices[3] = new Vector3(x + 1, y + 1, z);
                break;
            case 5: // Left
                vertices[0] = new Vector3(x, y, z + 1);
                vertices[1] = new Vector3(x, y, z);
                vertices[2] = new Vector3(x, y + 1, z);
                vertices[3] = new Vector3(x, y + 1, z + 1);
                break;
        }
        return vertices;
    }
}