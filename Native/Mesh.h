#include <windows.h>
#include "device.h"

#pragma once

// Represent default structure for a vertex
struct Vertex 
{
		FLOAT x, y, z;		// Position
		FLOAT nx, ny, nz;	// Normal
		FLOAT tu, tv;		// Texture coordinates
};


// Represents a mesh object
class Mesh
{
	// Count of vertex & index elements
	UINT vertexCount, indexCount;
	// Index & vertex buffers
	LPDIRECT3DINDEXBUFFER9 indexBuffer;
	LPDIRECT3DVERTEXBUFFER9 vertexBuffer;
	// Vertex declaration
	LPDIRECT3DVERTEXDECLARATION9 vertexDeclaration;

	
    public:
    
    // Constructor
    Mesh();
    // Destructor
    ~Mesh();
	// Releases dx-interfaces
	void Release();

    // Draws the mesh
    void Draw();

	// Sets data
	// Indecies must be Int16[] if indexCount < 65536 otherwise Int32[] must be used
	void Set(void* vertices, int vertexCount, void* indices, int indexCount);
};

