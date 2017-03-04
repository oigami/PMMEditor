
struct VS_INPUT
{
  float4 pos : POSITION;
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

VS_OUTPUT VS(VS_INPUT input)
{

  const float4 pos = input.pos;

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
