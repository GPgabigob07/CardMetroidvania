#ifndef TIC_ENEMY_HIT_RIPPLE_INCLUDED
#define TIC_ENEMY_HIT_RIPPLE_INCLUDED

// Identical layout in lit, normals and forward passes.
CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    float _RippleCount;
    float _RipplePixelsPerUnit;
    float4 _RipplePixelGridOrigin;
    float4x4 _RippleVisualToOwner;
    float4x4 _RippleOwnerToVisual;
    float4 _RippleOriginRadius0, _RippleOriginRadius1, _RippleOriginRadius2;
    half4 _RippleLeadingColor0, _RippleLeadingColor1, _RippleLeadingColor2;
    half4 _RippleTrailingColor0, _RippleTrailingColor1, _RippleTrailingColor2;
    float _RippleStrength0, _RippleStrength1, _RippleStrength2;
CBUFFER_END

void EvaluateRipple(float2 position, float4 wave, half3 leading, half3 trailing,
    float strength, inout float bestStrength, inout half3 bestColor)
{
    float2 delta = position - wave.xy;
    float distance = length(delta);
    float2 direction = distance > 0.00001 ? delta / distance : float2(1, 0);
    // Convert four art pixels into radial owner-space width, including nonuniform child scale.
    float width = wave.w / max(length(mul(_RippleOwnerToVisual, float4(direction, 0, 0)).xy), 0.00001);
    float mask = distance <= wave.z && distance >= max(0, wave.z - width) ? 1 : 0;
    float weight = saturate(strength) * mask;
    if (weight > 0 && weight >= bestStrength)
    {
        bestStrength = weight;
        bestColor = lerp(trailing, leading, saturate((distance - wave.z + width) / max(width, 0.00001)));
    }
}

half4 ApplyRipple(half4 baseColor, float2 renderedPositionOS)
{
    if (_RippleCount < 0.5) return baseColor;
    float ppu = max(_RipplePixelsPerUnit, 0.00001);
    float2 gridOrigin = _RipplePixelGridOrigin.xy;
    float2 pixelCenter = (floor((renderedPositionOS - gridOrigin) * ppu) + 0.5) / ppu + gridOrigin;
    float2 ownerPosition = mul(_RippleVisualToOwner, float4(pixelCenter, 0, 1)).xy;
    float strength = 0;
    half3 color = 0;
    if (_RippleCount > 0.5) EvaluateRipple(ownerPosition, _RippleOriginRadius0, _RippleLeadingColor0.rgb, _RippleTrailingColor0.rgb, _RippleStrength0, strength, color);
    if (_RippleCount > 1.5) EvaluateRipple(ownerPosition, _RippleOriginRadius1, _RippleLeadingColor1.rgb, _RippleTrailingColor1.rgb, _RippleStrength1, strength, color);
    if (_RippleCount > 2.5) EvaluateRipple(ownerPosition, _RippleOriginRadius2, _RippleLeadingColor2.rgb, _RippleTrailingColor2.rgb, _RippleStrength2, strength, color);
    // Blend after lighting for legible highlights in dark rooms; retain original coverage.
    baseColor.rgb = lerp(baseColor.rgb, color, strength);
    return baseColor;
}
#endif
