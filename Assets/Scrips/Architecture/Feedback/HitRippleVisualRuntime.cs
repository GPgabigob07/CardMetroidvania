using System;
using UnityEngine;
namespace TicGame.Architecture
{
    internal sealed class HitRippleVisualRuntime
    {
        private Transform owner;
 private HitRippleProfileSO profile;
 private SpriteRenderer[] visuals = Array.Empty<SpriteRenderer>();
        private readonly HitRippleRuntime runtime = new();
        private readonly int[] sortedSlots = new int[HitRippleRuntime.Capacity];
        private MaterialPropertyBlock[] blocks = Array.Empty<MaterialPropertyBlock>();
        private static readonly int CountId = Shader.PropertyToID("_RippleCount");
        private static readonly int MatrixId = Shader.PropertyToID("_RippleVisualToOwner");
        private static readonly int InverseMatrixId = Shader.PropertyToID("_RippleOwnerToVisual");
        private static readonly int PpuId = Shader.PropertyToID("_RipplePixelsPerUnit");
        private static readonly int GridId = Shader.PropertyToID("_RipplePixelGridOrigin");
        private static readonly int[] WaveIds = MakeIds("_RippleOriginRadius");
        private static readonly int[] LeadingIds = MakeIds("_RippleLeadingColor");
        private static readonly int[] TrailingIds = MakeIds("_RippleTrailingColor");
        private static readonly int[] StrengthIds = MakeIds("_RippleStrength");
        public int ActiveCount => runtime.ActiveCount;
        private static int[] MakeIds(string prefix) => new[]
        { Shader.PropertyToID(prefix + "0"), Shader.PropertyToID(prefix + "1"), Shader.PropertyToID(prefix + "2") };


        public void Configure(Transform root, HitRippleProfileSO definition, SpriteRenderer[] renderers)
        {
            Clear(); owner = root; profile = definition; visuals = renderers ?? Array.Empty<SpriteRenderer>();
            CreateBlocks(); Upload();
        }
        private void CreateBlocks()
        {
            if (blocks.Length == visuals.Length) return;
            blocks = new MaterialPropertyBlock[visuals.Length];
            for (var index = 0; index < blocks.Length; index++) blocks[index] = new MaterialPropertyBlock();
        }
        public void Present(in DamageContext context, in DamageResult result)
        {
            if (owner == null || profile == null) return;
            var kind = HitRippleOutcome.Classify(result);
            if (kind == HitRippleKind.None) return;
            var origin = (Vector2)owner.InverseTransformPoint(context.HitPoint);
            var distance = 0f;
            var validVisual = false;
            foreach (var visual in visuals)
            {
                if (visual == null || visual.sprite == null) continue;
                validVisual = true;
                var matrix = owner.worldToLocalMatrix * visual.transform.localToWorldMatrix;
                var bounds = visual.sprite.bounds;
                var flip = new Vector2(visual.flipX ? -1 : 1, visual.flipY ? -1 : 1);
                var padding = profile.WidthPixels / visual.sprite.pixelsPerUnit * Mathf.Max(
                    matrix.MultiplyVector(Vector3.right).magnitude, matrix.MultiplyVector(Vector3.up).magnitude);
                for (var corner = 0; corner < 4; corner++)
                {
                    var local = new Vector3((corner % 2 == 0 ? bounds.min.x : bounds.max.x) * flip.x,
                        (corner < 2 ? bounds.min.y : bounds.max.y) * flip.y, 0);
                    var point = (Vector2)matrix.MultiplyPoint3x4(local);
                    distance = Mathf.Max(distance, Vector2.Distance(origin, point) + padding);
                }
            }
            if (!validVisual) return;
            runtime.Add(origin, kind, distance, profile.TraversalSeconds, profile.WidthPixels);
            Upload();
        }
        public void Tick(float unscaledDeltaTime, bool paused)
        {
            runtime.Tick(unscaledDeltaTime, paused);
            Upload();
        }
        public void Clear() { runtime.Clear(); Upload(); }

        private void Upload()
        {
            CreateBlocks();
            var count = 0;
            for (var slot = 0; slot < HitRippleRuntime.Capacity; slot++)
            {
                if (!runtime.GetWave(slot).IsActive) continue;
                var index = count++;
                while (index > 0 && runtime.GetWave(sortedSlots[index - 1]).StartSequence > runtime.GetWave(slot).StartSequence)
                { sortedSlots[index] = sortedSlots[index - 1]; index--; }
                sortedSlots[index] = slot;
            }
            for (var index = 0; index < visuals.Length; index++)
            {
                var visual = visuals[index];
                if (visual == null) continue;
                var block = blocks[index]; visual.GetPropertyBlock(block);
                var sprite = visual.sprite;
                var hasVisual = sprite != null && owner != null && profile != null;
                block.SetFloat(CountId, hasVisual ? count : 0);
                if (hasVisual)
                {
                    var matrix = owner.worldToLocalMatrix * visual.transform.localToWorldMatrix;
                    block.SetMatrix(MatrixId, matrix); block.SetMatrix(InverseMatrixId, matrix.inverse);
                    block.SetFloat(PpuId, sprite.pixelsPerUnit);
                    var grid = -sprite.pivot / sprite.pixelsPerUnit;
                    if (visual.flipX) grid.x = -grid.x;
                    if (visual.flipY) grid.y = -grid.y;
                    block.SetVector(GridId, new Vector4(grid.x, grid.y, 0, 0));
                }
                for (var waveIndex = 0; waveIndex < HitRippleRuntime.Capacity; waveIndex++)
                {
                    if (!hasVisual || waveIndex >= count)
                    { block.SetFloat(StrengthIds[waveIndex], 0); block.SetVector(WaveIds[waveIndex], Vector4.zero); continue; }
                    var wave = runtime.GetWave(sortedSlots[waveIndex]);
                    var leading = wave.Kind == HitRippleKind.Fatal ? profile.FatalLeadingColor
                        : wave.Kind == HitRippleKind.Rejected ? profile.RejectedColor : profile.BlueColor;
                    var trailing = wave.Kind == HitRippleKind.Fatal ? profile.FatalTrailingColor : leading;
                    var fade = profile.FadeOutFraction;
                    var strength = profile.Strength * (fade > 0 ? Mathf.Clamp01((1 - wave.Progress) / fade) : 1);
                    block.SetVector(WaveIds[waveIndex], new Vector4(wave.Origin.x, wave.Origin.y, wave.Radius, wave.WidthPixels / sprite.pixelsPerUnit));
                    block.SetColor(LeadingIds[waveIndex], leading); block.SetColor(TrailingIds[waveIndex], trailing);
                    block.SetFloat(StrengthIds[waveIndex], strength);
                }
                visual.SetPropertyBlock(block);
            }
        }
    }
}
