using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScrubberData_New", menuName = "Scritchy Scratchy/Scrubber Data")]
public class ScrubberData : ScriptableObject
{
    [Header("기본 정보")]
    public int id = 1;
    public string scrubberName = "Standard Scrubber";

    [Header("스프라이트 (2D)")]
    [Tooltip("기본 상태 스프라이트")]
    public Sprite inspectSprite;        // 기본 상태 표시용 

    [Header("세정 및 확대뷰 보조 수치")]
    [Tooltip("한 번 닦을 때 지워지는 반경/범위")]
    [Min(0.01f)]
    public float cleanRadius = 0.5f;
    [Tooltip("한 번 닦을 때 줄어드는 오염도/강도")]
    [Min(0.01f)]
    public float cleanPower = 1f;

    public Sprite GetSprite(bool isDragging)
    {
        return inspectSprite;
    }
}