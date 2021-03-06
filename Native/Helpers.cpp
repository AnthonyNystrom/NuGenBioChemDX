#include "device.h"
#include "helpers.h"
#include <stdio.h>
#include <io.h>
#pragma once

struct D3DCOLORHELPERSTRUCT
{
	byte b;
	byte g;
	byte r;
	byte a;
};

// Converts D3DCOLOR to D3DCOLORVALUE
D3DCOLORVALUE D3DCOLORTOD3DCOLORVALUE(D3DCOLOR& color)
{
	D3DCOLORHELPERSTRUCT* helper = (D3DCOLORHELPERSTRUCT*)(&color);
	D3DCOLORVALUE output;
	output.r = (float)helper->r / 255.0;
	output.g = (float)helper->g / 255.0;
	output.b = (float)helper->b / 255.0;
	output.a = (float)helper->a / 255.0;

	return output;
}

// REMOVE IT
#include <direct.h>
    #define GetCurrentDir _getcwd

// Load all data from the file
void LoadFileData(const char* path, void** data, int* length)
{
	char cCurrentPath[20000];

	GetCurrentDir(cCurrentPath, 20000);

	FILE* fileHandler = fopen(path,"rb");
	if (fileHandler == NULL) 
	{
		*data = NULL;
		*length = 0;
		return;
	}

	*length = filelength(fileno(fileHandler));
	*data = malloc((size_t)*length);

	fread(*data, 1, (size_t)*length, fileHandler);
	fclose(fileHandler);
}

// 4x4 matrix multiplication
D3DMATRIX Multiply(const D3DMATRIX &a, const D3DMATRIX &b)
{
    return D3DMATRIXCREATE(
     a._11*b._11 + a._12*b._21 + a._13*b._31 + a._14*b._41,
     a._11*b._12 + a._12*b._22 + a._13*b._32 + a._14*b._42,
     a._11*b._13 + a._12*b._23 + a._13*b._33 + a._14*b._43,
     a._11*b._14 + a._12*b._24 + a._13*b._34 + a._14*b._44,
     a._21*b._11 + a._22*b._21 + a._23*b._31 + a._24*b._41,
     a._21*b._12 + a._22*b._22 + a._23*b._32 + a._24*b._42,
     a._21*b._13 + a._22*b._23 + a._23*b._33 + a._24*b._43,
     a._21*b._14 + a._22*b._24 + a._23*b._34 + a._24*b._44,
     a._31*b._11 + a._32*b._21 + a._33*b._31 + a._34*b._41,
     a._31*b._12 + a._32*b._22 + a._33*b._32 + a._34*b._42,
     a._31*b._13 + a._32*b._23 + a._33*b._33 + a._34*b._43,
     a._31*b._14 + a._32*b._24 + a._33*b._34 + a._34*b._44,
     a._41*b._11 + a._42*b._21 + a._43*b._31 + a._44*b._41,
     a._41*b._12 + a._42*b._22 + a._43*b._32 + a._44*b._42,
     a._41*b._13 + a._42*b._23 + a._43*b._33 + a._44*b._43,
     a._41*b._14 + a._42*b._24 + a._43*b._34 + a._44*b._44);
}

D3DMATRIX D3DMATRIXCREATE(
	float _11, float _12, float _13, float _14,
    float _21, float _22, float _23, float _24,
    float _31, float _32, float _33, float _34,
    float _41, float _42, float _43, float _44)
{
	D3DMATRIX m;
	m._11 = _11;
	m._12 = _12;
	m._13 = _13;
	m._14 = _14;

	m._21 = _21;
	m._22 = _22;
	m._23 = _23;
	m._24 = _24;

	m._31 = _31;
	m._32 = _32;
	m._33 = _33;
	m._34 = _34;

	m._41 = _41;
	m._42 = _42;
	m._43 = _43;
	m._44 = _44;

	return m;
}

inline bool ApproxEqual(float a, float b, float epsilon)
{
	if (abs(a - b) < epsilon) return true;
	else return false;
}

bool ApproxEqual(D3DVECTOR& a, D3DVECTOR& b, float epsilon)
{
	if (ApproxEqual(a.x, b.x, epsilon) &&
		ApproxEqual(a.y, b.y, epsilon) &&
		ApproxEqual(a.z, b.z, epsilon)) return true;
	return false;
}

// Gets lenght of the given vector
float GetLength(D3DVECTOR& a)
{
	return sqrt(a.x * a.x + a.y * a.y + a.z * a.z);
}

// Normalizes the given vector
void Normalize(D3DVECTOR& a)
{	
	double length = GetLength(a);
    if (length == 0.0f) return;

	a.x = a.x / length;
	a.y = a.y / length;
	a.z = a.z / length;
}

// Produces cross product
D3DVECTOR CrossProduct(D3DVECTOR& u, D3DVECTOR& v)
{
	D3DVECTOR result;
	result.x = u.y * v.z - u.z * v.y;
    result.y = u.z * v.x - u.x * v.z;
    result.z = u.x * v.y - u.y * v.x;
	return result;
}

// Appends transform along to the given direction 
// (the object must have original direction along YAxis(!))
D3DMATRIX TransformAlongTo(D3DVECTOR direction)
{
	Normalize(direction);

	D3DMATRIX matrix = D3DMATRIXCREATE(
		1.0f, 0, 0, 0,
		0, 1.0f, 0, 0,
		0, 0, 1.0f, 0,
		0, 0, 0, 1.0f);

	D3DVECTOR up, down;
	up.x = up.z = down.x = down.z = 0;
	up.y = +1;
	down.y = -1;

    if ((!ApproxEqual(down, direction, 0.001)) && 
        (!ApproxEqual(up, direction, 0.001)))
    {
		D3DVECTOR temp; temp.x = temp.z = 0; temp.y = 10.0f;

		D3DVECTOR firstVector = CrossProduct(temp, direction);
        Normalize(firstVector);

        // Получаем перпендикуляр к полученному ранее перепендикуляру
        D3DVECTOR secondVector = CrossProduct(firstVector, direction);
        Normalize(secondVector);
                
        // Получаем матрицу поворота
        matrix._11 = firstVector.x; matrix._12 = firstVector.y; matrix._13 = firstVector.z;
        matrix._21 = direction.x; matrix._22 = direction.y; matrix._23 = direction.z;
        matrix._31 = secondVector.x; matrix._32 = secondVector.y; matrix._33 = secondVector.z;
    }
    else if (ApproxEqual(direction, down, 0.001))
    {
		matrix._22 = -1.0f;
    }
    return matrix;
}