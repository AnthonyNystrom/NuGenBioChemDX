#include "device.h"
#include "mesh.h"
#include "helpers.h"
#pragma once

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

// Length
inline FLOAT Length(const D3DVECTOR &v)
{
    return sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
}

// Helper function to add a vertex
void AddVertex(FLOAT x, FLOAT y, FLOAT z, FLOAT nx, FLOAT ny, FLOAT nz, FLOAT u, FLOAT v)
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

void AddIndex(USHORT index)
{
    indices[indexCurrentIndex++] = index;
}

/// <summary>
/// Helper function to create a face for the base octahedron.
/// </summary>
/// <param name="mesh">Mesh</param>
/// <param name="p1">Vertex 1.</param>
/// <param name="p2">Vertex 2.</param>
/// <param name="p3">Vertex 3.</param>
void AddBaseTriangle(D3DVECTOR& p1, D3DVECTOR& p2, D3DVECTOR& p3)
{
    AddVertex(p1.x, p1.y, p1.z, p1.x, p1.y, p1.z, 0, 0);
    AddVertex(p2.x, p2.y, p2.z, p2.x, p2.y, p2.z, 0, 0);
    AddVertex(p3.x, p3.y, p3.z, p3.x, p3.y, p3.z, 0, 0);
    
    AddIndex(nextIndex++);
    AddIndex(nextIndex++);
    AddIndex(nextIndex++);
}

/// <summary>
/// Calculates the midpoint between two points on a unit sphere and projects the result
/// back to the surface of the sphere.
/// </summary>
/// <param name="p1">Point 1.</param>
/// <param name="p2">Point 2.</param>
/// <returns>The normalized midpoint.</returns>
static D3DVECTOR GetNormalizedMidpoint(D3DVECTOR& p1, D3DVECTOR& p2)
{
    D3DVECTOR vector;
    vector.x = (p1.x + p2.x) / 2;
    vector.y = (p1.y + p2.y) / 2;
    vector.z = (p1.z + p2.z) / 2;
    Normalize(vector);

    return vector;
}

/// <summary>
/// Replaces a triangle at a given index buffer offset and replaces it with four triangles
/// that compose an equilateral subdivision.
/// </summary>
/// <param name="mesh">Mesh</param>
/// <param name="indexOffset">An offset into the index buffer.</param>
void DivideTriangle(USHORT indexOffset)
{	
    USHORT i1 = indices[indexOffset];
    USHORT i2 = indices[indexOffset + 1];
    USHORT i3 = indices[indexOffset + 2];

    D3DVECTOR p1; p1.x = vertices[i1].x; p1.y = vertices[i1].y, p1.z = vertices[i1].z;
    D3DVECTOR p2; p2.x = vertices[i2].x; p2.y = vertices[i2].y, p2.z = vertices[i2].z;
    D3DVECTOR p3; p3.x = vertices[i3].x; p3.y = vertices[i3].y, p3.z = vertices[i3].z;	
    D3DVECTOR p4 = GetNormalizedMidpoint(p1, p2);
    D3DVECTOR p5 = GetNormalizedMidpoint(p2, p3);
    D3DVECTOR p6 = GetNormalizedMidpoint(p3, p1);

    AddVertex(p4.x, p4.y, p4.z, p4.x, p4.y, p4.z, 0, 0);
    AddVertex(p5.x, p5.y, p5.z, p5.x, p5.y, p5.z, 0, 0);
    AddVertex(p6.x, p6.y, p6.z, p6.x, p6.y, p6.z, 0, 0);

    USHORT i4 = nextIndex++;
    USHORT i5 = nextIndex++;
    USHORT i6 = nextIndex++;

    indices[indexOffset] = i4;
    indices[indexOffset + 1] = i5;
    indices[indexOffset + 2] = i6;

    AddIndex(i1);
    AddIndex(i4);
    AddIndex(i6);

    AddIndex(i4);
    AddIndex(i2);
    AddIndex(i5);

    AddIndex(i6);
    AddIndex(i5);
    AddIndex(i3);
}

/// <summary>
/// Performs the recursive subdivision.
/// </summary>
/// <param name="mesh">Mesh</param>
void Divide()
{
    int indexCount = indexCurrentIndex;

    for (USHORT indexOffset = 0; indexOffset < indexCount; indexOffset += 3)
        DivideTriangle(indexOffset);
}



// Creates a sphere
Mesh* CreateSphere(int subdivisions)
{
    nextIndex = 0;
	// TODO: calc how many items in the arrays we need
    vertices = new Vertex[100000];
	vertexCurrentIndex = 0;
	indices = new USHORT[100000];
	indexCurrentIndex = 0;

    AddBaseTriangle(CreateVector(0, 0, 1), CreateVector(1, 0, 0), CreateVector(0, 1, 0));
    AddBaseTriangle(CreateVector(1, 0, 0), CreateVector(0, 0, -1), CreateVector(0, 1, 0));
    AddBaseTriangle(CreateVector(0, 0, -1), CreateVector(-1, 0, 0), CreateVector(0, 1, 0));
    AddBaseTriangle(CreateVector(-1, 0, 0), CreateVector(0, 0, 1), CreateVector(0, 1, 0));
    AddBaseTriangle(CreateVector(1, 0, 0), CreateVector(0, 0, 1), CreateVector(0, -1, 0));
    AddBaseTriangle(CreateVector(0, 0, -1), CreateVector(1, 0, 0), CreateVector(0, -1, 0));
    AddBaseTriangle(CreateVector(-1, 0, 0), CreateVector(0, 0, -1), CreateVector(0, -1, 0));
    AddBaseTriangle(CreateVector(0, 0, 1), CreateVector(-1, 0, 0), CreateVector(0, -1, 0));

    for (int division = 1; division < subdivisions; division++) Divide();

	Mesh* mesh = new Mesh();
	mesh->Set(vertices, vertexCurrentIndex, indices, indexCurrentIndex);

	// Free allocated arrays
	delete [] vertices;
	delete [] indices;

	return mesh;
}

