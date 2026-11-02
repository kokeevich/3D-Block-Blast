using UnityEngine;

[System.Serializable]
public class ShapeInstanceData
{
    public GameObject prefab;
    public Vector3 eulerAngles;
    public string category; // ← добавлено

    public Quaternion GetRotation() => Quaternion.Euler(eulerAngles);
}