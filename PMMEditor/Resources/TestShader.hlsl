
struct VS_INPUT
{
  float4 pos : POSITION;
  int4 Index : BONE_INDEX;
  float4 Weight : BONE_WEIGHT;

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
float4x4 GetBoneMatrix(int index)
{
  int4 vPos = int4(index * 4, 0, 0, 0);
  matrix res;
  res[0] = boneTex.Load(vPos.xyz);
  vPos.x++;
  res[1] = boneTex.Load(vPos.xyz);
  vPos.x++;
  res[2] = boneTex.Load(vPos.xyz);
  vPos.x++;
  res[3] = boneTex.Load(vPos.xyz);
  return res;
}
VS_OUTPUT VS(VS_INPUT input)
{
  const float4 inPos = input.pos;
  float4 pos = mul(inPos, GetBoneMatrix(input.Index.x)) * input.Weight.x;
  pos += mul(inPos, GetBoneMatrix(input.Index.y)) * input.Weight.y;
  pos += mul(inPos, GetBoneMatrix(input.Index.z)) * input.Weight.z;
  pos += mul(inPos, GetBoneMatrix(input.Index.w)) * input.Weight.w;

  VS_OUTPUT Out;
  Out.pos = mul(pos, g_viewProjectionMatrix);
  return Out;
}

// ピクセルシェーダ

cbuffer vscbMesh0 : register(b0)
{
  float4 g_diffuseColor;
}

PS_OUTPUT PS(VS_OUTPUT input)
{
  PS_OUTPUT res;
  res.color = g_diffuseColor;
  return res;
}
