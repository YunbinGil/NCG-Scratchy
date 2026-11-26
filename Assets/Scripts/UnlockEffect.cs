using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class UnlockEffect : MonoBehaviour
{
    private enum Playback { None, Automatic, Confirm }

    public int unlockLevel;
    [SerializeField] private Playback playback;
    [SerializeField] private UnlockLotteryCatalog effectPrefab;
    [SerializeField] private Texture2D lightTexture;
    [SerializeField] private string confirmationText = "Catalog Unlocked!";
    [SerializeField] private string confirmationKey = "unlock.catalog.complete";
    [SerializeField] private UnlockEffect unlockWith;

    [Header("Standalone Object")]
    [SerializeField] private bool registerOnStart;
    [SerializeField] private UnityEvent onUnlocked = new UnityEvent();

    private void Start()
    {
        if (!registerOnStart)
            return;

        GameManager player = FindFirstObjectByType<GameManager>();
        if (player != null)
            player.UnlockEffects.Watch(this, this, onUnlocked.Invoke);
    }

    internal UnlockEffect UnlockWith => unlockWith;
    internal bool RequiresConfirmation => playback == Playback.Confirm && effectPrefab != null;
    internal bool MeetsLevel(int playerLevel) => playerLevel >= unlockLevel;

    internal sealed class Registration
    {
        internal UnlockEffect Effect;
        internal MonoBehaviour Owner;
        internal Func<bool> Ready;
        internal Func<bool> AlreadyUnlocked;
        internal Action Complete;
        internal bool CompleteAfterVisual;
        internal bool Cancelled;

        internal void Cancel()
        {
            Cancelled = true;
        }
    }

    internal sealed class Scheduler
    {
        private readonly GameManager player;
        private readonly List<Registration> pending = new List<Registration>();
        private readonly List<UnlockEffect> automaticEffects = new List<UnlockEffect>();
        private readonly List<(Registration Request, UnlockLotteryCatalog Visual)> activeReveals =
            new List<(Registration, UnlockLotteryCatalog)>();
        private UnlockLotteryCatalog automaticPrefab;
        private GameObject automaticVisual;
        private int suppressedFrame = -1;

        internal Scheduler(GameManager player)
        {
            this.player = player;
        }

        internal Registration Watch(
            MonoBehaviour owner, UnlockEffect effect, Action complete,
            Func<bool> ready = null, Func<bool> alreadyUnlocked = null,
            bool completeAfterVisual = false)
        {
            var request = new Registration
            {
                Owner = owner,
                Effect = effect,
                Complete = complete,
                Ready = ready,
                AlreadyUnlocked = alreadyUnlocked,
                CompleteAfterVisual = completeAfterVisual
            };
            pending.Add(request);
            return request;
        }

        internal void SuppressAutomaticOnRestore()
        {
            suppressedFrame = Time.frameCount;
            automaticEffects.Clear();
            if (automaticVisual != null)
                UnityEngine.Object.Destroy(automaticVisual);
        }

        internal void Tick()
        {
            for (int i = activeReveals.Count - 1; i >= 0; i--)
            {
                var (request, visual) = activeReveals[i];
                if (IsFinished(request))
                {
                    if (visual != null)
                        UnityEngine.Object.Destroy(visual.gameObject);
                    activeReveals.RemoveAt(i);
                }
                else if (visual == null)
                {
                    activeReveals.RemoveAt(i);
                    pending.Add(request);
                }
            }

            for (int i = 0; i < pending.Count;)
            {
                Registration request = pending[i];
                if (IsFinished(request))
                {
                    pending.RemoveAt(i);
                    continue;
                }

                UnlockEffect effect = request.Effect;
                if ((effect != null && !effect.MeetsLevel(player.Level)) ||
                    (request.Ready != null && !request.Ready()))
                {
                    i++;
                    continue;
                }

                bool confirm = effect != null && effect.RequiresConfirmation;
                bool playBeforeCompleting = request.CompleteAfterVisual && effect != null &&
                    effect.playback == Playback.Automatic && effect.effectPrefab != null;
                if (confirm || playBeforeCompleting)
                {
                    bool blockedByReveal = activeReveals.Count > 0 &&
                        (confirm || activeReveals.Exists(active => !active.Visual.IsAwaitingConfirmation));
                    if (blockedByReveal || automaticVisual != null || !StartReveal(request, confirm))
                    {
                        i++;
                        continue;
                    }
                    pending.RemoveAt(i);
                    continue;
                }

                pending.RemoveAt(i);
                request.Complete();
                if (effect != null && effect.playback == Playback.Automatic &&
                    effect.effectPrefab != null && suppressedFrame != Time.frameCount &&
                    (automaticVisual == null || effect.effectPrefab != automaticPrefab) &&
                    !automaticEffects.Exists(queued => queued != null && queued.effectPrefab == effect.effectPrefab))
                    automaticEffects.Add(effect);
            }

            if (activeReveals.Count == 0)
                PlayNextAutomatic();
        }

        private static bool IsFinished(Registration request)
        {
            return request.Cancelled || request.Owner == null ||
                   (request.AlreadyUnlocked != null && request.AlreadyUnlocked());
        }

        private bool StartReveal(Registration request, bool confirm)
        {
            UnlockEffect effect = request.Effect;
            UnlockLotteryCatalog visual;
            if (confirm)
            {
                ItemSpawner spawner = player.ItemSpawner;
                RectTransform table = player.TableEffectParent;
                if (spawner == null || !spawner.isActiveAndEnabled || table == null ||
                    !table.gameObject.activeInHierarchy ||
                    !spawner.TrySpawnItemAtTableCenter(effect.effectPrefab.gameObject, out GameObject spawnedObject))
                    return false;

                visual = spawnedObject.GetComponent<UnlockLotteryCatalog>();
            }
            else
            {
                visual = CreateVisual(effect);
                if (visual == null)
                    return false;
            }

            activeReveals.Add((request, visual));
            void CompleteReveal()
            {
                activeReveals.Remove((request, visual));
                if (!IsFinished(request))
                    request.Complete();
            }

            if (confirm)
                visual.Initialize(effect.lightTexture, CompleteReveal, effect.confirmationText, effect.confirmationKey);
            else
                visual.PlayVisualOnly(effect.lightTexture, CompleteReveal);
            return true;
        }

        private void PlayNextAutomatic()
        {
            if (automaticVisual != null)
                return;

            automaticPrefab = null;
            while (automaticEffects.Count > 0)
            {
                UnlockEffect effect = automaticEffects[0];
                automaticEffects.RemoveAt(0);
                if (effect == null)
                    continue;

                UnlockLotteryCatalog visual = CreateVisual(effect);
                if (visual == null)
                    continue;

                automaticPrefab = effect.effectPrefab;
                automaticVisual = visual.gameObject;
                visual.PlayVisualOnly(effect.lightTexture);
                break;
            }
        }

        private UnlockLotteryCatalog CreateVisual(UnlockEffect effect)
        {
            RectTransform table = player.TableEffectParent;
            if (table == null || !table.gameObject.activeInHierarchy)
                return null;

            UnlockLotteryCatalog visual = UnityEngine.Object.Instantiate(effect.effectPrefab, table, false);
            GameAudio.RegisterButtons(visual.gameObject);
            RectTransform rect = (RectTransform)visual.transform;
            rect.position += table.TransformPoint(table.rect.center) - rect.TransformPoint(rect.rect.center);
            rect.SetAsLastSibling();
            return visual;
        }
    }
}
