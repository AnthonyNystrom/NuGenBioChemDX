#include "device.h"
#include "render.h"
#include "mesh.h"
#include "sphere.h"
#include "cylinder.h"
#include "helpers.h"
#pragma once

// Main effects:
// Phong lighting model
ID3DXEffect* phongShader = NULL;
// Quasi-realistic molecule shading model
ID3DXEffect* quasiShader = NULL;

// Depth surface
LPDIRECT3DSURFACE9 depthSurface = NULL;

Mesh* lowPolySphere = NULL;
Mesh* middlePolySphere = NULL;
Mesh* highPolySphere = NULL;

Mesh* lowPolyCylinder = NULL;
Mesh* middlePolyCylinder = NULL;
Mesh* highPolyCylinder = NULL;



// Disposes all residues meshes
void Render::DisposeResidueMeshes()
{
	if (residuesMeshes != NULL)
	{
		for (int i = 0; i < residuesMeshesCount; i++)
		{
			residuesMeshes[i]->Release();
			delete residuesMeshes[i];
		}
		delete [] residuesMeshes;
		residuesMeshes = NULL;
	}
}

// Effects creating
bool EnsureShaders1()
{
    // Get or create device
    LPDIRECT3DDEVICE9 device = GetDevice();
    if (device == NULL) return false;

    LPD3DXBUFFER errorsBuffer = NULL;

    // Load Phong effect
    if (phongShader == NULL)
    {
        D3DXCreateEffectFromFile(device, L"NuGenBioChem.Native.Phong.fx", NULL, NULL, D3DXSHADER_OPTIMIZATION_LEVEL3, NULL, &phongShader, &errorsBuffer);
                        
        if (phongShader == NULL) 
        {			
            if (errorsBuffer != NULL) 
            {
                // TIP: hit breakpoint here to see errors
                char* errors = (char*)errorsBuffer->GetBufferPointer();
                errorsBuffer->Release();
            }
            return false;
        }

		phongShader->SetTechnique("Phong");
    }

    // Load Quasi-realistic molecule shading model
    if (quasiShader == NULL)
    {
        // TODO: Add quasi-realistic molecule shading effect
    }

    if (errorsBuffer != NULL) errorsBuffer->Release();
    return true;
}

// Constructor
Render::Render()
{
    atoms = NULL;
	atomCount = 0;
	bonds = NULL;
	bondCount = 0;
	elementMaterials = NULL;
	elementMaterialCount = 0;   
	residues = NULL;
	residuesCount = 0;
	residueMaterials = NULL;
	residuesMeshes = NULL;
	residuesMeshesCount = 0;
}

// Destructor
Render::~Render()
{
    //  Free allocated memory for atoms
    if (atoms != NULL) delete [] atoms;
	//  Free allocated memory for bonds
    if (bonds != NULL) delete [] bonds;
	//  Free allocated memory for element materials
    if (elementMaterials != NULL) delete [] elementMaterials;
	//  Free allocated memory for residues
    if (residues != NULL) delete [] residues;
	//  Free allocated memory for residue's materials
	if (residueMaterials != NULL) delete [] residueMaterials;
	// Release meshes
	DisposeResidueMeshes();
}

// Sets atoms (atoms must be copied to local array)
void Render::SetAtoms(Atom* atoms, int count)
{
    if (this->atoms != NULL) delete [] this->atoms;
    this->atoms = new Atom[count];
	this->atomCount = count;
    memcpy(this->atoms, atoms, sizeof(Atom) * count);
}

// Sets materials (materials must be copied to local array)
void Render::SetElementMaterials(Material* materials, int count)
{
    if (this->elementMaterials != NULL) delete [] this->elementMaterials;
    this->elementMaterials = new Material[count];
	this->elementMaterialCount = count;
    memcpy(this->elementMaterials, materials, sizeof(Material) * count);
}

// Sets bonds (bonds must be copied to local array)
void Render::SetBonds(Bond* bonds, int count)
{
	if (this->bonds != NULL) delete [] this->bonds;
    this->bonds = new Bond[count];
	this->bondCount = count;
    memcpy(this->bonds, bonds, sizeof(Bond) * count);
}

// Sets residues (residues must be copied to local array)
void Render::SetResidues(Residue* residues, int count)
{
	if (this->residues != NULL) delete [] this->residues;
    this->residues = new Residue[count];
	this->residuesCount = count;
    memcpy(this->residues, residues, sizeof(Residue) * count);

	// Clear previous meshes
	DisposeResidueMeshes();	
}


// Sets residues (residues must be copied to local array)
void Render::SetResidueMaterials(Material* materials, int count)
{
	if (this->residueMaterials != NULL) delete [] this->residueMaterials;
    this->residueMaterials = new Material[count];
	this->residueMaterialCount = count;
    memcpy(this->residueMaterials, materials, sizeof(Material) * count);
}

// Sets bond's render options
void Render::SetBondRenderOptions(BondRenderOptions* options)
{
	memcpy(&(this->bondRenderOptions), options, sizeof(BondRenderOptions));
}

void Render::Draw(IDirect3DSurface9* renderTarget, D3DXMATRIX* view, D3DXMATRIX* projection)
{
    // Get or create device
    LPDIRECT3DDEVICE9 device = GetDevice();
    if (device == NULL) return;

	// Load shaders if it is required
//    if (!EnsureShaders()) return;
    
	// Prepare depth surface
	D3DSURFACE_DESC renderTargetDescription;
	renderTarget->GetDesc(&renderTargetDescription);
	D3DSURFACE_DESC depthSurfaceDescription;
	if (depthSurface != NULL) depthSurface->GetDesc(&depthSurfaceDescription);
	if (depthSurface == NULL || depthSurfaceDescription.Width != renderTargetDescription.Width || depthSurfaceDescription.Height != renderTargetDescription.Height)
	{
		if (depthSurface != NULL) depthSurface->Release();
		device->CreateDepthStencilSurface(renderTargetDescription.Width, renderTargetDescription.Height, D3DFMT_D24X8, D3DMULTISAMPLE_NONE, 0, FALSE, &depthSurface, NULL);
		if (depthSurface == NULL) return;
	}

    device->SetRenderTarget(0, renderTarget);
	device->SetDepthStencilSurface(depthSurface);
    device->Clear(0, NULL, D3DCLEAR_TARGET | D3DCLEAR_ZBUFFER, D3DCOLOR_ARGB(0, 0, 0, 0), 1.0f, 0);

	if (lowPolySphere == NULL)
	{
		// Create spheres
		lowPolySphere = CreateSphere(2);
		middlePolySphere = CreateSphere(3);
		highPolySphere = CreateSphere(5);
		// Create cylinders
		lowPolyCylinder = CreateCylinder(3);
		middlePolyCylinder = CreateCylinder(12);
		highPolyCylinder = CreateCylinder(24);
	}

    
	
	// FIXME: light dir must be slightly different!
	D3DVECTOR lightDirection; lightDirection.x = view->_13; lightDirection.y = view->_23; lightDirection.z = view->_33;
	D3DVECTOR viewDirection; viewDirection.x = view->_13; viewDirection.y = view->_23; viewDirection.z = view->_33;
	elementMaterials[0].SetViewLightDirection(&viewDirection, &lightDirection);

	// Rendering
	device->BeginScene();
	

    DrawAtoms(view, projection);
	DrawBonds(view, projection);
	DrawResidues(view, projection);

	device->EndScene();
}

void Render::DrawAtoms(D3DMATRIX* view, D3DMATRIX* projection)
{
    // Apply the technique contained in the effect   
	BYTE previousRenderedMaterial = 255;

	for(int iAtom = 0; iAtom < atomCount; iAtom++)
	{
		Atom* currentAtom = &atoms[iAtom];

		Material* material = elementMaterials + currentAtom->Material;
		
		// Scale & translate the sphere
		D3DXMATRIX world = D3DMATRIXCREATE(
			currentAtom->Radius,0,0,0, 
			0,currentAtom->Radius,0,0, 
			0,0,currentAtom->Radius,0, 
			currentAtom->Position.x,currentAtom->Position.y,currentAtom->Position.z,1.0f);
			
		material->SetMatrices(&world, view, projection);
					

		// Setup material's parameters	
		if (previousRenderedMaterial != currentAtom->Material)
		{
			// Skip if the params has been already presented
			previousRenderedMaterial = currentAtom->Material;
			material->Apply();
		}
			
		// Render the mesh with the applied technique
		highPolySphere->Draw();
	}
}

void Render::DrawResidues(D3DMATRIX* view, D3DMATRIX* projection)
{
    // Apply the technique contained in the effect   
	BYTE previousRenderedMaterial = 255;

	if (residueMaterials == NULL || residues == NULL) return;
	if (residuesMeshes == NULL)
	{
		residuesMeshesCount = residuesCount;
		residuesMeshes = new Mesh*[residuesCount];

		for (int i = 0; i < residuesMeshesCount; i++)
		{
			Mesh* mesh = new Mesh();
			mesh->Set(residues[i].VerticesPointer, residues[i].VerticesCount, residues[i].IndicesPointer, residues[i].IndicesCount);
			residuesMeshes[i] = mesh;
		}
	}

	// Scale & translate the sphere
	D3DXMATRIX world = D3DMATRIXCREATE(
		1,0,0,0, 
		0,1,0,0, 
		0,0,1,0, 
		0,0,0,1.0f);
			
	residueMaterials[0].SetMatrices(&world, view, projection);

	for(int i = 0; i < residuesCount; i++)
	{
		Residue* currentResidue = &residues[i];

		Material* material = residueMaterials + currentResidue->Material;
		
		// Setup material's parameters	
		if (previousRenderedMaterial != currentResidue->Material)
		{
			// Skip if the params has been already presented
			previousRenderedMaterial = currentResidue->Material;
			material->Apply();
		}
			
		// Render the mesh with the applied technique
		residuesMeshes[i]->Draw();
	}
}

void Render::DrawBonds(D3DMATRIX* view, D3DMATRIX* projection)
{
	// Apply the technique contained in the effect   
	BYTE previousRenderedMaterial = 255;

	if (bondRenderOptions.UseSingleMaterial)
	{
		bondRenderOptions.Material.Apply();
	}

	for(int iBond = 0; iBond < bondCount; iBond++)
	{
		Bond* currentBond = &bonds[iBond];

		Material* material = bondRenderOptions.UseSingleMaterial ? 
			&bondRenderOptions.Material : 
		    elementMaterials + currentBond->Material;
		
	
		// Transform along to direction
		D3DVECTOR direction;
		direction.x = currentBond->End.x - currentBond->Begin.x;
		direction.y = currentBond->End.y - currentBond->Begin.y;
		direction.z = currentBond->End.z - currentBond->Begin.z;
		float height = GetLength(direction);
		//Normalize(direction);
		D3DMATRIX alongTo = TransformAlongTo(direction);

		// Scale 
		D3DXMATRIX scale = D3DMATRIXCREATE(
			bondRenderOptions.BondSize,0,0,0, 
			0,1,0,0, 
			0,0,bondRenderOptions.BondSize,0, 
			0,0,0,1.0f);

		// Translate the cylinder
		D3DXMATRIX translate = D3DMATRIXCREATE(
			1.0f,0,0,0, 
			0,1.0f,0,0, 
			0,0,1.0f,0, 
			currentBond->Begin.x,currentBond->Begin.y,currentBond->Begin.z,1.0f);
				
		D3DXMATRIX world = Multiply(Multiply(scale, alongTo), translate);
		material->SetMatrices(&world, view, projection);
					

		// Setup material's parameters	
		if (!bondRenderOptions.UseSingleMaterial && previousRenderedMaterial != currentBond->Material)
		{
			// Skip if the params has been already presented
			previousRenderedMaterial = currentBond->Material;
			material->Apply();
		}
			
		// Render the mesh with the applied technique
		highPolyCylinder->Draw();
	}
}

