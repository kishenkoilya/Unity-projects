using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility {
    public class TextureScanner
    {
        SphereSpawner ss = new SphereSpawner();
        Texture2D tex;
        int h;
        int w;

        Color[] pixels;
        bool[] scannedPixels;
        List<Figure> figures = new List<Figure>();

        void InitializeScannedPixels() {
            scannedPixels = new bool[tex.height * tex.width];
        }

        bool WhiteOrOutOfBorders(int x, int y) {
            if (y < 0 || y >= h || x < 0 || x >= w) 
                return true;
            else if (pixels[y * w + x] == Color.white)
                return true;
            else 
                return false;
        }
        // bool CheckBoundaries(int x, int left, int right, bool checkingLeft) {
        //     if (left == int.MaxValue)
        //         return true;
        //     else if (right == int.MinValue)
        //         return true;
        //     else {
        //         return checkingLeft ? 
        //     }
        // }
        void AddRightBoundary(int x, int y, Dictionary<int, List<(int, int)>> boundaries) {
            if (!boundaries.ContainsKey(y)) {
                boundaries[y] = new List<(int, int)>();
                boundaries[y].Add((int.MaxValue, x));
            }
            else {
                int distance = int.MaxValue;
                int index = -1;
                for (int i = 0; i < boundaries[y].Count; i++) {
                // if (y == 58) {
                //     Debug.Log($"x: {x}, bi2: {boundaries[y][i].Item1}, dist: {x - boundaries[y][i].Item1}, distInt: {boundaries[y][i].Item2 - boundaries[y][i].Item1}");
                // }
                    if (x - boundaries[y][i].Item1 >= 0 && x - boundaries[y][i].Item1 < distance && (boundaries[y][i].Item2 == int.MinValue || x < boundaries[y][i].Item2)) {
                        index = i;
                        distance = x - boundaries[y][i].Item1;
                    }
                }
                if (index >= 0) {
                    if (boundaries[y][index].Item2 != int.MinValue)
                        boundaries[y].Add((int.MaxValue, boundaries[y][index].Item2));
                    boundaries[y][index] = (boundaries[y][index].Item1, x);
                }
                else {
                    boundaries[y].Add((int.MaxValue, x));
                }
            }
            // if (y == 58) {
            //     string resp = "Right 58: ";
            //     for (int i = 0; i < boundaries[y].Count; i++) {
            //         resp += boundaries[y][i];
            //     }
            //     Debug.Log(resp);
            // }
        }

        void AddLeftBoundary(int x, int y, Dictionary<int, List<(int, int)>> boundaries) {
            int distance = int.MaxValue;
            int index = -1;
            for (int i = 0; i < boundaries[y].Count; i++) {
                // if (y == 58) {
                //     Debug.Log($"x: {x}, bi2: {boundaries[y][i].Item2}, dist: {boundaries[y][i].Item2 - x}, distInt: {boundaries[y][i].Item2 - boundaries[y][i].Item1}");
                // }
                if (boundaries[y][i].Item2 - x >= 0 && boundaries[y][i].Item2 - x < distance && (boundaries[y][i].Item1 == int.MaxValue || x > boundaries[y][i].Item1)) {
                    distance = boundaries[y][i].Item2 - x;
                    index = i;
                }
            }
            if (index >= 0) {
                if (boundaries[y][index].Item1 != int.MaxValue) 
                    boundaries[y].Add((boundaries[y][index].Item1, int.MinValue));
                boundaries[y][index] = (x, boundaries[y][index].Item2);
            }
            else {
                boundaries[y].Add((x, int.MinValue));
            }
            // if (y == 58) {
            //     string resp = "Left 58: ";
            //     for (int i = 0; i < boundaries[y].Count; i++) {
            //         resp += boundaries[y][i];
            //     }
            //     Debug.Log(resp);
            // }
        }
        
        (Vector2, Vector2, int, int) FindNextAngle(int x, int y, Vector2 lookWhere, Dictionary<int, List<(int, int)>> boundaries) {
            (Vector2 vertex, Vector2 lookWhere, int x, int y) result;
            int lwx = (int)lookWhere.x;
            int lwy = (int)lookWhere.y;

            Vector2 movementDirection = Vector2.Perpendicular(lookWhere);
            int mdx = (int)movementDirection.x;
            int mdy = (int)movementDirection.y;

            while (true) {
                if (!WhiteOrOutOfBorders(x, y) && WhiteOrOutOfBorders(x, y) != WhiteOrOutOfBorders(x + lwx, y + lwy)) {
                    scannedPixels[y * w + x] = true;
                    if (x + lwx >= 0 && x + lwx < w && y + lwy >= 0 && y + lwy < h)
                        scannedPixels[(y + lwy) * w + x + lwx] = true;
                    
                    if (movementDirection == Vector2.up) {
                        AddRightBoundary(x, y, boundaries);
                    }
                    else if (movementDirection == Vector2.down) {
                        AddLeftBoundary(x, y, boundaries);
                    }
                    x += mdx;
                    y += mdy;

                }
                else {
                    if (WhiteOrOutOfBorders(x, y)) {
                        //external
                        x -= mdx;
                        y -= mdy;
                        Vector2 resCoords = new Vector2(x, y);
                        if (movementDirection == Vector2.right || movementDirection == Vector2.up) resCoords += Vector2.right;
                        if (movementDirection == Vector2.left || movementDirection == Vector2.up) resCoords += Vector2.up;
                        lookWhere = Vector2.Perpendicular(lookWhere);
                        result = (resCoords, lookWhere, x, y);
                    }
                    else {
                        //internal
                        x += lwx;
                        y += lwy;
                        Vector2 resCoords = new Vector2(x, y);
                        if (movementDirection == Vector2.right || movementDirection == Vector2.down) resCoords += Vector2.up;
                        if (movementDirection == Vector2.left || movementDirection == Vector2.down) resCoords += Vector2.right;
                        lookWhere = -Vector2.Perpendicular(lookWhere);
                        result = (resCoords, lookWhere, x, y);
                    }
                    break;
                }
            }
            return result;
        }

        void GetOuterVertices(int x, int y, Figure figure, Dictionary<int, List<(int, int)>> boundaries) {
            Vector2 firstVertex = new Vector2(x, y);
            figure.AddVertex(firstVertex);
            Vector2 lookWhere = Vector2.down;
            (Vector2 vertex, Vector2 lookWhere, int x, int y) res = FindNextAngle(x, y, lookWhere, boundaries);
            while (res.vertex != firstVertex) {
                figure.AddVertex(res.vertex);
                res = FindNextAngle(res.x, res.y, res.lookWhere, boundaries);
            }
        }

        void GetHoleVertices(int x, int y, Figure figure, Dictionary<int, List<(int, int)>> boundaries) {
            figure.NewHole();
            y--;
            Vector2 lookWhere = Vector2.up;
            (Vector2 vertex, Vector2 lookWhere, int x, int y) res = FindNextAngle(x, y, lookWhere, boundaries);
            Vector2 firstVertex = res.vertex;
            figure.AddVertex(firstVertex, true);
            res = FindNextAngle(res.x, res.y, res.lookWhere, boundaries);
            while (res.vertex != firstVertex) {
                figure.AddVertex(res.vertex, true);
                res = FindNextAngle(res.x, res.y, res.lookWhere, boundaries);
            }
        }

        void ScanInteriors(Figure figure, Dictionary<int, List<(int, int)>> boundaries) {
            while (boundaries.Count > 0) {
                List<int> scannedKeys = new List<int>(boundaries.Count);
                foreach(KeyValuePair<int, List<(int, int)>> kv in boundaries) {
                    bool breaking = false;
                    kv.Value.Sort();
                    foreach ((int, int) borders in kv.Value) {
                        for (int i = borders.Item1; i < borders.Item2; i++) {
                            if (!scannedPixels[kv.Key * w + i]) {
                                if (pixels[kv.Key * w + i] != Color.white)
                                    scannedPixels[kv.Key * w + i] = true;
                                else {
                                    GetHoleVertices(i, kv.Key, figure, boundaries);
                                    breaking = true;                                
                                    break;
                                }
                            }
                        }
                        if (breaking) break;
                    }
                    if (breaking) break;
                    scannedKeys.Add(kv.Key);
                }

                for (int i = 0; i < scannedKeys.Count; i++)
                {
                    boundaries.Remove(scannedKeys[i]);
                }
            }
        }

        Figure ScanFigure(int x, int y) {
            Figure figure = new Figure(w, h);
            Dictionary<int, List<(int, int)>> boundaries = new Dictionary<int, List<(int, int)>>();
            GetOuterVertices(x, y, figure, boundaries);
            ScanInteriors(figure, boundaries);
            return figure;
        }

        void FindFigures() {
            h = tex.height;
            w = tex.width;
            pixels = tex.GetPixels();
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    if (!scannedPixels[i * w + j]) {
                        if (pixels[i * w + j] == Color.white)
                            scannedPixels[i * w + j] = true;
                        else {
                            figures.Add(ScanFigure(j, i));
                        }
                    }
                }
            }
        }

        public void ScanTexture(Texture2D texture) {
            tex = texture;
            InitializeScannedPixels();
            FindFigures();
            foreach (Figure f in figures) {
                f.MergeWithHoles();
            }
        }

        public void AddTowerEdges(int edgesNumber) {
            foreach (Figure f in figures) {
                f.AddEdges(edgesNumber);
            }
        }

        public List<Figure> GetFigures() {
            return figures;
        }
        public void SphereSpawnerClear() {
            ss.Clear();
        }
    }
}
