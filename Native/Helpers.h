#include <windows.h>
#include "device.h"

#pragma once

// Converts D3DCOLOR to D3DCOLORVALUE
D3DCOLORVALUE D3DCOLORTOD3DCOLORVALUE(D3DCOLOR& color);

// Load all data from the file
void LoadFileData(const char* path, void** data, int* length);

// 4x4 matrix multiplication
D3DMATRIX Multiply(const D3DMATRIX& a, const D3DMATRIX& b);

// Creates the matrix
D3DMATRIX D3DMATRIXCREATE(	
	float _11, float _12, float _13, float _14,
    float _21, float _22, float _23, float _24,
    float _31, float _32, float _33, float _34,
    float _41, float _42, float _43, float _44);

// Appends transform along to the given direction 
// (the object must have original direction along YAxis(!))
D3DMATRIX TransformAlongTo(D3DVECTOR direction);

// Gets lenght of the given vector
float GetLength(D3DVECTOR& a);

// Normalizes the given vector
void Normalize(D3DVECTOR& a);

// Produces cross product
D3DVECTOR CrossProduct(D3DVECTOR& a, D3DVECTOR& b);