#include <windows.h>
#include "device.h"
#include "render.h"



// Creates render
extern "C" __declspec(dllexport) LPVOID WINAPI CreateRender()
{
    return new Render();
}

// Set atoms to the render
extern "C" __declspec(dllexport) VOID WINAPI SetAtoms(Render* render, Atom* atoms, int atomCount)
{
	render->SetAtoms(atoms, atomCount);
}

// Set residues to the render
extern "C" __declspec(dllexport) VOID WINAPI SetResidues(Render* render, Residue* residues, int residuesCount)
{
	render->SetResidues(residues, residuesCount);
}

// Set bonds to the render
extern "C" __declspec(dllexport) VOID WINAPI SetBonds(Render* render, Bond* bonds, int bondCount)
{
	render->SetBonds(bonds, bondCount);
}

// Set bonds render options
extern "C" __declspec(dllexport) VOID WINAPI SetBondRenderOptions(Render* render, BondRenderOptions* options)
{
	render->SetBondRenderOptions(options);
}

// Set element materials to the render
extern "C" __declspec(dllexport) VOID WINAPI SetElementMaterials(Render* render, Material* materials, int materialCount)
{
	render->SetElementMaterials(materials, materialCount);
}

// Set element materials to the render
extern "C" __declspec(dllexport) VOID WINAPI SetResidueMaterials(Render* render, Material* materials, int materialCount)
{
	render->SetResidueMaterials(materials, materialCount);
}


// Release render
extern "C" __declspec(dllexport) VOID WINAPI DisposeRender(Render* render)
{
    delete render;
}

// Creates render target
extern "C" __declspec(dllexport) LPVOID WINAPI CreateRenderTarget(INT width, INT height)
{
    IDirect3DSurface9* renderTarget = NULL;
    if (IsDeviceEnhanced()) 
    {
        // To improve WPF D3DImage perf we need enhanced version of surface
        IDirect3DDevice9Ex* d3d9Exdevice = (IDirect3DDevice9Ex*)GetDevice();
        d3d9Exdevice->CreateRenderTargetEx(width, height, D3DFMT_A8R8G8B8, D3DMULTISAMPLE_NONE, 0, FALSE, &renderTarget, NULL, 0);
    }
    else GetDevice()->CreateRenderTarget(width, height, D3DFMT_A8R8G8B8, D3DMULTISAMPLE_NONE, 0, TRUE, &renderTarget, NULL);
    return renderTarget;
}

// Release render target
extern "C" __declspec(dllexport) VOID WINAPI DisposeRenderTarget(IDirect3DSurface9* renderTarget)
{
    if (renderTarget != NULL) renderTarget->Release();
}

// Get render target data
extern "C" __declspec(dllexport) VOID WINAPI GetRenderTargetData(IDirect3DSurface9* renderTarget, byte* data)
{
    LPDIRECT3DDEVICE9 device = GetDevice();
    if (device == NULL) return;

    // Get render target's parameters
    D3DSURFACE_DESC renderTargetDescription;
    renderTarget->GetDesc(&renderTargetDescription);
    if (renderTargetDescription.Width == 0) return;
    
    // Create texture in system memory
    LPDIRECT3DTEXTURE9 texture = NULL;
    device->CreateTexture(renderTargetDescription.Width, renderTargetDescription.Height, 1, D3DUSAGE_DYNAMIC, renderTargetDescription.Format, D3DPOOL_SYSTEMMEM, &texture, NULL);
    if (texture == NULL) return;

    // Get surface of that texture
    LPDIRECT3DSURFACE9 surface = NULL;
    texture->GetSurfaceLevel(0, &surface);
    if (surface == NULL)
    {
        texture->Release();
        return;
    }

    // Download data to our system memory texture
    device->GetRenderTargetData(renderTarget, surface);

	D3DLOCKED_RECT lockedRect;
	ZeroMemory(&lockedRect, sizeof(D3DLOCKED_RECT));
	surface->LockRect(&lockedRect, NULL, 0);
	if (lockedRect.pBits != 0)
	{		
		if (4 * renderTargetDescription.Width == lockedRect.Pitch) memcpy(data, lockedRect.pBits, 4 * renderTargetDescription.Width * renderTargetDescription.Height);  
		else
		{
			for (int i = 0; i < renderTargetDescription.Height; i++)
			{
				// TODO: this part of code is not checked
				memcpy(data + i * 4 * renderTargetDescription.Width, (byte*)lockedRect.pBits + i * lockedRect.Pitch, 4 * renderTargetDescription.Width);  
			}
		}
		surface->UnlockRect();
	}

    // Release objects
    surface->Release();
    texture->Release();
}


//  Determines whether hardware is compatible
extern "C" __declspec(dllexport) BOOL IsHardwareCompatible()
{
    return WhetherHardwareCompatible();
}

#ifndef WPFMATRIX_DEFINED
typedef struct _WPFMATRIX {
    union {
        struct {
            double        _11, _12, _13, _14;
            double        _21, _22, _23, _24;
            double        _31, _32, _33, _34;
            double        _41, _42, _43, _44;

        };
        float m[4][4];
    };
} WPFMATRIX;
#define WPFMATRIX_DEFINED
#endif

void ConvertMatrix(WPFMATRIX* from, D3DXMATRIX* to)
{
    to->_11 = from->_11;
    to->_12 = from->_12;
    to->_13 = from->_13;
    to->_14 = from->_14;

    to->_21 = from->_21;
    to->_22 = from->_22;
    to->_23 = from->_23;
    to->_24 = from->_24;

    to->_31 = from->_31;
    to->_32 = from->_32;
    to->_33 = from->_33;
    to->_34 = from->_34;

    to->_41 = from->_41;
    to->_42 = from->_42;
    to->_43 = from->_43;
    to->_44 = from->_44;
}

// Render to the render target
extern "C" __declspec(dllexport) VOID WINAPI RenderTo(Render* render, IDirect3DSurface9* renderTarget, WPFMATRIX* viewMatrix, WPFMATRIX* projectionMatrix)
{
    D3DXMATRIX view, projection;

    ConvertMatrix(viewMatrix, &view);
    ConvertMatrix(projectionMatrix, &projection);

    render->Draw(renderTarget, &view, &projection);
}