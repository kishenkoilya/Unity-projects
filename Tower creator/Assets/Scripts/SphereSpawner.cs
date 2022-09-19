using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;


namespace Utility {
    public class SphereSpawner
    {
        public struct SphereColors {
            public Color colorBegin;
            public Color colorEnd;
            public Color colorStep;
            public Color lastColor;
            public int stepSign;
            public SphereColors(Color begin, Color end, Color step, Color last) {
                colorBegin = begin;
                colorEnd = end;
                colorStep = step;
                lastColor = last;
                stepSign = 1;
            }
            public SphereColors(SphereColors sc, int sign) {
                colorBegin = sc.colorBegin;
                colorEnd = sc.colorEnd;
                colorStep = sc.colorStep;
                lastColor = sc.lastColor;
                stepSign = sign;
            }
            public SphereColors(SphereColors sc, Color last) {
                colorBegin = sc.colorBegin;
                colorEnd = sc.colorEnd;
                colorStep = sc.colorStep;
                lastColor = last;
                stepSign = sc.stepSign;
            }
            public void ChangeLastColor(Color newLast) {
                this.lastColor = newLast;
            }
        }
        
        private List<List<GameObject>> spheres = new List<List<GameObject>>();
        private List<GameObject> emptyObjects = new List<GameObject>();
        private List<SphereColors> sphereColors = new List<SphereColors>();
        private Dictionary<int, SphereColors> sphereColorsTemplate = new Dictionary<int, SphereColors>();
        public void Clear() {
            for (int i = spheres.Count - 1; i >= 0; i--) {
                Clear(i);
            }
        }

        public int SphereListCount() {
            return spheres.Count;
        }
        public void Clear(int i) {
            foreach (GameObject go in spheres[i]) {
                GameObject.DestroyImmediate(go);
            }
            spheres[i].Clear();
            spheres.RemoveAt(i);
            GameObject.DestroyImmediate(emptyObjects[i]);
            emptyObjects.RemoveAt(i);
            sphereColors.RemoveAt(i);
        }

        public int CreateSphereList() {
            if (spheres.Count == 0 && sphereColorsTemplate.Count == 0) {
                sphereColorsTemplate.Add(0, new SphereColors(new Color(0.2f,0,0), new Color(1,0,0), new Color(0.1f,0,0), new Color(0.1f,0,0)));
                sphereColorsTemplate.Add(1, new SphereColors(new Color(0,0.2f,0), new Color(0,1,0), new Color(0,0.1f,0), new Color(0,0.1f,0)));
                sphereColorsTemplate.Add(2, new SphereColors(new Color(0,0,0.2f), new Color(0,0,1), new Color(0,0,0.1f), new Color(0,0,0.1f)));
                sphereColorsTemplate.Add(3, new SphereColors(new Color(0.2f,0.2f,0), new Color(1,1,0), new Color(0.1f,0.1f,0), new Color(0.1f,0.1f,0)));
                sphereColorsTemplate.Add(4, new SphereColors(new Color(0,0.2f,0.2f), new Color(0,1,1), new Color(0,0.1f,0.1f), new Color(0,0.1f,0.1f)));
                sphereColorsTemplate.Add(5, new SphereColors(new Color(0.2f,0,0.2f), new Color(1,0,1), new Color(0.1f,0,0.1f), new Color(0.1f,0,0.1f)));
            }
            sphereColors.Add(sphereColorsTemplate[spheres.Count % 6]);
            spheres.Add(new List<GameObject>());
            emptyObjects.Add(new GameObject());
            emptyObjects[emptyObjects.Count - 1].name = "SphereList" + emptyObjects.Count;
            emptyObjects[emptyObjects.Count - 1].tag = "EditorOnly";
            return spheres.Count - 1;
        }

        Material CreateMaterial(int i) {
            Material newMat = new Material(Shader.Find("Standard"));
            if (sphereColors[i].lastColor == sphereColors[i].colorEnd) {
                sphereColors[i] = new SphereColors(sphereColors[i], new Color(sphereColors[i].colorBegin.r - sphereColors[i].colorStep.r, 
                                    sphereColors[i].colorBegin.g - sphereColors[i].colorStep.g, 
                                    sphereColors[i].colorBegin.b - sphereColors[i].colorStep.b));
            }
            newMat.color = new Color(sphereColors[i].lastColor.r + sphereColors[i].colorStep.r * sphereColors[i].stepSign, 
                                    sphereColors[i].lastColor.g + sphereColors[i].colorStep.g * sphereColors[i].stepSign, 
                                    sphereColors[i].lastColor.b + sphereColors[i].colorStep.b * sphereColors[i].stepSign);
            sphereColors[i] = new SphereColors(sphereColors[i], newMat.color);
            return newMat;
        }
        public void SpawnSphere(int i, Vector3 pos, float size = 0.5f) {
            GameObject sp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Transform spTransform = sp.GetComponent<Transform>();
            spTransform.position = pos;
            spTransform.localScale = new Vector3(size, size, size);
            sp.transform.parent = emptyObjects[i].transform;
            GameObject.DestroyImmediate(sp.GetComponent<SphereCollider>());
            MeshRenderer mr = sp.GetComponent<MeshRenderer>();
            mr.material = CreateMaterial(i);
            spheres[i].Add(sp);
            sp.name = "Sphere" + spheres[i].Count;
        }
    }
}
