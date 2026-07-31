using System.Collections.Generic;
using IdleTower.Core.Events;
using IdleTower.Data.Definitions;
using IdleTower.Rooms.Production;
using UnityEngine;

namespace IdleTower.Audio
{
    /// <summary>
    /// Presenter звука: факты GameEvents → SfxId → клип.
    /// Масштаб: новый SFX = значение в SfxId + (опционально) клип в bindings + одна подписка ниже, если новый факт.
    /// Стартовый unlock режима при постройке не Raise'ит OperationModeUnlocked — звука не будет.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private SfxBinding[] bindings;
        [SerializeField] private AudioSource audioSource;

        private readonly Dictionary<SfxId, AudioClip> _clips = new();
        private readonly Dictionary<SfxId, AudioClip> _fallbackClips = new();
        private bool _subscribed;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            RebuildClipLookup();
        }

        private void OnValidate()
        {
            RebuildClipLookup();
        }

        private void OnEnable()
        {
            if (_subscribed)
                return;

            // Факт → SfxId. Добавляя десятки звуков, держите маппинг здесь компактно.
            GameEvents.RoomBuilt += OnRoomBuilt;
            GameEvents.ProductionModeChanged += OnProductionModeChanged;
            GameEvents.OperationModeUnlocked += OnOperationModeUnlocked;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
                return;

            GameEvents.RoomBuilt -= OnRoomBuilt;
            GameEvents.ProductionModeChanged -= OnProductionModeChanged;
            GameEvents.OperationModeUnlocked -= OnOperationModeUnlocked;
            _subscribed = false;
        }

        public void Play(SfxId id)
        {
            if (audioSource == null)
                return;

            var clip = ResolveClip(id);
            if (clip == null)
                return;

            audioSource.PlayOneShot(clip);
        }

        private void OnRoomBuilt(int roomIndex, RoomDefinition room)
            => Play(SfxId.RoomBuilt);

        private void OnProductionModeChanged(int roomIndex, ModeId modeId)
            => Play(SfxId.ModeChanged);

        private void OnOperationModeUnlocked(int roomIndex, ModeId modeId)
            => Play(SfxId.ModeUnlocked);

        private AudioClip ResolveClip(SfxId id)
        {
            if (_clips.TryGetValue(id, out var clip) && clip != null)
                return clip;

            if (_fallbackClips.TryGetValue(id, out var fallback) && fallback != null)
                return fallback;

            fallback = ProceduralSfx.Create(id);
            _fallbackClips[id] = fallback;
            return fallback;
        }

        private void RebuildClipLookup()
        {
            _clips.Clear();
            if (bindings == null)
                return;

            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding.Clip == null)
                    continue;

                _clips[binding.Id] = binding.Clip;
            }
        }
    }
}
