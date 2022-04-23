using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Utility {
    public class Figure
    {
        private LinkedList<Vector2> vertices;
        private LinkedList<LinkedList<Vector2>> holes;
        private int textureWidth;
        private int textureHeight;
        private const float coordinateError = 0.0001f; 

        public Figure(int width, int height) {
            vertices = new LinkedList<Vector2>();
            holes = new LinkedList<LinkedList<Vector2>>();
            textureWidth = width;
            textureHeight = height;
        }
        public LinkedListNode<Vector2> FindVertexInHoles(Vector2 v) {
            LinkedListNode<Vector2> result = null;
            foreach (LinkedList<Vector2> hole in holes) {
                result = hole.Find(v);
                if (result != null)
                    return result;
            }
            return result;
        }
        
        public LinkedListNode<Vector2> FindVertexInVertices(Vector2 v) {
            return vertices.Find(v);
        }
        public LinkedList<Vector2> GetVertices() {
            return vertices;
        }

        public void SetVertices(LinkedList<Vector2> ll) {
            vertices = ll;
        }

        public bool AddVertex(Vector2 v, bool isHole = false) {
            if (isHole) {
                if (vertices.Find(v) == null) {
                    holes.Last.Value.AddLast(v);
                    return true;
                }
            }
            else {
                vertices.AddLast(v);
                return true;
            }
            return false;
        }
        public void NewHole() {
            holes.AddLast(new LinkedList<Vector2>());
        }
        public void MergeWithHoles() {
            if (holes.Count == 0)
                return;
            while (holes.Count > 0) { 
                float dist = float.PositiveInfinity;
                LinkedListNode<LinkedList<Vector2>> holeNode = holes.First;
                LinkedListNode<Vector2> holeVertexNode = holes.First.Value.First;
                LinkedListNode<Vector2> vertexNode = vertices.First;
                foreach (Vector2 ver in vertices) {
                    LinkedListNode<LinkedList<Vector2>> holeNodeIter = holes.First;
                    foreach (LinkedList<Vector2> hole in holes) {
                        foreach (Vector2 holeV in hole) {
                            if (Vector2.Distance(ver, holeV) < dist) {
                                dist = Vector2.Distance(ver, holeV);
                                vertexNode = vertices.Find(ver);
                                holeVertexNode = hole.Find(holeV);
                                holeNode = holeNodeIter;
                            }
                        }
                        if (holeNodeIter.Next != null)
                            holeNodeIter = holeNodeIter.Next;
                    }
                }
                if (dist == float.PositiveInfinity)
                    holes.Clear();
                else {
                    Vector2 vertexLink = vertexNode.Value;
                    LinkedListNode<Vector2> holeNodeCopy = holeVertexNode;
                    for (int i = 0; i <= holeNode.Value.Count; i++) {
                        vertices.AddAfter(vertexNode, holeVertexNode.Value);
                        vertexNode = vertexNode.Next;
                        if (holeVertexNode.Next != null)
                            holeVertexNode = holeVertexNode.Next;
                        else
                            holeVertexNode = holeNode.Value.First;
                    }
                    vertices.AddAfter(vertexNode, vertexLink);
                    holes.Remove(holeNode);
                }
            }
        }
        public void VisualizeVertices(SphereSpawner ss, int i) {                
            foreach (Vector2 v in vertices) {
                ss.SpawnSphere(i, new Vector3(v.x, 0, v.y));
            }
            foreach (LinkedList<Vector2> ll in holes) {
                foreach (Vector2 v in ll) {
                    ss.SpawnSphere(i, new Vector3(v.x, 0, v.y));
                }
            }
        }

        private float CrossProduction2D(Vector2 to1, Vector2 to2, Vector2 from) {//8,3   10,1   14,1
            return (to1.x - from.x) * (to2.y - from.y) - (to2.x - from.x) * (to1.y - from.y);//(8 - 14)(1 - 1) - (10 - 14)(3 - 1)
        }
        private bool CheckIsVertexInsideTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 point) {
            if (Vector2.Distance(point, v1) < coordinateError || Vector2.Distance(point, v2) < coordinateError || Vector2.Distance(point, v3) < coordinateError)
                return false;
            // if (CrossProduction2D(v1,v2,v3) == 0)
            //     return true;
            float det1 = CrossProduction2D(point, v1, v2);
            float det2 = CrossProduction2D(point, v2, v3);
            float det3 = CrossProduction2D(point, v3, v1);
            bool has_neg = (det1 < 0) || (det2 < 0) || (det3 < 0);
            bool has_pos = (det1 > 0) || (det2 > 0) || (det3 > 0);
            return !(has_neg && has_pos);
        }
        private bool CheckIsVerticesInsideTriangle(LinkedListNode<Vector2> currentNode) {
            LinkedListNode<Vector2> previousNode, nextNode, checkingNode;
            previousNode = currentNode.Previous == null ? vertices.Last : currentNode.Previous;
            nextNode = currentNode.Next == null ? vertices.First : currentNode.Next;
            checkingNode = nextNode.Next == null ? vertices.First : nextNode.Next;
            for (int i = 0; i < vertices.Count - 3; i++) {
                if (CheckIsVertexInsideTriangle(previousNode.Value, currentNode.Value, nextNode.Value, checkingNode.Value)) {
                    return true;
                }
                checkingNode = checkingNode.Next == null ? vertices.First : checkingNode.Next;
            }
            return false;
        }

        private (Dictionary<LinkedListNode<Vector2>, int>, Vector3[]) CreateMeshVertices() {
            LinkedListNode<Vector2> currentNode = vertices.First;
            Vector3[] mVertices = new Vector3[vertices.Count];
            Dictionary<LinkedListNode<Vector2>, int> listToVerticeNumDictionary = new Dictionary<LinkedListNode<Vector2>, int>();
            for (int i = 0; i < vertices.Count; i++)
            {
                mVertices[i] = new Vector3(currentNode.Value.x, 0, currentNode.Value.y);
                listToVerticeNumDictionary.Add(currentNode, i);
                currentNode = currentNode.Next == null ? vertices.First : currentNode.Next;
            }
            return (listToVerticeNumDictionary, mVertices);
        }

        private bool VerticesInOneSegment(int segmentsNumber, Vector2 v1, Vector2 v2, Vector2 v3) {
            float seg1, seg2, seg3;
            
            seg1 = v1.x * segmentsNumber / textureWidth;
            seg2 = v2.x * segmentsNumber / textureWidth;
            seg3 = v3.x * segmentsNumber / textureWidth;
            if (Mathf.Abs(Mathf.Round(seg1) - seg1) < coordinateError * 10)
                seg1 = Mathf.Round(seg1);
            if (Mathf.Abs(Mathf.Round(seg2) - seg2) < coordinateError * 10)
                seg2 = Mathf.Round(seg2);
            if (Mathf.Abs(Mathf.Round(seg3) - seg3) < coordinateError * 10)
                seg3 = Mathf.Round(seg3);
            if (v1.y != v2.y || v2.y != v3.y || v3.y != v1.y) {
                // Debug.Log($"v1: {v1}, v2: {v2}, v3: {v3}");
                // Debug.Log($"seg1: {seg1},seg2: {seg2},seg3: {seg3}, left: {Mathf.Ceil(Mathf.Max(seg1, seg2, seg3))}, right: {Mathf.Floor(Mathf.Min(seg1, seg2, seg3))}, result: {Mathf.Ceil(Mathf.Max(seg1, seg2, seg3)) - Mathf.Floor(Mathf.Min(seg1, seg2, seg3))}," + 
                // $" bool: {Mathf.Ceil(Mathf.Max(seg1, seg2, seg3)) - Mathf.Floor(Mathf.Min(seg1, seg2, seg3)) <= 1}");
            }
            return Mathf.Ceil(Mathf.Max(seg1, seg2, seg3)) - Mathf.Floor(Mathf.Min(seg1, seg2, seg3)) <= 1;
        }
        private int[] CreateMeshTriangles(Dictionary<LinkedListNode<Vector2>, int> listToVerticeNumDictionary, int segmentsNumber) {
            LinkedListNode<Vector2> currentNode = vertices.First;
            LinkedListNode<Vector2> previousNode, nextNode;
            int[] mTriangles = new int[(vertices.Count - 2) * 3];
            int triangleNum = 0;
            int counter = 0;
            int faultCounterSegment = 0;
            int faultCounterAngle = 0;
            int faultCounterPointInTriangle = 0;
            while (vertices.Count > 2) {
                counter++;
                if (counter > 1000000) {
                    Debug.LogError("Mesh triangles failed to triangulate.");
                    Debug.LogError($"faultCounterSegment: {faultCounterSegment}, faultCounterAngle: {faultCounterAngle}, faultCounterPointInTriangle: {faultCounterPointInTriangle}");
                    break;
                }
                previousNode = currentNode.Previous == null ? vertices.Last : currentNode.Previous;
                nextNode = currentNode.Next == null ? vertices.First : currentNode.Next;
                if (!VerticesInOneSegment(segmentsNumber, previousNode.Value, currentNode.Value, nextNode.Value)){
                    currentNode = nextNode;
                    faultCounterSegment++;
                    continue;
                }
                float crossProd = CrossProduction2D(nextNode.Value, previousNode.Value, currentNode.Value);
                if (crossProd > 0) {
                    bool triangleEmpty = !CheckIsVerticesInsideTriangle(currentNode);
                    if (triangleEmpty) {
                        mTriangles[triangleNum * 3 + 0] = listToVerticeNumDictionary[nextNode];
                        mTriangles[triangleNum * 3 + 1] = listToVerticeNumDictionary[currentNode];
                        mTriangles[triangleNum * 3 + 2] = listToVerticeNumDictionary[previousNode];
                        triangleNum++;
                        vertices.Remove(currentNode);
                        currentNode = nextNode;
                        continue;
                    }
                    else {
                        faultCounterPointInTriangle++;
                        // Debug.Log("Is point inside");
                    }
                }
                else {
                    faultCounterAngle++;
                    // Debug.Log($"v1: {nextNode.Value}, v2: {previousNode.Value}, v3: {currentNode.Value}, crossP: {crossProd}");
                }
                currentNode = nextNode;
            }
            return mTriangles;
        }
        public Mesh CreateMesh(int segmentsNumber) {
            Mesh mesh = new Mesh();
            (Dictionary<LinkedListNode<Vector2>, int> listToVerticeNumDictionary, Vector3[] mVertices) = CreateMeshVertices();
            mesh.vertices = mVertices;
            // SphereSpawner ss = new SphereSpawner();
            // int x = ss.CreateSphereList();
            // VisualizeVertices(ss, x);
                    mesh.triangles = CreateMeshTriangles(listToVerticeNumDictionary, segmentsNumber);
            // x = ss.CreateSphereList();
            // VisualizeVertices(ss, x);
            // mesh.RecalculateNormals();
            // GameObject go = new GameObject("Figure");
            // go.tag = "EditorOnly";
            // MeshFilter mf = go.AddComponent<MeshFilter>();
            // mf.mesh = mesh;
            // go.AddComponent<MeshRenderer>();
            return mesh;
        }

    }
}
