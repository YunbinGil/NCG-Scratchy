using UnityEngine;
using UnityEngine.EventSystems;

public class ScratchedLotteryClickHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging)
            return;

        ScratchBot bot = GetComponentInParent<ScratchBot>();
        if (bot != null)
        {
            bot.TryRevealScratchedLottery(gameObject);
        }
    }
}