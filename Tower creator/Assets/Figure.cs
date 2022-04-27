using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Utility {
    public class Figure
    {
        private LinkedList<Vector2> vertices;
        private LinkedList<LinkedList<Vector2>> holes;
        private LinkedList<LinkedListNode<Vector2>> concaveVertices;
        private LinkedList<LinkedListNode<Vector2>> ears;
        private int textureWidth;
        private int textureHeight;
        private int towerEdges;
        private float[] edgeXCoordinates;

        public Figure(int width, int height) {
            vertices = new LinkedList<Vector2>();
            holes = new LinkedList<LinkedList<Vector2>>();
            concaveVertices = new LinkedList<LinkedListNode<Vector2>>();
            ears = new LinkedList<LinkedListNode<Vector2>>();
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

        void FillEdgeCoordinates() {
            edgeXCoordinates = new float[towerEdges + 1];
            for (int i = 0; i <= towerEdges; i++)
            {
                edgeXCoordinates[i] = (float)i * textureWidth / towerEdges;
            }
        }

        bool AddEdgeBetween(LinkedListNode<Vector2> currentNode, LinkedListNode<Vector2> nextNode) {
            if (Mathf.Approximately(currentNode.Value.x, nextNode.Value.x)) {
                return false;
            }
            else {
                float segment1 = currentNode.Value.x * towerEdges / textureWidth;
                float segment2 = nextNode.Value.x * towerEdges / textureWidth;
                if (Mathf.Approximately(Mathf.Round(segment1), segment1))
                    segment1 = Mathf.Round(segment1);
                if (Mathf.Approximately(Mathf.Round(segment2), segment2))
                    segment2 = Mathf.Round(segment2);
                
                if (Mathf.Ceil(Mathf.Max(segment1, segment2)) - Mathf.Floor(Mathf.Min(segment1, segment2)) > 1) {
                    Vector2 result;
                    if (segment1 > segment2) {
                        float newX = edgeXCoordinates[(int)(!Mathf.Approximately(Mathf.Floor(segment1), segment1) ? Mathf.Floor(segment1) : Mathf.Floor(segment1) - 1)];
                        if (!Mathf.Approximately(currentNode.Value.y, nextNode.Value.y)) {
                            float newY = (currentNode.Value.x - newX) / (currentNode.Value.x - nextNode.Value.x) * (nextNode.Value.y - currentNode.Value.y) + currentNode.Value.y;
                            result = new Vector2(newX, newY);
                        }
                        else {
                            result = new Vector2(newX, currentNode.Value.y);
                        }
                    }
                    else {
                        float newX = edgeXCoordinates[(int)(!Mathf.Approximately(Mathf.Ceil(segment1), segment1) ? Mathf.Ceil(segment1) : Mathf.Ceil(segment1) + 1)];
                        if (!Mathf.Approximately(currentNode.Value.y, nextNode.Value.y)) {
                            float newY = (newX - currentNode.Value.x) / (nextNode.Value.x - currentNode.Value.x) * (nextNode.Value.y - currentNode.Value.y) + currentNode.Value.y;
                            result = new Vector2(newX, newY);
                        }
                        else {
                            result = new Vector2(newX, currentNode.Value.y);
                        }
                    }
                    vertices.AddAfter(currentNode, result);
                    return true;
                }
                else 
                    return false;
            }
        }

        public void AddEdges(int edgesNumber) {
            LinkedListNode<Vector2> currentNode = vertices.First;
            LinkedListNode<Vector2> nextNode;
            towerEdges = edgesNumber;
            FillEdgeCoordinates();
            while (true) {
                nextNode = currentNode.Next ?? vertices.First;
                if (AddEdgeBetween(currentNode, nextNode)) {
                    currentNode = currentNode.Next ?? vertices.First;
                }
                else {
                    currentNode = nextNode;
                }

                if (currentNode == vertices.First)
                    break;
            }
        }

        private float CrossProduction2D(Vector2 to1, Vector2 to2, Vector2 from) {//8,3   10,1   14,1
            return (to1.x - from.x) * (to2.y - from.y) - (to2.x - from.x) * (to1.y - from.y);//(8 - 14)(1 - 1) - (10 - 14)(3 - 1)
        }
        private bool CheckIsVertexInsideTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 point) {
            if (Vector2.Distance(point, v1) < Mathf.Epsilon || Vector2.Distance(point, v2) < Mathf.Epsilon || Vector2.Distance(point, v3) < Mathf.Epsilon)
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

        private bool CheckTriangleEmpty(LinkedListNode<Vector2> currentNode) {
            LinkedListNode<Vector2> previousNode, nextNode;
            LinkedListNode<LinkedListNode<Vector2>> checkingNode;
            previousNode = currentNode.Previous == null ? vertices.Last : currentNode.Previous;
            nextNode = currentNode.Next == null ? vertices.First : currentNode.Next;
            checkingNode = concaveVertices.First;
            for (int i = 0; i < concaveVertices.Count; i++) {
                if (CheckIsVertexInsideTriangle(previousNode.Value, currentNode.Value, nextNode.Value, checkingNode.Value.Value)) {
                    return false;
                }
                checkingNode = checkingNode.Next ?? concaveVertices.First;
            }
            return true;
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

        private bool VerticesInOneSegment(Vector2 v1, Vector2 v2, Vector2 v3) {
            float seg1, seg2, seg3;
            
            seg1 = v1.x * towerEdges / textureWidth;
            seg2 = v2.x * towerEdges / textureWidth;
            seg3 = v3.x * towerEdges / textureWidth;
            if (Mathf.Approximately(Mathf.Round(seg1), seg1))
                seg1 = Mathf.Round(seg1);
            if (Mathf.Approximately(Mathf.Round(seg2), seg2))
                seg2 = Mathf.Round(seg2);
            if (Mathf.Approximately(Mathf.Round(seg3), seg3))
                seg3 = Mathf.Round(seg3);
            if (v1.y != v2.y || v2.y != v3.y || v3.y != v1.y) {
                // Debug.Log($"v1: {v1}, v2: {v2}, v3: {v3}");
                // Debug.Log($"seg1: {seg1},seg2: {seg2},seg3: {seg3}, left: {Mathf.Ceil(Mathf.Max(seg1, seg2, seg3))}, right: {Mathf.Floor(Mathf.Min(seg1, seg2, seg3))}, result: {Mathf.Ceil(Mathf.Max(seg1, seg2, seg3)) - Mathf.Floor(Mathf.Min(seg1, seg2, seg3))}," + 
                // $" bool: {Mathf.Ceil(Mathf.Max(seg1, seg2, seg3)) - Mathf.Floor(Mathf.Min(seg1, seg2, seg3)) <= 1}");
            }
            return Mathf.Ceil(Mathf.Max(seg1, seg2, seg3)) - Mathf.Floor(Mathf.Min(seg1, seg2, seg3)) <= 1;
        }

        bool CheckVertexIsEar(LinkedListNode<Vector2> node) {
            LinkedListNode<Vector2> nextNode = node.Next ?? vertices.First;
            LinkedListNode<Vector2> previousNode = node.Previous ?? vertices.Last;
            if (CrossProduction2D(nextNode.Value, previousNode.Value, node.Value) > 0) {
                if (VerticesInOneSegment(previousNode.Value, node.Value, nextNode.Value)) {
                    if (CheckTriangleEmpty(node)) {
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
                return false;
        }

        void FindConcaveVertices() {
            LinkedListNode<Vector2> currentNode = vertices.First;
            LinkedListNode<Vector2> nextNode = currentNode.Next ?? vertices.First;
            LinkedListNode<Vector2> previousNode = currentNode.Previous ?? vertices.Last;
            while (true) {
                if (CrossProduction2D(nextNode.Value, previousNode.Value, currentNode.Value) < 0) {
                    concaveVertices.AddLast(currentNode);
                }
                currentNode = nextNode;
                nextNode = currentNode.Next ?? vertices.First;
                previousNode = currentNode.Previous ?? vertices.Last;
                if (currentNode == vertices.First)
                    break;
            }
        }

        void FindEars() {
            LinkedListNode<Vector2> currentNode = vertices.First;
            while (true) {
                if (CheckVertexIsEar(currentNode)) {
                    ears.AddLast(currentNode);
                }
                currentNode = currentNode.Next ?? vertices.First;
                if (currentNode == vertices.First)
                    break;
            }
        }

        void CheckVertexStatus(LinkedListNode<Vector2> node) {
            LinkedListNode<LinkedListNode<Vector2>> ear = null;
            LinkedListNode<LinkedListNode<Vector2>> concave = null;
            LinkedListNode<LinkedListNode<Vector2>> currentNode = ears.First;
            for (int i = 0; i < ears.Count; i++)
            {
                if (node == currentNode.Value) {
                    ear = currentNode;
                    break;
                }
                currentNode = currentNode.Next;
            }
            currentNode = concaveVertices.First;
            for (int i = 0; i < concaveVertices.Count; i++)
            {
                if (node == currentNode.Value) {
                    concave = currentNode;
                    break;
                }
                currentNode = currentNode.Next;
            }

            if (concave != null) {
                LinkedListNode<Vector2> nextNode = node.Next ?? vertices.First;
                LinkedListNode<Vector2> previousNode = node.Previous ?? vertices.Last;
                if (CrossProduction2D(nextNode.Value, previousNode.Value, node.Value) > 0) {
                    concaveVertices.Remove(concave);
                }
            }

            if (CheckVertexIsEar(node)) {
                if (ear == null)
                    ears.AddLast(node);
            }
            else {
                if (ear != null) {
                    ears.Remove(ear);
                }
            }
        }
        private int[] CreateMeshTriangles(Dictionary<LinkedListNode<Vector2>, int> listToVerticeNumDictionary) {
            FindConcaveVertices();
            FindEars();
            LinkedListNode<LinkedListNode<Vector2>> currentNode = ears.First;
            LinkedListNode<Vector2> previousNode, nextNode;
            int[] mTriangles = new int[(vertices.Count - 2) * 3];
            int triangleNum = 0;
            while (ears.Count > 0) {
                previousNode = currentNode.Value.Next ?? vertices.First;
                nextNode = currentNode.Value.Previous ?? vertices.Last;
                // Debug.Log(previousNode.Value + " " + currentNode.Value.Value + " " + nextNode.Value);

                mTriangles[triangleNum * 3 + 0] = listToVerticeNumDictionary[previousNode];
                mTriangles[triangleNum * 3 + 1] = listToVerticeNumDictionary[currentNode.Value];
                mTriangles[triangleNum * 3 + 2] = listToVerticeNumDictionary[nextNode];
                triangleNum++;
                vertices.Remove(currentNode.Value);
                ears.Remove(currentNode);
                CheckVertexStatus(nextNode);
                CheckVertexStatus(previousNode);
                currentNode = ears.First;
            }

            return mTriangles;
        }
        public (Vector2[], int[]) CreateMesh() {
            Mesh mesh = new Mesh();
            Vector2[] verts = new Vector2[vertices.Count];
            vertices.CopyTo(verts, 0);
            (Dictionary<LinkedListNode<Vector2>, int> listToVerticeNumDictionary, Vector3[] mVertices) = CreateMeshVertices();
            mesh.vertices = mVertices;

            // SphereSpawner ss = new SphereSpawner();
            // int x = ss.CreateSphereList();
            // VisualizeVertices(ss, x);
                    mesh.triangles = CreateMeshTriangles(listToVerticeNumDictionary);
            // x = ss.CreateSphereList();
            // VisualizeVertices(ss, x);
            // mesh.RecalculateNormals();
            // GameObject go = new GameObject("Figure");
            // go.tag = "EditorOnly";
            // MeshFilter mf = go.AddComponent<MeshFilter>();
            // mf.mesh = mesh;
            // go.AddComponent<MeshRenderer>();
            return (verts, mesh.triangles);
        }

    }
}
