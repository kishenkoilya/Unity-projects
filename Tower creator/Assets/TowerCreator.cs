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

    private const float coordinateError = 0.0001f;
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

    (List<Vector3>, List<Vector3>, LinkedList<Vector2>) CreateLevelFigureVertices(LinkedList<Vector2> figureVertices) {
        List<Vector3> levelFigureVertices = new List<Vector3>();
        List<Vector3> levelFigureVerticesInternal = new List<Vector3>();
        Vector3[] polygonVertices = CreatePolygonVerteces(vertexNumber, towerRadius);
        Vector3[] polygonVerticesInternal = CreatePolygonVerteces(vertexNumber, towerRadius - floorWidth);
        float polygonLength = 0;
        float polygonLengthInternal = 0;
        for (int i = 0; i < polygonVertices.Length; i+=2) {
            polygonLength += Vector3.Distance(polygonVertices[i], polygonVertices[(i+2)%polygonVertices.Length]);
        }
        for (int i = 0; i < polygonVerticesInternal.Length; i+=2) {
            polygonLengthInternal += Vector3.Distance(polygonVerticesInternal[i], polygonVerticesInternal[(i+2)%polygonVerticesInternal.Length]);
        }
        int imageWidth = levelConfiguration.width;
        int imageHeigth = levelConfiguration.height;
        LinkedListNode<Vector2> currentNode, nextNode;
        currentNode = figureVertices.First;
        nextNode = currentNode.Next == null ? figureVertices.First : currentNode.Next;

        //First coordinate search
        float distanceToMoveX = currentNode.Value.x / imageWidth * polygonLength;
        float distanceToMoveXInternal = currentNode.Value.x / imageWidth * polygonLengthInternal;
        Vector3 vertexCoordinates = polygonVertices[0];
        Vector3 vertexCoordinatesInternal = polygonVerticesInternal[0];
        int polygonVertexNumber = 0;
        for (int i = 0; i < polygonVertices.Length; i+=2) {
            float pvDistance = Vector3.Distance(vertexCoordinates, polygonVertices[(i+2)%polygonVertices.Length]);
            float pvDistanceInternal = Vector3.Distance(vertexCoordinatesInternal, polygonVerticesInternal[(i+2)%polygonVerticesInternal.Length]);
            if (distanceToMoveX > pvDistance) {
                distanceToMoveX -= pvDistance;
                distanceToMoveXInternal -= pvDistanceInternal;
                vertexCoordinates = polygonVertices[(i+2)%polygonVertices.Length];
                vertexCoordinatesInternal = polygonVerticesInternal[(i+2)%polygonVerticesInternal.Length];
            }
            else {
                vertexCoordinates = Vector3.MoveTowards(vertexCoordinates, polygonVertices[(i+2)%polygonVertices.Length], distanceToMoveX);
                vertexCoordinatesInternal = Vector3.MoveTowards(vertexCoordinatesInternal, polygonVerticesInternal[(i+2)%polygonVerticesInternal.Length], distanceToMoveXInternal);
                distanceToMoveX = 0;
                distanceToMoveXInternal = 0;
                polygonVertexNumber = i;
                break;
            }
        }
        vertexCoordinates += Vector3.up * (currentNode.Value.y / imageHeigth * towerHeight);
        vertexCoordinatesInternal += Vector3.up * (currentNode.Value.y / imageHeigth * towerHeight);
        levelFigureVertices.Add(vertexCoordinates);
        levelFigureVerticesInternal.Add(vertexCoordinatesInternal);
        float distanceToMoveY;
        int figureVerticesCount = figureVertices.Count;
        for (int i = 0; i < figureVerticesCount; i++)
        {
            distanceToMoveX = (nextNode.Value.x - currentNode.Value.x) * polygonLength / imageWidth;
            distanceToMoveXInternal = (nextNode.Value.x - currentNode.Value.x) * polygonLengthInternal / imageWidth;
            distanceToMoveY = (nextNode.Value.y - currentNode.Value.y) * towerHeight / imageHeigth;
            float pvDistance;
            float pvDistanceInternal;
            if (distanceToMoveX != 0) {
                int j, nj;
                j = polygonVertexNumber;
                nj = distanceToMoveX > 0 ? j + 2 : j;
                while (true) {  
                    // Debug.Log($"j: {j}, nj: {nj}, polygonVertices.Length: {polygonVertices.Length}, res: {(nj)%polygonVertices.Length}");
                    if (Vector3.Distance(new Vector3 (vertexCoordinates.x, 0, vertexCoordinates.z), polygonVertices[(nj)%polygonVertices.Length]) < coordinateError) {
                        j = distanceToMoveX > 0 ? nj : j - 2;
                        nj = distanceToMoveX > 0 ? j + 2 : j; 
                    }  
                    pvDistance = Vector3.Distance(new Vector3 (vertexCoordinates.x, 0, vertexCoordinates.z), polygonVertices[(nj)%polygonVertices.Length]);
                    pvDistanceInternal = Vector3.Distance(new Vector3 (vertexCoordinatesInternal.x, 0, vertexCoordinatesInternal.z), polygonVerticesInternal[(nj)%polygonVerticesInternal.Length]);
                    if (Mathf.Abs(distanceToMoveX) > pvDistance || Mathf.Abs(Mathf.Abs(distanceToMoveX) - pvDistance) < coordinateError) {
                        vertexCoordinates = new Vector3(polygonVertices[(nj)%polygonVertices.Length].x, vertexCoordinates.y, polygonVertices[(nj)%polygonVertices.Length].z);
                        vertexCoordinatesInternal = new Vector3(polygonVerticesInternal[(nj)%polygonVerticesInternal.Length].x, vertexCoordinatesInternal.y, polygonVerticesInternal[(nj)%polygonVerticesInternal.Length].z);
                        float vertexYCoord = vertexCoordinates.y;
                        vertexCoordinates += Vector3.up * (pvDistance / Mathf.Abs(distanceToMoveX)) * distanceToMoveY;
                        vertexCoordinatesInternal += Vector3.up * (pvDistance / Mathf.Abs(distanceToMoveX)) * distanceToMoveY;
                        distanceToMoveY -= vertexCoordinates.y - vertexYCoord;
                        if (Mathf.Abs(Mathf.Abs(distanceToMoveX) - pvDistance) > coordinateError) {
                            currentNode = figureVertices.AddAfter(currentNode, new Vector2( currentNode.Value.x + (pvDistance / Mathf.Abs(distanceToMoveX)) * (nextNode.Value.x - currentNode.Value.x), 
                                                                                            currentNode.Value.y + (pvDistance / Mathf.Abs(distanceToMoveX)) * (nextNode.Value.y - currentNode.Value.y)));
                        }
                        distanceToMoveX += distanceToMoveX > 0 ? -pvDistance : pvDistance;
                        distanceToMoveXInternal += distanceToMoveXInternal > 0 ? -pvDistanceInternal : pvDistanceInternal;
                        levelFigureVertices.Add(vertexCoordinates);
                        levelFigureVerticesInternal.Add(vertexCoordinatesInternal);
                        if (Mathf.Abs(distanceToMoveX) < coordinateError){
                            distanceToMoveX = 0;
                            distanceToMoveXInternal = 0;
                            distanceToMoveY = 0;
                            polygonVertexNumber = j;
                            break;
                        }
                    }
                    else {
                        vertexCoordinates = Vector3.MoveTowards(new Vector3(vertexCoordinates.x, 0, vertexCoordinates.z), polygonVertices[(nj)%polygonVertices.Length], Mathf.Abs(distanceToMoveX)) + 
                                            Vector3.up * (vertexCoordinates.y + distanceToMoveY);
                        vertexCoordinatesInternal = Vector3.MoveTowards(new Vector3(vertexCoordinatesInternal.x, 0, vertexCoordinatesInternal.z), polygonVerticesInternal[(nj)%polygonVerticesInternal.Length], Mathf.Abs(distanceToMoveXInternal)) + 
                                            Vector3.up * (vertexCoordinatesInternal.y + distanceToMoveY);
                        levelFigureVertices.Add(vertexCoordinates);
                        levelFigureVerticesInternal.Add(vertexCoordinatesInternal);
                        distanceToMoveX = 0;
                        distanceToMoveXInternal = 0;
                        distanceToMoveY = 0;
                        polygonVertexNumber = j;
                        break;
                    }
                    j += distanceToMoveX > 0 ? 2 : -2;
                    j += j < 0 ? polygonVertices.Length : 0;
                    nj += distanceToMoveX > 0 ? 2 : -2;
                    nj += nj < 0 ? polygonVertices.Length : 0;
                }
            }
            else {
                vertexCoordinates += Vector3.up * distanceToMoveY;
                vertexCoordinatesInternal += Vector3.up * distanceToMoveY;
                if (i < figureVerticesCount - 1) {
                    levelFigureVertices.Add(vertexCoordinates);
                    levelFigureVerticesInternal.Add(vertexCoordinatesInternal);
                }
            }

            currentNode = nextNode;
            nextNode = currentNode.Next == null ? figureVertices.First : currentNode.Next;
        }
        return (levelFigureVertices, levelFigureVerticesInternal, figureVertices);
    }

    (Vector3[], int[]) CreateFigureMesh(List<Vector3> wallVertices, List<Vector3> internalVertices, Utility.Figure figure) {
        Vector3[] mVertices = new Vector3[wallVertices.Count + internalVertices.Count];
        wallVertices.CopyTo(mVertices);
        internalVertices.CopyTo(mVertices, wallVertices.Count);
        int[] mTriangles = new int[(wallVertices.Count + internalVertices.Count) * 3 + (internalVertices.Count - 2) * 3];
        for (int i = 0; i < wallVertices.Count; i++) {
            mTriangles[i * 6 + 0] = i;
            mTriangles[i * 6 + 1] = i + wallVertices.Count;
            mTriangles[i * 6 + 2] = (i + 1) % wallVertices.Count + wallVertices.Count;
            mTriangles[i * 6 + 3] = (i + 1) % wallVertices.Count + wallVertices.Count;
            mTriangles[i * 6 + 4] = (i + 1) % wallVertices.Count;
            mTriangles[i * 6 + 5] = i;
        }

        int[] frontTriangles = figure.CreateMesh(vertexNumber).triangles;
        // Debug.Log($"wallVertices.Count: {wallVertices.Count}, internalVertices.Count: {internalVertices.Count}, " + 
        //             $"result: {(wallVertices.Count + internalVertices.Count) * 3 + (internalVertices.Count - 2) * 3}" + 
        //             $"frontTriangles.Length: {frontTriangles.Length}");
        for (int i = 0; i < frontTriangles.Length; i++) {
            // Debug.Log($"mTl: {mTriangles.Length}, mTi: {wallVertices.Count * 6 + i}, fTl: {frontTriangles.Length}, fTi: {i}");
            mTriangles[wallVertices.Count * 6 + i] = frontTriangles[i] + wallVertices.Count;
        }

        return (mVertices, mTriangles);
    }
        void CreateTower() {
        ClearAll();
        List<Utility.Figure> figs2D = textureScanner.ScanTexture(levelConfiguration);
        MakeTowerWalls();
        int x = 0;
        
        Mesh superMesh = new Mesh();
        List<Vector3> verticesSM = new List<Vector3>();
        List<int> trianglesSM = new List<int>();
        int verticesCount = 0;
        int trianglesCount = 0;
        foreach (Utility.Figure figure2D in figs2D) {
            x++;
            // if (x != 10)
            //     continue;
            (List<Vector3> levelVertices, List<Vector3> levelVerticesInternal, LinkedList<Vector2> figureVertices) vert = CreateLevelFigureVertices(figure2D.GetVertices());
            // int i = sphereSpawner.CreateSphereList();
            // foreach (Vector3 v in vert.levelVertices) {
            //     sphereSpawner.SpawnSphere(i, v, 1.5f);
            // }
            // i = sphereSpawner.CreateSphereList();
            // foreach (Vector3 v in vert.levelVerticesInternal) {
            //     sphereSpawner.SpawnSphere(i, v, 1.5f);
            // }
            
            // GameObject figure = new GameObject();
            // MeshFilter mf = figure.AddComponent<MeshFilter>();
            // figure.GetComponent<Transform>().parent = tower.GetComponent<Transform>();
            // figure.name = "Figure" + x;
            // figure.tag = "EditorOnly";
            // mf.mesh = CreateFigureMesh(vert.levelVertices, vert.levelVerticesInternal, figure2D);
            (Vector3[] vs, int[] ts) res = CreateFigureMesh(vert.levelVertices, vert.levelVerticesInternal, figure2D);
            for (int j = 0; j < res.vs.Length; j++) {
                verticesSM.Add(res.vs[j]);
            }
            for (int j = 0; j < res.ts.Length; j++) {
                trianglesSM.Add(res.ts[j] + verticesCount);
            }
            verticesCount += res.vs.Length;
            trianglesCount += res.ts.Length;
            // MeshRenderer mr = figure.AddComponent<MeshRenderer>();       
            // mr.material = material;
            // if (figures == null)
                // figures = new List<GameObject>();   
            // figures.Add(figure);  
        }
        Vector3[] vs = new Vector3[verticesSM.Count];
        verticesSM.CopyTo(vs);
        int[] ts = new int[trianglesSM.Count];
        trianglesSM.CopyTo(ts);
        superMesh.vertices = vs;
        superMesh.triangles = ts;
        superMesh.RecalculateNormals();
        GameObject fig = new GameObject();
        MeshFilter mf0 = fig.AddComponent<MeshFilter>();
        fig.GetComponent<Transform>().parent = tower.GetComponent<Transform>();
        fig.name = "Figure0";
        fig.tag = "EditorOnly";
        mf0.mesh = superMesh;
        MeshRenderer mr0 = fig.AddComponent<MeshRenderer>();       
        mr0.material = materialInterior;
        if (figures == null)
            figures = new List<GameObject>();   
        figures.Add(fig);     
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
