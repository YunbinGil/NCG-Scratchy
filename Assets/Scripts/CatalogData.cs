using System;
using UnityEngine;

[Serializable]
public class CatalogData
{
    public GameObject catalogPrefab;

    [Min(0)]
    public int unlockLevel;
}