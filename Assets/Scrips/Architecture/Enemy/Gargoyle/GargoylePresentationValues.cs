using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public readonly struct GargoylePresentationValues
    {
        public GargoylePresentationValues(GargoylePresentationSO source)
        {
            BodyRegion = source.BodyRegion; CoreRegion = source.CoreRegion; HeadRegion = source.HeadRegion;
            FrameRate = source.FrameRate; FlashDuration = source.FlashDuration; TelegraphLineWidth = source.TelegraphLineWidth;
            NormalColor = source.NormalColor; WindupColor = source.WindupColor; ActiveColor = source.ActiveColor;
            ReactorColor = source.ReactorColor; StunColor = source.StunColor; HitColor = source.HitColor;
            IdleSprite = source.IdleSprite; HitVfxPrefab = source.HitVfxPrefab; AttackAudio = source.AttackAudio;
            var bindings = new List<GargoyleAnimationBinding>();
            foreach (var binding in source.AnimationBindings) if (binding != null) bindings.Add(binding.Copy());
            AnimationBindings = bindings.AsReadOnly();
        }

        public GargoyleRegionSettings BodyRegion { get; }
        public GargoyleRegionSettings CoreRegion { get; }
        public GargoyleRegionSettings HeadRegion { get; }
        public float FrameRate { get; }
        public float FlashDuration { get; }
        public float TelegraphLineWidth { get; }
        public Color NormalColor { get; }
        public Color WindupColor { get; }
        public Color ActiveColor { get; }
        public Color ReactorColor { get; }
        public Color StunColor { get; }
        public Color HitColor { get; }
        public Sprite IdleSprite { get; }
        public GameObject HitVfxPrefab { get; }
        public AudioClip AttackAudio { get; }
        public IReadOnlyList<GargoyleAnimationBinding> AnimationBindings { get; }
    }
}
