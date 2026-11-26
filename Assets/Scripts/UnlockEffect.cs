using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class UnlockEffect : MonoBehaviour
{
    private enum Playback { None, Automatic, Confirm }

    [SerializeField, Min(0)] private int unlockLevel;
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
        private Registration activeConfirmation;
        private UnlockLotteryCatalog confirmationVisual;
        private UnlockLotteryCatalog automaticPrefab;
        private GameObject automaticVisual;
        private int suppressedFrame = -1;

        internal Scheduler(GameManager player)
        {
            this.player = player;
        }

        internal Registration Watch(
            MonoBehaviour owner, UnlockEffect effect, Action complete,
            Func<bool> ready = null, Func<bool> alreadyUnlocked = null)
        {
            var request = new Registration
            {
                Owner = owner,
                Effect = effect,
                Complete = complete,
                Ready = ready,
                AlreadyUnlocked = alreadyUnlocked
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
            if (activeConfirmation != null &&
                (IsFinished(activeConfirmation) || confirmationVisual == null))
            {
                if (confirmationVisual != null)
                    UnityEngine.Object.Destroy(confirmationVisual.gameObject);
                activeConfirmation = null;
                confirmationVisual = null;
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

                if (effect != null && effect.RequiresConfirmation)
                {
                    if (activeConfirmation != null || automaticVisual != null || !StartConfirmation(request))
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

            if (activeConfirmation == null)
                PlayNextAutomatic();
        }

        private static bool IsFinished(Registration request)
        {
            return request.Cancelled || request.Owner == null ||
                   (request.AlreadyUnlocked != null && request.AlreadyUnlocked());
        }

        private bool StartConfirmation(Registration request)
        {
            UnlockEffect effect = request.Effect;
            ItemSpawner spawner = player.ItemSpawner;
            RectTransform table = player.TableEffectParent;
            if (spawner == null || !spawner.isActiveAndEnabled || table == null ||
                !table.gameObject.activeInHierarchy ||
                !spawner.TrySpawnItemAtTableCenter(effect.effectPrefab.gameObject, out GameObject spawnedObject))
                return false;

            confirmationVisual = spawnedObject.GetComponent<UnlockLotteryCatalog>();

            activeConfirmation = request;
            confirmationVisual.Initialize(effect.lightTexture, () =>
            {
                if (!IsFinished(request))
                    request.Complete();
                activeConfirmation = null;
                confirmationVisual = null;
            }, effect.confirmationText, effect.confirmationKey);
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
            RectTransform rect = (RectTransform)visual.transform;
            rect.position += table.TransformPoint(table.rect.center) - rect.TransformPoint(rect.rect.center);
            rect.SetAsLastSibling();
            return visual;
        }
    }
}
