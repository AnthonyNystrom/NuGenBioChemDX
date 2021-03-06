#include "device.h"
#include "cylinder.h"
#include "helpers.h"
#include <math.h>
#pragma once

#define PI 3.1415926535897932384626433832795

// Static temp data (used during tessellation)
static USHORT nextIndex;
static Vertex* vertices;
static USHORT vertexCurrentIndex;
static USHORT* indices;
static USHORT indexCurrentIndex;

inline D3DVECTOR CreateVector(FLOAT x, FLOAT y, FLOAT z)
{
    D3DVECTOR vector;
    vector.x = x;
    vector.y = y;
    vector.z = z;
    return vector;
}

// Cylinder equation
D3DVECTOR Function(double height, double angle)
{
    D3DVECTOR result;
	result.x =  sin(angle);
    result.y =  height;
    result.z =  cos(angle);
	
    return result;
}

// Helper function to add a vertex
void AddVertex1(FLOAT x, FLOAT y, FLOAT z, FLOAT nx, FLOAT ny, FLOAT nz, FLOAT u, FLOAT v)
{
    Vertex* currentVertex = vertices + vertexCurrentIndex;
    currentVertex->x = x;
    currentVertex->y = y;
    currentVertex->z = z;
    currentVertex->nx = nx;
    currentVertex->ny = ny;
    currentVertex->nz = nz;
	currentVertex->tu = u;
    currentVertex->tv = v;
    vertexCurrentIndex++;
}

void AddIndex1(USHORT index)
{
    indices[indexCurrentIndex++] = index;
}

// Creates a cylinder
Mesh* CreateCylinder(int slices)
{   
	vertices = new Vertex[slices * 4];
	vertexCurrentIndex = 0;
	indices = new USHORT[slices * 6];
	indexCurrentIndex = 0;


    double slice = (2.0 * PI) / (double)slices;
	
    for(int i = 0; i < slices; i++)
    {
        int secondIndex = (i == slice - 1) ? 0 : i + 1;
        D3DVECTOR topFirst = Function(1, slice * i);
        D3DVECTOR topSecond = Function(1, slice * secondIndex);
        D3DVECTOR bottomFirst = Function(0, slice * i);
        D3DVECTOR bottomSecond = Function(0, slice * secondIndex);

        D3DVECTOR firstNormal = CreateVector(bottomFirst.x, 0, bottomFirst.z);
        D3DVECTOR secondNormal = CreateVector(bottomSecond.x, 0, bottomSecond.z);

		AddVertex1(topFirst.x, topFirst.y, topFirst.z, firstNormal.x, firstNormal.y, firstNormal.z, 0, 0);
		AddVertex1(topSecond.x, topSecond.y, topSecond.z, secondNormal.x, secondNormal.y, secondNormal.z, 0, 0);
		AddVertex1(bottomFirst.x, bottomFirst.y, bottomFirst.z, firstNormal.x, firstNormal.y, firstNormal.z, 0, 0);
		AddVertex1(bottomSecond.x, bottomSecond.y, bottomSecond.z, secondNormal.x, secondNormal.y, secondNormal.z, 0, 0);
                 				 
        AddIndex1(i * 4 + 0);
        AddIndex1(i * 4 + 3);
        AddIndex1(i * 4 + 1);

        AddIndex1(i * 4 + 0);
        AddIndex1(i * 4 + 2);
        AddIndex1(i * 4 + 3);
    }

	Mesh* mesh = new Mesh();
	mesh->Set(vertices, vertexCurrentIndex, indices, indexCurrentIndex);

	// Free allocated arrays
	delete [] vertices;
	delete [] indices;

	return mesh;
}

