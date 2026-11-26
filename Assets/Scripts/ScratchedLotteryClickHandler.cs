using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScratchedLotteryClickHandler : MonoBehaviour, IPointerClickHandler
{
    private readonly List<OpenedSymbolChance> symbolProbabilities = new List<OpenedSymbolChance>();

    public IReadOnlyList<OpenedSymbolChance> SymbolProbabilities => symbolProbabilities;

    public void SetSymbolProbabilities(IEnumerable<OpenedSymbolChance> probabilities)
    {
        symbolProbabilities.Clear();
        if (probabilities != null)
        {
            symbolProbabilities.AddRange(probabilities);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging)
            return;

        GameObject targetObj = gameObject;
        Lottery lottery = targetObj.GetComponent<Lottery>();
        if (lottery == null)
            lottery = GetComponentInParent<Lottery>();

        if (lottery != null)
        {
            lottery.EvaluateOpenedSlots();
            SetSymbolProbabilities(lottery.GetCurrentOpenedSymbolsWithProbability());
            targetObj = lottery.gameObject;
        }
        else if (targetObj.GetComponent<IInspectableSource>() == null)
        {
            IInspectableSource parentSource = GetComponentInParent<IInspectableSource>();
            if (parentSource != null)
                targetObj = ((Component)parentSource).gameObject;
        }

        ScratchBot bot = GetComponentInParent<ScratchBot>();
        if (bot != null && bot.TryRevealScratchedLottery(targetObj))
            return;

        ScratchBot.TryRevealFromAnyBot(targetObj);
    }
}