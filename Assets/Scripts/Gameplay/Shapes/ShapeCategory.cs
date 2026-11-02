using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShapeCategory
{
    public string name;
    public float baseWeight = 1f;
    public int minCellsNeeded = 1;
    public float previewScale = 0.2f;
    public GameObject[] prefabs;
}

