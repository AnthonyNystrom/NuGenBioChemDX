#include <windows.h>
#include "device.h"
#include "material.h"
#include "mesh.h"

#pragma once

// Represents an atom
struct Atom
{
	// Position of center of atom
	D3DVECTOR Position;
	// Radius of the atom
	float Radius;
	// Index of the material
	byte Material;
};

// Represents an bond
struct Bond
{
	// Position of begin of the bond
	D3DVECTOR Begin;
	// Position of end of the bond
	D3DVECTOR End;
	// Material
	byte Material;
};

struct Residue
{
    void* VerticesPointer;
    int VerticesCount;
    void* IndicesPointer;
    int IndicesCount;
    byte Material;
};

// Represents an atom
struct BondRenderOptions
{
	// Size of bonds
    float BondSize;
    // Material of the bonds
    Material Material;
    // Whether we heve to use single material
    // (as boolean value)
    byte UseSingleMaterial;
};


// Represents the main renderer
class Render
{
	// All rendered atoms
	Atom* atoms;
	int atomCount;
	// All rendered bonds
	Bond* bonds;
	int bondCount;
	// Render options for bonds
	BondRenderOptions bondRenderOptions;
	// All rendered residues
	Residue* residues;
	int residuesCount;

	
	Mesh** residuesMeshes;
	int residuesMeshesCount;

	// Elements materials
	Material* elementMaterials;
	int elementMaterialCount;
	// Residue's materials
	Material* residueMaterials;
	int residueMaterialCount;

	// Private functions
	void DrawAtoms(D3DMATRIX* view, D3DMATRIX* projection);
	void DrawBonds(D3DMATRIX* view, D3DMATRIX* projection);
	void DrawResidues(D3DMATRIX* view, D3DMATRIX* projection);
	void DisposeResidueMeshes();

	public:
	
	// Constructor
	Render();
	// Destructor
	~Render();

	// Sets atoms (atoms must be copied to local array)
	void SetAtoms(Atom* atoms, int count);
	// Sets bonds (bonds must be copied to local array)
	void SetBonds(Bond* bonds, int count);
	// Sets residues (residues must be copied to local array)
	void SetResidues(Residue* residues, int count);
	// Sets bond's render options
	void SetBondRenderOptions(BondRenderOptions* bonds);
	// Sets materials (materials must be copied to local array)
	void SetElementMaterials(Material* materials, int materialCount);
	// Sets residues (residues must be copied to local array)
	void SetResidueMaterials(Material* materials, int materialCount);

	void Draw(IDirect3DSurface9* renderTarget, D3DXMATRIX* view, D3DXMATRIX* projection);
};

