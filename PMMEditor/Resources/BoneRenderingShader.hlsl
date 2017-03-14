
struct VS_INPUT
{
  int2 BoneIndex : BONE_INDEX;
  int IsBegin : IS_BEGIN;
};

struct VS_OUTPUT
{
  float4 pos : SV_POSITION;
};

struct PS_OUTPUT
{
  float4 color : SV_Target0;
};

// 頂点シェーダ
cbuffer vscbMesh0 : register(b0)
{
  float4x4 g_viewProjectionMatrix;
}
Texture2D boneTex : register(t0);
float4 GetBonePosition(int2 index)
{
  int3 vPos = int3(index, 0);
  vPos.x += 3;
  return boneTex.Load(vPos);
}
VS_OUTPUT VS(VS_INPUT input)
{
  float4 bonePos = GetBonePosition(input.BoneIndex);
  VS_OUTPUT Out;
  Out.pos = mul(bonePos, g_viewProjectionMatrix);
  return Out;
}

// ピクセルシェーダ

PS_OUTPUT PS(VS_OUTPUT input)
{
  PS_OUTPUT res;
  res.color = float4(1,0,0,1);
  return res;
}
