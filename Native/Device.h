#include <windows.h>
#include <d3d9.h>
#include <d3dx9.h>
#pragma once


// Gets the device
LPDIRECT3DDEVICE9 GetDevice();

// Is D3D9Ex device used
BOOL IsDeviceEnhanced();

//  Determines whether hardware is compatible
BOOL WhetherHardwareCompatible();