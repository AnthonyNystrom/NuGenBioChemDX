#include "device.h"
#include "mesh.h"
#pragma once

// Constructor
Mesh::Mesh()
{
    indexBuffer = NULL;
    vertexBuffer = NULL;
    vertexDeclaration = NULL;
}

// Destructor
Mesh::~Mesh()
{
    Release();
}

// Releases dx-interfaces
void Mesh::Release()
{
    if (indexBuffer != NULL)
    {
        indexBuffer->Release();
        indexBuffer = NULL;
    }
    if (vertexBuffer != NULL)
    {
        vertexBuffer->Release();
        vertexBuffer = NULL;		
    }
	if (vertexDeclaration != NULL)
    {
        vertexDeclaration->Release();
        vertexDeclaration = NULL;		
    }
}

// Sets data
void Mesh::Set(void* vertices, int vertexCount, void* indices, int indexCount)
{	
	Release();

	this->vertexCount = vertexCount;
	this->indexCount = indexCount;

	// Get or create device
    LPDIRECT3DDEVICE9 device = GetDevice();
    if (device == NULL) return;

	// Create index buffer
	D3DFORMAT indexBufferFormat = indexCount < 65536 ? D3DFMT_INDEX16 : D3DFMT_INDEX32;
	UINT indexBufferSize = indexCount < 65536 ? indexCount * 2 : indexCount * 4;
	device->CreateIndexBuffer(indexBufferSize, D3DUSAGE_WRITEONLY, indexBufferFormat, D3DPOOL_DEFAULT, &indexBuffer, NULL);
	if (indexBuffer == NULL) return;

	// Set data to index buffer
	void* indexBufferLockedData = NULL;
	indexBuffer->Lock(0, indexBufferSize, &indexBufferLockedData, 0);
	if (indexBufferLockedData == NULL) return;
	memcpy(indexBufferLockedData, indices, indexBufferSize);
	indexBuffer->Unlock();

	// Create vertex buffer
	// TODO: rewrite the following code
	UINT vertexSize = sizeof (Vertex);
	UINT vertexBufferSize = vertexCount * vertexSize;
	device->CreateVertexBuffer(vertexBufferSize, D3DUSAGE_WRITEONLY, 0, D3DPOOL_DEFAULT, &vertexBuffer, NULL);
	if (vertexBuffer == NULL) return;

	// Set data to the vertex buffer
	void* vertexBufferLockedData = NULL;
	vertexBuffer->Lock(0, vertexBufferSize, &vertexBufferLockedData, 0);
	if (vertexBufferLockedData == NULL) return;
	memcpy(vertexBufferLockedData, vertices, vertexBufferSize);
	vertexBuffer->Unlock();

	// Create vertex declaration
	D3DVERTEXELEMENT9 vertexElements[] = 
	{
		{0,  0,  D3DDECLTYPE_FLOAT3, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_POSITION, 0},
		{0, 12,  D3DDECLTYPE_FLOAT3, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_NORMAL, 0},
		{0, 24,  D3DDECLTYPE_FLOAT2, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_TEXCOORD, 0},
		D3DDECL_END()
	};

	device->CreateVertexDeclaration(vertexElements, &vertexDeclaration);
	//delete [] vertexElements;
	if (vertexDeclaration == NULL) return;
}

// Draws the mesh
void Mesh::Draw()
{
	// Validate buffers
	if (vertexDeclaration == NULL || vertexBuffer == NULL || indexBuffer == NULL) return;

    // Get or create device
    LPDIRECT3DDEVICE9 device = GetDevice();
    if (device == NULL) return;

	// Set vertex declaration
	device->SetVertexDeclaration(vertexDeclaration);
	// Set vertex buffer
	device->SetStreamSource(0, vertexBuffer, 0, sizeof(Vertex));
	// Set index buffer
	device->SetIndices(indexBuffer);

	// Draw geometry
	device->DrawIndexedPrimitive(D3DPT_TRIANGLELIST, 0, 0, vertexCount, 0, indexCount / 3);
}