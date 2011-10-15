struct VS_INPUT
{    
    float2 Texture0    : TEXCOORD0;
    float2 Texture1    : TEXCOORD1;
    float2 Position   : POSITION;
};

struct VS_OUTPUT
{    
    float2 Texture0    : TEXCOORD0;
    float2 Texture1    : TEXCOORD1;
    float4 Position   : POSITION;
};

// Simple Vertex Shader
VS_OUTPUT main(in VS_INPUT input)
{
	VS_OUTPUT output;
	output.Position = float4(input.Position, 0, 1);
	output.Texture0 = input.Texture0;
	output.Texture1 = input.Texture1;
    return output;
}