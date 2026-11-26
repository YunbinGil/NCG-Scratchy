using System;
using UnityEngine;

[Serializable]
public class CatalogData
{
    public GameObject catalogPrefab;
    public GameObject unlockImagePrefab;

    [Min(0)]
    public int unlockLevel;
}