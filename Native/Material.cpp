#include "device.h"
#include "material.h"
#include "helpers.h"
#pragma once

// Shaders
LPDIRECT3DPIXELSHADER9 pixelShader = NULL;
LPDIRECT3DVERTEXSHADER9 vertexShader = NULL;


// Check and create the shaders if it's required
bool EnsureShaders()
{
	LPDIRECT3DDEVICE9 device = GetDevice();
	if (device == NULL) return false;

	// Temp variables
	void* pFunction = NULL;
	int lenght = 0;

	if (vertexShader == NULL)
	{
		LoadFileData("Shaders\\BlinPhong.vs", &pFunction, &lenght);
		if (pFunction == NULL) return false;

		device->CreateVertexShader((DWORD*)pFunction, &vertexShader);
		free(pFunction);
	}

	if (pixelShader == NULL)
	{
		pFunction = NULL;
	    lenght = 0;

		LoadFileData("Shaders\\BlinPhong.ps", &pFunction, &lenght);
		if (pFunction == NULL) return false;

		device->CreatePixelShader((DWORD*)pFunction, &pixelShader);
		free(pFunction);
	}

	return (pixelShader != NULL) && (vertexShader != NULL);
}

// Constructor
Material::Material()
{
	
}

// Destructor
Material::~Material()
{
    
}

// Apply (upload parameters & set shaders to the device, 
// invokation of this fuction must be minimized)
void Material::Apply()
{
	LPDIRECT3DDEVICE9 device = GetDevice();
	if (device == NULL || !EnsureShaders()) return;

	device->SetVertexShader(vertexShader);
	device->SetPixelShader(pixelShader);

	// Colors
	D3DCOLORVALUE ambient =  D3DCOLORTOD3DCOLORVALUE(Ambient);
	device->SetVertexShaderConstantF(0, (float*)&ambient, 1);
	D3DCOLORVALUE diffuse =  D3DCOLORTOD3DCOLORVALUE(Diffuse);
	device->SetVertexShaderConstantF(1, (float*)&diffuse, 1);
	D3DCOLORVALUE specular =  D3DCOLORTOD3DCOLORVALUE(Specular);
	device->SetVertexShaderConstantF(2, (float*)&specular, 1);
	D3DCOLORVALUE emissive =  D3DCOLORTOD3DCOLORVALUE(Emissive);
    device->SetVertexShaderConstantF(3, (float*)&emissive, 1);

	// Other parameters
	float* parameters = new float[8];
	parameters[0] = max(300.0f - Glossiness * 300.0f, 1.0f);
    parameters[1] = SpecularPower;
    parameters[2] = ReflectionLevel;
    parameters[3] = BumpLevel;
    parameters[4] = EmissiveLevel;
	device->SetVertexShaderConstantF(4, parameters, 2);
	delete [] parameters;

	// TODO: optimize it, remember last setted parameters to skip 
	// their upload, but it must be optional coz GPU mem may be overriden...
}

// Set matrices (upload matrices to the device, 
// invokation of this fuction must be minimized)
void Material::SetMatrices(D3DMATRIX* world, D3DMATRIX* view, D3DMATRIX* projection)
{
	LPDIRECT3DDEVICE9 device = GetDevice();
	if (device == NULL || !EnsureShaders()) return;

	// FIXME: possible bug here
	D3DMATRIX worldViewProjection = Multiply(Multiply(*world, *view), *projection);

	device->SetVertexShaderConstantF(6, (float*)&worldViewProjection, 4);
	device->SetVertexShaderConstantF(10, (float*)world, 4);
}

// Set view & light directions (invokation of this fuction must be minimized)
void Material::SetViewLightDirection(D3DVECTOR* viewDirection, D3DVECTOR* lightDirection)
{
	LPDIRECT3DDEVICE9 device = GetDevice();
	if (device == NULL || !EnsureShaders()) return;
		
	float* parameters = new float[8];
	parameters[0] = viewDirection->x;
	parameters[1] = viewDirection->y;
	parameters[2] = viewDirection->z;
	parameters[4] = lightDirection->x;
	parameters[5] = lightDirection->y;
	parameters[6] = lightDirection->z;

	device->SetVertexShaderConstantF(14, parameters, 2);
	delete [] parameters;
}