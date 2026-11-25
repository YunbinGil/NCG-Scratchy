using UnityEngine;

[CreateAssetMenu(fileName = "TrophyData_New", menuName = "Scritchy Scratchy/Trophy Data")]
public class TrophyData : ScriptableObject
{
    [Header("기본 정보")]
    public int id = 1;
    public string dishName = "Day Job";
    [TextArea(2, 4)]
    public string description = "It may be a pittance, but it’s the reward for my hard work.";

    [Header("스프라이트 (2D)")]
    public Sprite tableSprite;          // 테이블 표시용
    public Sprite inspectSprite;        // 확대뷰 기본/더러운 상태 표시용
    public Sprite cleanSprite;          // 어느 정도 다 닦았을 때 표시용
    public Sprite stainSprite;          // 얼룩 오버레이용
    public Sprite brokenSprite;         // 깨진 상태용

    [Header("경제 및 파손 수치")]
    [Min(0)] public int buyPrice = 1;                   // 상점 구매가격
    [Min(0)] public int sellPriceAfterClean = 3;        // 닦은 후 판매가격
    [SerializeField] private string sellPriceProductId;
    [SerializeField, Min(0)] private int sellPriceBonusPerLevel;
    [Min(0)] public int breakPenalty = 3;               // 파손 시 패널티 금액(
    [Range(0f, 1f)] public float breakChance = 0.1f;    // 접시 파손 확률 (0.1 = 10%)

    [SerializeField, Range(0f, 1f)] private float breakChanceReductionPerLevel;

    [Header("세정 및 확대뷰 보조 수치")]
    [Min(0.1f)] public float baseCleanRequirement = 100f;
    //TODO: Scratchable.cs에서 이 필드 사용하는걸로 변경
    [Range(0.1f, 1f)] public float cleanCompleteRatio = 0.8f; // 닦기 완료 판정 비율
    [Min(0.5f)] public float inspectScale = 1.5f;

    public int GetSellPriceAfterClean()
    {
        if (sellPriceBonusPerLevel <= 0 || string.IsNullOrWhiteSpace(sellPriceProductId))
            return sellPriceAfterClean;

        long price = sellPriceAfterClean + (long)GetProductLevel() * sellPriceBonusPerLevel;
        return (int)System.Math.Min(price, int.MaxValue);
    }

    public float GetBreakChance()
    {
        if (breakChanceReductionPerLevel <= 0f)
            return breakChance;

        return Mathf.Clamp01(breakChance - GetProductLevel() * breakChanceReductionPerLevel);
    }

    private int GetProductLevel()
    {
        if (string.IsNullOrWhiteSpace(sellPriceProductId))
            return 0;

        ProductProgress progress = FindFirstObjectByType<ProductProgress>();
        return progress != null ? progress.GetLevel(sellPriceProductId) : 0;
    }
}
