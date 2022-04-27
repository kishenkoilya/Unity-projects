using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class TowerCreator : EditorWindow
{
    private Utility.SphereSpawner sphereSpawner = new Utility.SphereSpawner(); 
    private Utility.TextureScanner textureScanner = new Utility.TextureScanner();
    private GameObject towerGeneratorGUI;
    private int vertexNumber = 8;
    private float towerRadius = 100;
    private float towerHeight = 500;
    private int floorNumber = 1;
    private float floorWidth = 20;
    private Material material;
    private Material materialInterior;
    private Texture2D levelConfiguration;
    private GameObject tower;
    private List<GameObject> figures;
    private Vector3[] towerVerticesExternal;
    private Vector3[] towerVerticesInternal;

    /// <summary>
    /// OnGUI is called for rendering and handling GUI events.
    /// This function can be called multiple times per frame (one call per event).
    /// </summary>
    void OnGUI() {
        GUILayout.Label("Tower generator", EditorStyles.boldLabel);
        vertexNumber = EditorGUILayout.IntField("Vertex Number", vertexNumber);
        towerRadius = EditorGUILayout.FloatField("Tower Radius", towerRadius);
        towerHeight = EditorGUILayout.FloatField("Tower Height", towerHeight);
        floorNumber = EditorGUILayout.IntField("Floor Number", floorNumber);
        floorWidth = EditorGUILayout.FloatField("Floor Width", floorWidth);
        
        material = (Material)EditorGUILayout.ObjectField("Tower material", material, typeof(Material), false);
        materialInterior = (Material)EditorGUILayout.ObjectField("Platform material", materialInterior, typeof(Material), false);
        levelConfiguration = (Texture2D)EditorGUILayout.ObjectField("Level configuration", levelConfiguration, typeof(Texture2D), false);
        if(GUILayout.Button("Generate tower")) {
            CreateTower();
        }
        if(GUILayout.Button("Clear All")) {
            ClearAll();
        }
        if(GUILayout.Button("Save prefab")) {
            SavePrefab();
        }
        if(GUILayout.Button("Test")) {
            Test();
        }
    }

    [MenuItem("GameObject/3D Object/Tower/TowerGeneratorWindow")]
    private static void TowerGeneratorWindow() {
        GetWindow(typeof(TowerCreator));
    }
    Vector3[] CreatePolygonVerteces(int vertexCount, float radius) {
        float centralAngle = 2 * Mathf.PI / vertexCount;
        float currentAngle = 0;
        Vector3[] verteces = new Vector3[vertexCount * 2];
        for (int i = 0; i < vertexCount + 1; i++) {
            float x, y, z;
            x = Mathf.Sin(currentAngle) * radius;
            y = 0;
            z = Mathf.Cos(currentAngle) * radius;
            if (i > 0) {
                verteces[i * 2 - 1] = new Vector3(x, y, z);
            }
            if (i < vertexCount) {
                verteces[i * 2] = new Vector3(x, y, z);
            }

            currentAngle += centralAngle; 
        }
        return verteces;
    }

    Vector3[] CreateTowerVerteces() {
        Vector3[] towerBasementVerteces = CreatePolygonVerteces(vertexNumber, towerRadius);
        Vector3[] towerAllVerteces = new Vector3[towerBasementVerteces.Length * (floorNumber + 1)];
        for (int i = 0; i <= floorNumber; i++) {
            towerBasementVerteces.CopyTo(towerAllVerteces, towerBasementVerteces.Length * i);
            if (i > 0) {
                for (int j = 0; j < towerBasementVerteces.Length; j++) {
                    towerAllVerteces[towerBasementVerteces.Length * i + j] += Vector3.up * (towerHeight / floorNumber) * i;
                }
            }
        }
        return towerAllVerteces; 
    }
    
    Mesh CreateTowerMesh(Vector3[] towerVerteces) {
        Mesh mesh = new Mesh();
        mesh.vertices = towerVerteces;
        int[] triangles = new int[vertexNumber * floorNumber * 6];
        Vector2[] UVs = new Vector2[towerVerteces.Length];
        float uvStepH = 1.0f / vertexNumber;
        float uvStepV = 1.0f / floorNumber;
        for (int i = 0; i < floorNumber; i++) {
            for (int j = 0; j < vertexNumber; j ++) {
                UVs[i * vertexNumber * 2 + j * 2] = new Vector2(j * uvStepH, i * uvStepV);
                UVs[(i + 1) * vertexNumber * 2 + j * 2] = new Vector2(j * uvStepH, (i + 1) * uvStepV);
                UVs[i * vertexNumber * 2 + j * 2 + 1] = new Vector2((j + 1) * uvStepH, i * uvStepV);
                UVs[(i + 1) * vertexNumber * 2 + j * 2 + 1] = new Vector2((j + 1) * uvStepH, (i + 1) * uvStepV);
                if (j < vertexNumber) {
                    triangles[vertexNumber * i * 6 + j * 6 + 0] = vertexNumber * 2 * i + j * 2;
                    triangles[vertexNumber * i * 6 + j * 6 + 1] = vertexNumber * 2 * (i + 1) + j * 2;
                    triangles[vertexNumber * i * 6 + j * 6 + 2] = vertexNumber * 2 * (i + 1) + j * 2 + 1;
                    triangles[vertexNumber * i * 6 + j * 6 + 3] = vertexNumber * 2 * (i + 1) + j * 2 + 1;
                    triangles[vertexNumber * i * 6 + j * 6 + 4] = vertexNumber * 2 * i + j * 2 + 1;
                    triangles[vertexNumber * i * 6 + j * 6 + 5] = vertexNumber * 2 * i + j * 2; 
                }
            }
        }
        mesh.triangles = triangles;
        mesh.uv = UVs;
        mesh.RecalculateNormals();
        // mesh.RecalculateTangents();
        return mesh;
    }
    void MakeTowerWalls() {
        Vector3[] towerAllVerteces = CreateTowerVerteces();
        tower = new GameObject();
        MeshFilter mf = tower.AddComponent<MeshFilter>();
        tower.name = "Tower";
        tower.tag = "EditorOnly";
        mf.mesh = CreateTowerMesh(towerAllVerteces);
        tower.AddComponent<MeshRenderer>();
        tower.GetComponent<MeshRenderer>().material = material;
    }
    
    Vector3 TransformVector2To3(Vector2 vector, bool external) {
        float segment = vector.x * vertexNumber / levelConfiguration.width;
        bool rounded = false;
        if (Mathf.Approximately(Mathf.Round(segment), segment)) {
            segment = Mathf.Round(segment);
            rounded = true;
        }

        Vector3 result = external ? towerVerticesExternal[((int)Mathf.Floor(segment) * 2) % towerVerticesExternal.Length] : 
                                    towerVerticesInternal[((int)Mathf.Floor(segment) * 2) % towerVerticesInternal.Length];
        Vector3 nextVertex = external ? towerVerticesExternal[(((int)Mathf.Floor(segment) + 1) * 2) % towerVerticesExternal.Length] : 
                                        towerVerticesInternal[(((int)Mathf.Floor(segment) + 1) * 2) % towerVerticesInternal.Length];
        if (!rounded) {
            result = Vector3.MoveTowards(result, nextVertex, Vector3.Distance(result, nextVertex) * (segment - Mathf.Floor(segment)));
        }
        result += Vector3.up * vector.y * towerHeight / levelConfiguration.height;
        return result;
    }

    Mesh CreateFigureMesh(Vector2[] vertices, int[] triangles) {
        towerVerticesExternal = CreatePolygonVerteces(vertexNumber, towerRadius);
        towerVerticesInternal = CreatePolygonVerteces(vertexNumber, towerRadius - floorWidth);
        Vector3[] vertices3D = new Vector3[vertices.Length];
        Vector3[] vertices3DInternal = new Vector3[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices3D[i] = TransformVector2To3(vertices[i], true);
            vertices3DInternal[i] = TransformVector2To3(vertices[i], false);
        }

        Vector3[] mVertices = new Vector3[vertices.Length * 4 + triangles.Length];
        int[] mTriangles = new int[vertices.Length * 6 + triangles.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            int indexExt = i * 2;
            int indexInt = vertices3D.Length * 2 + i * 2;

            mVertices[indexExt] = vertices3D[i];
            mVertices[indexExt + 1] = vertices3D[(i + 1) % vertices3D.Length];
            mVertices[indexInt] = vertices3DInternal[i];
            mVertices[indexInt + 1] = vertices3DInternal[(i + 1) % vertices3D.Length];

            mTriangles[i * 6 + 0] = indexExt;
            mTriangles[i * 6 + 1] = indexInt;
            mTriangles[i * 6 + 2] = indexInt + 1;
            mTriangles[i * 6 + 3] = indexInt + 1;
            mTriangles[i * 6 + 4] = indexExt + 1;
            mTriangles[i * 6 + 5] = indexExt;
        }

        int indexVertStart = vertices.Length * 4;
        int indexTriangStart = vertices.Length * 6;
        for (int i = 0; i < triangles.Length; i++)
        {
            mVertices[indexVertStart + i] = vertices3DInternal[triangles[i]];
            mTriangles[indexTriangStart + i] = indexVertStart + i;
        }
        Mesh mesh = new Mesh();
        mesh.vertices = mVertices;
        mesh.triangles = mTriangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    void CreateTower() {
        ClearAll();
        MakeTowerWalls();
        textureScanner.ScanTexture(levelConfiguration);
        textureScanner.AddTowerEdges(vertexNumber);
        List<Utility.Figure> figs2D = textureScanner.GetFigures();
        int x = 0;
        foreach (Utility.Figure figure2D in figs2D) {
            x++;
            (Vector2[] vertices, int[] triangles) res = figure2D.CreateMesh();
            // Mesh mesh = CreateFigureMesh(res.vertices, res.triangles);
            GameObject figure = new GameObject();
            MeshFilter mf = figure.AddComponent<MeshFilter>();
            figure.GetComponent<Transform>().parent = tower.GetComponent<Transform>();
            figure.name = "Figure" + x;
            figure.tag = "EditorOnly";
            mf.mesh = CreateFigureMesh(res.vertices, res.triangles);
            MeshRenderer mr = figure.AddComponent<MeshRenderer>();       
            mr.material = materialInterior;
        }
    }
    

    void ClearAll() {
        sphereSpawner.Clear();
        if (figures != null) 
            figures.Clear();
        textureScanner.SphereSpawnerClear();
        textureScanner = new Utility.TextureScanner();
        GameObject[] empt =  GameObject.FindGameObjectsWithTag("EditorOnly");
        foreach (GameObject e in empt) {
            GameObject.DestroyImmediate(e);
        }
        GameObject.DestroyImmediate(tower);
    }

    void SavePrefab() {
        string path = EditorUtility.SaveFilePanelInProject("Save mesh", "T", "", "Please enter a file name to save the texture to");
        AssetDatabase.CreateAsset(tower.GetComponent<MeshFilter>().sharedMesh, path + "walls" + ".asset");
        int i = 0;
        // AssetDatabase.CreateAsset(material, path + "material" + ".asset");
        foreach (GameObject f in figures) {
            i++;
            AssetDatabase.CreateAsset(f.GetComponent<MeshFilter>().sharedMesh, path + "mesh" + i + ".asset");
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(tower, path + "tower" + ".prefab", InteractionMode.UserAction);
    }

    void Test() {
        vertexNumber++;
        CreateTower();
    }
}
