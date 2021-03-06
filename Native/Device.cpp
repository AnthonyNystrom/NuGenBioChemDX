#include "device.h"
#include <d3d9.h>
#pragma once

LPDIRECT3D9             g_pD3D          = NULL;
LPDIRECT3DDEVICE9       g_pd3dDevice    = NULL;
LPDIRECT3D9EX           g_pD3DEx        = NULL;
LPDIRECT3DDEVICE9EX     g_pd3dDeviceEx  = NULL;
D3DPRESENT_PARAMETERS	presentParameters;

// Suppress to use D3DEx device
BOOL doNotUseEnhancedDevice = FALSE;

// Dafault message procedure
LRESULT WINAPI MsgProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    return DefWindowProc(hWnd, msg, wParam, lParam);
}

// Window class
WNDCLASSEX g_wc = { sizeof(WNDCLASSEX), CS_CLASSDC, MsgProc, 0L, 0L,
      GetModuleHandle(NULL), NULL, NULL, NULL, NULL, L"Foo", NULL };

// Determine hardware vertex processing capability
// ISSUE: how to do it right?
DWORD GetVertexProcessingCaps()
{
    D3DCAPS9 caps;
    DWORD dwVertexProcessing = D3DCREATE_SOFTWARE_VERTEXPROCESSING;
    if (SUCCEEDED(g_pD3D->GetDeviceCaps(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, &caps)))
    {
        if ((caps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT) == D3DDEVCAPS_HWTRANSFORMANDLIGHT)
        {
            dwVertexProcessing = D3DCREATE_HARDWARE_VERTEXPROCESSING;
        }
    }
    return dwVertexProcessing;
}

typedef HRESULT (WINAPI *DIRECT3DCREATE9EXFUNCTION)(UINT SDKVersion, IDirect3D9Ex**);

// Initialize subsystem
BOOL Initialize()
{
    // Set up the structure used to create the D3DDevice
    ZeroMemory(&presentParameters, sizeof(presentParameters));
    presentParameters.Windowed = TRUE;
    presentParameters.BackBufferHeight = 1;
    presentParameters.BackBufferWidth = 1;
    presentParameters.SwapEffect = D3DSWAPEFFECT_DISCARD;
    presentParameters.BackBufferFormat = D3DFMT_A8R8G8B8;

    // Create the device's window
    RegisterClassEx(&g_wc);
    HWND hWnd = CreateWindow(L"Foo", L"Foo", WS_OVERLAPPEDWINDOW, 
        0, 0, 0, 0, NULL, NULL, g_wc.hInstance, NULL);

    // Initialize Direct3D
    HMODULE hLibrary = NULL;
    hLibrary = LoadLibrary(TEXT("d3d9.dll"));
    if(hLibrary == NULL) return FALSE;

    DIRECT3DCREATE9EXFUNCTION pfnCreate9Ex = (DIRECT3DCREATE9EXFUNCTION)GetProcAddress(hLibrary, "Direct3DCreate9Ex");
    if (pfnCreate9Ex && !doNotUseEnhancedDevice)
    {
        if(FAILED((*pfnCreate9Ex)(D3D_SDK_VERSION, &g_pD3DEx))) goto createNonEx;
        if(FAILED(g_pD3DEx->QueryInterface(__uuidof(IDirect3D9), reinterpret_cast<void **>(&g_pD3DEx)))) goto createNonEx;
        g_pD3D = g_pD3DEx;
    }
    else
    {
        createNonEx:
        g_pD3DEx = NULL;
        if (NULL == (g_pD3D = Direct3DCreate9(D3D_SDK_VERSION)))
        {
            return FALSE;
        }
    }
    FreeLibrary(hLibrary);

    

    // Create the D3D device
    HRESULT result;
    if (g_pD3DEx)
    {
        if (FAILED(result = g_pD3DEx->CreateDeviceEx(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, hWnd,
            GetVertexProcessingCaps() | D3DCREATE_FPU_PRESERVE | D3DCREATE_MULTITHREADED,
            &presentParameters, NULL, &g_pd3dDeviceEx)))
        {
            return FALSE;
        }
        g_pd3dDevice = g_pd3dDeviceEx;
    }
    else
    {
        if (FAILED(result = g_pD3D->CreateDevice(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, hWnd,
            GetVertexProcessingCaps() | D3DCREATE_FPU_PRESERVE | D3DCREATE_MULTITHREADED,
            &presentParameters, &g_pd3dDevice)))
        {
            return FALSE;
        }
    }
        
    // Turn on culling counter clockwise
	g_pd3dDevice->SetRenderState(D3DRS_CULLMODE, D3DCULL_CW);
    // Turn on Z-buffer
	g_pd3dDevice->SetRenderState(D3DRS_ZENABLE, D3DZB_TRUE);
    g_pd3dDevice->SetRenderState(D3DRS_ZWRITEENABLE, TRUE);

    return TRUE;
}

// Release subsystem
VOID Release()
{
    UnregisterClass(NULL, g_wc.hInstance);

    if (g_pd3dDevice != NULL)
        g_pd3dDevice->Release();
    
    if (g_pD3D != NULL)
        g_pD3D->Release();
}


// Gets the device
LPDIRECT3DDEVICE9 GetDevice()
{
    if(g_pD3D == NULL) 
    {
        Initialize();
    }
    return g_pd3dDevice;
}

// Max texture size
INT GetMaxTextureSize()
{
    D3DCAPS9 caps;
    if(GetDevice()->GetDeviceCaps(&caps) == D3D_OK) return min(caps.MaxTextureWidth, min(caps.MaxTextureHeight, caps.MaxTextureAspectRatio));
    else return 4096;
}

//  Determines whether hardware is compatible
BOOL WhetherHardwareCompatible()
{
    D3DCAPS9 caps;
    ZeroMemory(&caps, sizeof(D3DCAPS9));
    LPDIRECT3DDEVICE9 device = GetDevice();
    if (device == NULL) return FALSE;
    device->GetDeviceCaps(&caps);
    
    // Pixel Shader 2.0 support
    if(D3DSHADER_VERSION_MAJOR(caps.PixelShaderVersion) < 2) return FALSE;

    return TRUE;
}

// Max texture bpp
INT GetMaxBitsPerPixel()
{
    if((FAILED(g_pD3D->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, D3DFMT_X8R8G8B8, D3DUSAGE_RENDERTARGET, D3DRTYPE_TEXTURE, D3DFMT_R32F))) ||
       (FAILED(g_pD3D->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, D3DFMT_X8R8G8B8, D3DUSAGE_RENDERTARGET, D3DRTYPE_TEXTURE, D3DFMT_A8R8G8B8)))) return 0;
    if((FAILED(g_pD3D->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, D3DFMT_X8R8G8B8, D3DUSAGE_RENDERTARGET, D3DRTYPE_TEXTURE, D3DFMT_A16B16G16R16))) ||
       (FAILED(g_pD3D->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, D3DFMT_X8R8G8B8, D3DUSAGE_RENDERTARGET, D3DRTYPE_TEXTURE, D3DFMT_A16B16G16R16F)))) return 32;
    if((FAILED(g_pD3D->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, D3DFMT_X8R8G8B8, D3DUSAGE_RENDERTARGET, D3DRTYPE_TEXTURE, D3DFMT_A32B32G32R32F)))) return 64;
    return 128;
}

BOOL IsDeviceEnhanced()
{
    if(g_pD3D == NULL) Initialize();
    return g_pd3dDeviceEx != NULL;
}

BOOL IsDeviceResetRequired()
{
    HRESULT result;
    if(g_pd3dDeviceEx != NULL)
    {
        while(true)
        {
            result = g_pd3dDeviceEx->CheckDeviceState(NULL);
            if(result == D3DERR_DEVICELOST) Sleep(200);
            else if(result == D3DERR_DEVICEHUNG) return TRUE;
            else return FALSE;
        }
    }
    else if (g_pd3dDevice != NULL)
    {
        while((result = g_pd3dDevice->TestCooperativeLevel()) == D3DERR_DEVICELOST) Sleep(200);
        return result != D3D_OK;
    }
    else return FALSE;
}

// If true resource recteation is required
VOID ResetDevice()
{
    if(g_pd3dDeviceEx != NULL) g_pd3dDeviceEx->ResetEx(&presentParameters, NULL);
    else if(g_pd3dDevice != NULL) g_pd3dDevice->Reset(&presentParameters);		
    
    // Restore render states
    if(g_pd3dDevice != NULL)
    {
        //g_pd3dDevice->SetRenderState(D3DRS_CULLMODE, D3DCULL_NONE);
        //g_pd3dDevice->SetRenderState(D3DRS_ZENABLE, D3DZB_FALSE);
        //g_pd3dDevice->SetRenderState(D3DRS_ZWRITEENABLE, FALSE);
    }    
}
