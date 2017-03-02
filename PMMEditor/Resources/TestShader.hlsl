
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

cbuffer vscbMesh0 : register(b0)
{
  float4x4 g_viewProjectionMatrix;
}

// 頂点シェーダ
VS_OUTPUT VS(VS_INPUT input)
{

  const float4 pos = input.pos;

  VS_OUTPUT Out;
  Out.pos = mul(pos, g_viewProjectionMatrix);
  return Out;
}

// 頂点シェーダ
PS_OUTPUT PS(VS_OUTPUT input)
{
  PS_OUTPUT res;
  res.color = float4(1, 0, 0, 1);
  return res;
}
