
struct VS_INPUT
{
  float4 pos : POSITION;
  int4 Index : BONE_INDEX;
  float Weight : BONE_WEIGHT;

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
matrix GetBoneMatrix(int index)
{
  int4 vPos = int4(index * 4, 0, 0, 0);
  matrix res;
  res[0] = boneTex.Load(vPos.xyz); vPos.x++;
  res[1] = boneTex.Load(vPos.xyz); vPos.x++;
  res[2] = boneTex.Load(vPos.xyz); vPos.x++;
  res[3] = boneTex.Load(vPos.xyz);
  return res;
}
VS_OUTPUT VS(VS_INPUT input)
{
  const float4 pos = input.pos;
  matrix comb = GetBoneMatrix(input.Index.x) * input.Weight;
  comb += GetBoneMatrix(input.Index.y) * (1.0f - input.Weight);

  VS_OUTPUT Out; 
  Out.pos = mul(mul(pos, comb), g_viewProjectionMatrix);
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
