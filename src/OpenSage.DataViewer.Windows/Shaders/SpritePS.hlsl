#include "Sprite.hlsli"

Texture2D<float4> BaseTexture : register(t0);
SamplerState Sampler : register(s0);

cbuffer MaterialConstants : register(b2)
{
    uint MipMapLevel;
};

float4 main(PSInput input) : SV_TARGET
{
    return BaseTexture.SampleLevel(
        Sampler,
        input.TexCoords, 
        MipMapLevel);
}