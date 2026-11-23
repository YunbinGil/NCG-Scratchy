using UnityEngine;

[CreateAssetMenu(fileName = "LotteryIcon_New", menuName = "Scritchy Scratchy/Lottery Icon Data")]
public class LotteryIconData : ScriptableObject
{
    public int id = 1;
    public string iconName = "Symbol";
    public Sprite iconSprite;
    [Min(0f)] public float spawnWeight = 50f;
    public int[] matchPayouts = new int[5] { 0, 10, 25, 50, 100 };

    public int GetPayoutForMatchCount(int matchCount)
    {
        if (matchPayouts == null || matchCount < 1 || matchCount > matchPayouts.Length)
            return 0;
        return matchPayouts[matchCount - 1];
    }
}