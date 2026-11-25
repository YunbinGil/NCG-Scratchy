using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LotteryData_New", menuName = "Scritchy Scratchy/Lottery Data")]
public class LotteryData : ScriptableObject
{
    [Header("기본 정보")]
    public int id = 1;
    public string lotteryName = "Scratchy Scratchy";
    [TextArea(2, 3)]
    public string description = "A scratch-off lottery ticket with a chance to win big! Scratch the surface to reveal symbols and see if you match enough to claim your prize.";

    [Header("스프라이트 (2D)")]
    public Sprite tableSprite;
    public Sprite inspectSprite;
    public Sprite scratchCoverSprite;
    public GameObject lotteryPrefab;

    [Header("복권 규칙")]
    [Range(1, 10)] public int scratchCount = 3;
    [Range(1, 10)] public int requiredMatchCount = 2;
    [Min(0)] public int hardness = 0;

    [Header("등장 아이콘 목록")]
    public List<LotteryIconData> availableIcons = new List<LotteryIconData>();
}