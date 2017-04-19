
struct VS_INPUT
{
  float4 pos : POSITION;
  int4 Index : BONE_INDEX;
  float4 Weight : BONE_WEIGHT;
  float2 tex : TEXCOORD0;
};

// 頂点シェーダ
float4x4 g_viewProjectionMatrix : VIEWPROJECTION;
Texture2D boneTex : BONE_TEXTURE;
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


struct VS_OUTPUT
{
  float4 oPos : SV_POSITION;
  float2 oUV : TEXCOORD0;
};

VS_OUTPUT VS(VS_INPUT input)
{
  const float4 inPos = input.pos;
  float4 pos = mul(inPos, GetBoneMatrix(input.Index.x)) * input.Weight.x;
  pos += mul(inPos, GetBoneMatrix(input.Index.y)) * input.Weight.y;
  pos += mul(inPos, GetBoneMatrix(input.Index.z)) * input.Weight.z;
  pos += mul(inPos, GetBoneMatrix(input.Index.w)) * input.Weight.w;

  VS_OUTPUT Out;
  Out.oPos = mul(pos, g_viewProjectionMatrix);
  Out.oUV = input.tex;
  return Out;
}

// ピクセルシェーダ

float4 g_diffuseColor : DIFFUSE;
Texture2D MaterialTexture : MATERIALTEXTURE;

SamplerState TextureSamp
{
  Filter = MIN_MAG_MIP_LINEAR;
  AddressU = Wrap;
  AddressV = Wrap;
};
void PS(in float4 pos : SV_POSITION, in float2 uv : TEXCOORD0, out float4 oColor : SV_Target0, uniform bool useTexute)
{
  float4 color = g_diffuseColor;
  
  if (useTexute)
  {
    color *= MaterialTexture.Sample(TextureSamp, uv);
  }
  oColor = color;
}
BlendState Blend
{
  AlphaToCoverageEnable = True;
  BlendEnable[0] = True;
  SrcBlend[0] = SRC_ALPHA;
  DestBlend[0] = INV_SRC_ALPHA;
  BlendOp[0] = ADD;
  SrcBlendAlpha[0] = ONE;
  DestBlendAlpha[0] = ONE;
  BlendOpAlpha[0] = MAX;

};

technique11 WithTex <
  bool UseTexture = true;
>
{
  pass P0
  {
  
    SetBlendState(Blend, float4(0.0f, 0.0f, 0.0f, 0.0f), 0xFFFFFFFF);

    SetVertexShader(CompileShader(vs_4_0, VS()));
    SetGeometryShader(NULL);
    SetPixelShader(CompileShader(ps_4_0, PS(true)));
  }
}


technique11 WithoutTex <
  bool UseTexture = false;
>
{
  pass P0
  {
    SetVertexShader(CompileShader(vs_4_0, VS()));
    SetGeometryShader(NULL);
    SetPixelShader(CompileShader(ps_4_0, PS(false)));
  }
}

