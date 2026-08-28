using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class GameplayTimeCoordinator :
        MonoBehaviour,
        IGameplayModule,
        IGameplayTimeService
    {
        private readonly Dictionary<object, GameplayTimeModifier> modifiers = new();

        private float baselineTimeScale = 1f;
        private float baselineFixedDeltaTime = 0.02f;

        public bool IsInitialized { get; private set; }
        public float EffectiveTimeScale { get; private set; } = 1f;

        private void OnDisable() { Shutdown(); }

        public void Initialize() {
            if (IsInitialized) {
                return;
            }

            baselineTimeScale = Time.timeScale;
            baselineFixedDeltaTime = Time.fixedDeltaTime;
            modifiers.Clear();
            IsInitialized = true;
            ApplyResolvedTime();
        }

        public void Shutdown() {
            if (!IsInitialized) {
                return;
            }

            modifiers.Clear();
            Time.timeScale = baselineTimeScale;
            Time.fixedDeltaTime = baselineFixedDeltaTime;
            EffectiveTimeScale = baselineTimeScale;
            IsInitialized = false;
        }

        public void SetModifier(
            object owner,
            GameplayTimeModifier modifier
        ) {
            if (!IsInitialized) {
                throw new InvalidOperationException(
                    "Gameplay time cannot accept modifiers before initialization.");
            }

            if (owner == null) {
                throw new ArgumentNullException(paramName: nameof(owner));
            }

            modifiers[owner] = modifier;
            ApplyResolvedTime();
        }

        public bool RemoveModifier(
            object owner
        ) {
            if (!IsInitialized || owner == null || !modifiers.Remove(owner)) {
                return false;
            }

            ApplyResolvedTime();
            return true;
        }

        private void ApplyResolvedTime() {
            EffectiveTimeScale = GameplayTimeModifierResolver.Resolve(
                baselineScale: baselineTimeScale,
                modifiers: modifiers.Values);
            
            Time.timeScale = EffectiveTimeScale;
            Time.fixedDeltaTime = EffectiveTimeScale > 0f && baselineTimeScale > 0f
                ? baselineFixedDeltaTime * (EffectiveTimeScale / baselineTimeScale)
                : baselineFixedDeltaTime;
        }
    }
}