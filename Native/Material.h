#include <windows.h>
#include "device.h"

#pragma once

// Represents materials parameters
struct MaterialParameters
{
	// Colors
	public:
	
};

// Represents a material object
struct Material
{
    public:
 
	// WARNING: sequence & count of the following fields are important!
	// Interop will be broken if it will be changed!
    D3DCOLOR Ambient;
    D3DCOLOR Diffuse;
    D3DCOLOR Specular;
    D3DCOLOR Emissive;

    // Parameters
    FLOAT Glossiness;
    FLOAT SpecularPower;
    FLOAT ReflectionLevel;
    FLOAT BumpLevel;
    FLOAT EmissiveLevel;

    // Constructor
    Material();
    // Destructor
    ~Material();
	
	// Apply (upload parameters & set shaders to the device, 
	// invokation of this fuction must be minimized)
	void Apply();

	// Set matrices (upload matrices to the device, 
	// invokation of this fuction must be minimized)
	void SetMatrices(D3DMATRIX* world, D3DMATRIX* view, D3DMATRIX* projection);
		
	// Set view & light directions (invokation of this fuction must be minimized)
	void SetViewLightDirection(D3DVECTOR* viewDirection, D3DVECTOR* lightDirection);
};

