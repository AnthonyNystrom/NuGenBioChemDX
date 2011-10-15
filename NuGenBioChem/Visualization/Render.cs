using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using NuGenBioChem.Data;
using Material = NuGenBioChem.Data.Material;

namespace NuGenBioChem.Visualization
{

    /// <summary>
    /// Represents renderer of the substance
    /// </summary>
    public class Render : IDisposable
    {
        #region Events

        /// <summary>
        /// Occurs when rendered content needs to be refreshed
        /// </summary>
        public event EventHandler Invalidated;

        // Raises the Invalidated event
        void RaiseInvalidated()
        {
            if (Invalidated != null) Invalidated(this, EventArgs.Empty);
        }

        #endregion

        #region Static Properties

        // Determines whether hardware is compatible
        static bool hardwareCompatible = false;

        /// <summary>
        /// Gets whether hardware compatible
        /// </summary>
        public static bool IsHardwareCompatible
        {
            get
            {
                if (!hardwareCompatible) hardwareCompatible = WhetherHardwareCompatible();
                return hardwareCompatible;
            }
        }

        #endregion

        #region Native Methods

        [DllImport("NuGenBioChem.Native.dll")]
        static extern IntPtr CreateRender();

        [DllImport("NuGenBioChem.Native.dll")]
        static extern void DisposeRender(IntPtr handle);

        [DllImport("NuGenBioChem.Native.dll", EntryPoint = "IsHardwareCompatible")]
        static extern bool WhetherHardwareCompatible();

        [DllImport("NuGenBioChem.Native.dll")]
        static extern void RenderTo(IntPtr renderHandle, IntPtr renderTargetHandle, IntPtr viewMatrix, IntPtr projectionMatrix);

        [DllImport("NuGenBioChem.Native.dll")]
        static extern void SetAtoms(IntPtr renderHandle, IntPtr atoms, int atomCount);

        [DllImport("NuGenBioChem.Native.dll")]
        static extern void SetBonds(IntPtr renderHandle, IntPtr bonds, int bondCount);

        [DllImport("NuGenBioChem.Native.dll")]
        static extern void SetResidues(IntPtr renderHandle, IntPtr resedues, int reseduesCount);

        [DllImport("NuGenBioChem.Native.dll")]
        static extern void SetBondRenderOptions(IntPtr renderHandle, IntPtr bondRenderOptionsPointer);

        [DllImport("NuGenBioChem.Native.dll")]
        static extern void SetElementMaterials(IntPtr renderHandle, IntPtr materials, int materialCount);

        [DllImport("NuGenBioChem.Native.dll")]
        static extern void SetResidueMaterials(IntPtr renderHandle, IntPtr materials, int materialCount);
        
        #endregion

        #region Fields

        // handle of the render 
        IntPtr handle = IntPtr.Zero;

        // Substance & style
        Substance substance = null;
        Style style = null;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the substance
        /// </summary>
        public Substance Substance
        {
            get { return substance; }
        }

        /// <summary>
        /// Gets or sets the style of the substance
        /// </summary>
        public Style Style
        {
            get { return style; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="substance">Substance</param>
        /// <param name="style">Style</param>
        public Render(Substance substance, Style style)
        {
            this.substance = substance;
            this.style = style;
            handle = CreateRender();

            style.ColorStyle.ColorScheme.Invalidated += OnColorSchemeInvalidated;
            style.ColorStyle.Invalidated += OnColorStyleInvalidated;
            style.GeometryStyle.Invalidated += OnGeometryStyleInvalidated;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~Render()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes unmanaged resources
        /// </summary>
        public void Dispose()
        {
            if (handle == IntPtr.Zero) return;
            
            GC.SuppressFinalize(this);
            DisposeRender(handle);
            handle = IntPtr.Zero;
        }

        #endregion

        #region Data Event Handlers

        // Anything has been changed in color style
        void OnColorStyleInvalidated(object sender, EventArgs e)
        {
            bondRenderOptionsUploaded = false;
            elementMaterialsUploaded = false;
            residuesMaterialsUploaded = false;
            RaiseInvalidated();
        }

        // Anything has been changed in color scheme
        void OnColorSchemeInvalidated(object sender, EventArgs e)
        {
            elementMaterialsUploaded = false;
            RaiseInvalidated();
        }

        // Anything has been changed in geometry style
        void OnGeometryStyleInvalidated(object sender, EventArgs e)
        {
            atomsUploaded = false;
            bondRenderOptionsUploaded = false;
            residuesUploaded = false;
            RaiseInvalidated();
        }

        #endregion

        #region Methods

        

        // Handles changes in atoms
        void OnAtomsChanged()
        {
            bounds = Rect3D.Empty;
            atomsUploaded = false;
        }

        #region GetAtomActualRadius

        /// <summary>
        /// Gets atom actual radius according its element type and geometric style parameters
        /// </summary>
        /// <param name="atom">Atom</param>
        /// <returns>Radius</returns>
        public double GetAtomActualRadius(Data.Atom atom)
        {
            if (style == null) return 1.0;
            GeometryStyle geometryStyle = style.GeometryStyle;

            // Calc atom radius using geometric style
            switch (geometryStyle.AtomSizeStyle)
            {
                case AtomSizeStyle.VanderWaals:
                    return atom.Element.VanderWaalsRadius * geometryStyle.AtomSize;
                case AtomSizeStyle.Uniform:
                    return 0.35 * geometryStyle.AtomSize;
                case AtomSizeStyle.Empirical:
                    return atom.Element.EmpiricalRadius * geometryStyle.AtomSize;
                case AtomSizeStyle.Covalent:
                    return atom.Element.CovalentRadius * geometryStyle.AtomSize;
                case AtomSizeStyle.Calculated:
                    return atom.Element.CalculatedRadius * geometryStyle.AtomSize;
            }

            return 1.0;
        }

        #endregion

        #region Bounds

        // Current axis aligned box approximately contained all atoms
        Rect3D bounds = Rect3D.Empty;

        /// <summary>
        /// Gets an axis aligned box approximately contained all atoms
        /// </summary>
        public Rect3D Bounds
        {
            get
            {
                if (bounds.IsEmpty)
                {
                    // Find a first atom
                    bool isFirstAtom = true;
                    foreach (var molecule in substance.Molecules)
                        foreach (var atom in molecule.Atoms)
                        {
                            double actualRadius = GetAtomActualRadius(atom);
                            Rect3D boundingBox = new Rect3D(
                                atom.Position.X - actualRadius,
                                atom.Position.Y - actualRadius,
                                atom.Position.Z - actualRadius,
                                actualRadius * 2.0,
                                actualRadius * 2.0,
                                actualRadius * 2.0);
                            if (isFirstAtom)
                            {
                                bounds = boundingBox;
                                isFirstAtom = false;
                            }
                            else
                            {
                                bounds.Union(boundingBox);
                            }
                        }
                }
                return bounds;
            }
        }

        #endregion


        #endregion

        #region Rendering

        // We have to upload atom's data
        bool atomsUploaded = false;
        // We have to upload bonds's data
        bool bondsUploaded = false;
        // We have to upload bonds render options
        bool bondRenderOptionsUploaded = false;
        // We have to upload elements materials data
        bool elementMaterialsUploaded = false;
        // We have to upload ribbons's data
        bool residuesUploaded = false;
        // We have to upload ribbons's materials data
        bool residuesMaterialsUploaded = false;

        /// <summary>
        /// Renders the content to the render target
        /// </summary>
        /// <param name="renderTarget">Render target</param>
        /// <param name="camera">Camera</param>
        public void Draw(RenderTarget renderTarget, Camera camera)
        {
            // Upload atom's data
            if (!atomsUploaded) { UploadAtoms(); atomsUploaded = true; }
            // Upload bonds's data
            if (!bondsUploaded) { UploadBonds(); bondsUploaded = true; }
            // Upload bonds's render options
            if (!bondRenderOptionsUploaded) { UploadBondRenderOptions(); bondRenderOptionsUploaded = true; }
            // Upload elements materials data
            if (!elementMaterialsUploaded) { UploadElementMaterials(); elementMaterialsUploaded = true; }
            // Upload ribbons data
            if (!residuesUploaded) { UploadResidues(); residuesUploaded = true; }
            // Upload ribbons material's data
            if (!residuesMaterialsUploaded) { UploadResidueMaterials(); residuesMaterialsUploaded = true; }

            // Rendering
            Matrix3D viewMatrix = camera.View;
            Matrix3D projectionMatrix = camera.Projection;

            IntPtr viewMatrixPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Matrix3D)));
            IntPtr projectionMatrixPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Matrix3D)));
            Marshal.StructureToPtr(viewMatrix, viewMatrixPointer, false);
            Marshal.StructureToPtr(projectionMatrix, projectionMatrixPointer, false);

            try
            {
                RenderTo(handle, renderTarget.Handle, viewMatrixPointer, projectionMatrixPointer);
            }
            finally
            {
                Marshal.FreeHGlobal(viewMatrixPointer);
                Marshal.FreeHGlobal(projectionMatrixPointer);
            }
            
            
        }

        #endregion

        #region Private Methods

        #region Ribbon Uploding

        [StructLayout(LayoutKind.Sequential)]
        struct ResidueRenderData
        {
            public IntPtr VerticesPointer;
            public Int32 VerticesCount;
            public IntPtr IndicesPointer;
            public Int32 IndicesCount;
            public Byte Material;
        }

        void UploadResidues()
        {
            int ribbonCount = substance.Molecules.Sum(x => x.Chains.Sum(y => y.Residues.Count));

            ResidueRenderData[] residues = new ResidueRenderData[ribbonCount];
            int residueIndex = 0;
            foreach (Data.Molecule molecule in substance.Molecules)
            {
                foreach (Data.Chain chain in molecule.Chains)
                {
                    Chain visualChain = new Chain();
                    visualChain.Data = chain;


                    foreach (Residue residue in visualChain.Children)
                    {
                        ResidueRenderData data = new ResidueRenderData();
                        
                        // Generate ribbon render data
                        // Set material index
                        switch (residue.Data.GetStructureType())
                        {
                            case SecondaryStructureType.Sheet: data.Material = 1; break;
                            case SecondaryStructureType.Helix: data.Material = 2; break;    
                            default: data.Material = 0; break;
                        }

                        // FIXME: temporary code to avoid style=null case
                        residue.Style = style;

                        // Build mesh data
                        float[] vertices;
                        short[] indices;
                        residue.ExtractMeshData(out vertices, out indices);

                        GCHandle gcHandleVertices = GCHandle.Alloc(vertices, GCHandleType.Pinned);
                        GCHandle gcHandleIndices = GCHandle.Alloc(indices, GCHandleType.Pinned);
                        data.VerticesPointer = gcHandleVertices.AddrOfPinnedObject();
                        data.IndicesPointer = gcHandleIndices.AddrOfPinnedObject();
                        data.VerticesCount = vertices.Length / 8;
                        data.IndicesCount = indices.Length;

                        // FIXME: we will have to release gcHandles when it won't be used (!)

                        residues[residueIndex] = data;
                        residueIndex++;   
                    }
                }
            }

            // Sort by materials to improve performance
            Array.Sort(residues, (a, b) => a.Material.CompareTo(b.Material));

            // Upload data
            GCHandle gcHandle = GCHandle.Alloc(residues, GCHandleType.Pinned);
            try { SetResidues(handle, gcHandle.AddrOfPinnedObject(), residues.Length); }
            finally { gcHandle.Free(); }
        }
        
        #endregion

        #region Atom Uploding

        [StructLayout(LayoutKind.Sequential)]
        struct AtomRenderData
        {
            // Position of center of atom
            public float X,Y,Z;
            // Radius of the atom
            public float Radius;
            // Index of the material
            public byte Material;

            /// <summary>
            /// Returns the fully qualified type name of this instance.
            /// </summary>
            /// <returns>A string containing a fully qualified type name</returns>
            public override string ToString()
            {
                return String.Format("({0}, {1}, {2}), R={3:0.00}, Material={4}", X, Y, Z, Radius, Material);
            }
        }

        void UploadAtoms()
        {
            int atomCount = substance.Molecules.Sum(x => x.Atoms.Count);

            AtomRenderData[] atoms = new AtomRenderData[atomCount];
            int atomIndex = 0;
            foreach (Data.Molecule molecule in substance.Molecules)
            {
                foreach (Data.Atom atom in molecule.Atoms)
                {
                    atoms[atomIndex].X = (float)atom.Position.X;
                    atoms[atomIndex].Y = (float)atom.Position.Y;
                    atoms[atomIndex].Z = (float)atom.Position.Z;
                    atoms[atomIndex].Radius = (float)GetAtomActualRadius(atom);
                    atoms[atomIndex].Material = IndexOfElement(atom.Element);
                    atomIndex++;
                }
            }

            // Sort by materials to improve performance
            // NOTE: sorting can be made in more optimal way
            Array.Sort(atoms, (a, b) => a.Material.CompareTo(b.Material));

            // Upload data
            GCHandle atomsHandle = GCHandle.Alloc(atoms, GCHandleType.Pinned);
            try{ SetAtoms(handle, atomsHandle.AddrOfPinnedObject(), atomCount); }
            finally { atomsHandle.Free(); }
        }

        static byte IndexOfElement(Element element)
        {
            for (int i = 0; i < Element.Elements.Length; i++)
            {
                if (Element.Elements[i] == element) return (byte)i;
            }
            return 0;
        }

        #endregion

        #region Bonds Uploading

        [StructLayout(LayoutKind.Sequential)]
        struct BondRenderData
        {
            // Begin of the bond
            public float BeginX, BeginY, BeginZ;
            // End of the bond
            public float EndX, EndY, EndZ;
            // Index of the material
            public byte Material;
        }

        void UploadBonds()
        {
            int bondCount = substance.Molecules.Sum(x => x.Bonds.Count);
            bool bondsSeparated = !style.ColorStyle.UseSingleBondMaterial;


            BondRenderData[] bonds = new BondRenderData[bondCount * (bondsSeparated ? 2 : 1)];
            int bondIndex = 0;
            foreach (Data.Molecule molecule in substance.Molecules)
            {
                foreach (Data.Bond bond in molecule.Bonds)
                {
                    if (bondsSeparated)
                    {
                        float xMiddle = (float)(bond.Begin.Position.X + bond.End.Position.X) / 2.0f;
                        float yMiddle = (float)(bond.Begin.Position.Y + bond.End.Position.Y) / 2.0f;
                        float zMiddle = (float)(bond.Begin.Position.Z + bond.End.Position.Z) / 2.0f;
                        bonds[bondIndex].BeginX = (float)bond.Begin.Position.X;
                        bonds[bondIndex].BeginY = (float)bond.Begin.Position.Y;
                        bonds[bondIndex].BeginZ = (float)bond.Begin.Position.Z;
                        bonds[bondIndex].EndX = xMiddle;
                        bonds[bondIndex].EndY = yMiddle;
                        bonds[bondIndex].EndZ = zMiddle;
                        bonds[bondIndex].Material = IndexOfElement(bond.Begin.Element);
                        bondIndex++;


                        bonds[bondIndex].BeginX = xMiddle;
                        bonds[bondIndex].BeginY = yMiddle;
                        bonds[bondIndex].BeginZ = zMiddle;
                        bonds[bondIndex].EndX = (float)bond.End.Position.X;
                        bonds[bondIndex].EndY = (float)bond.End.Position.Y;
                        bonds[bondIndex].EndZ = (float)bond.End.Position.Z;
                        bonds[bondIndex].Material = IndexOfElement(bond.End.Element);
                        bondIndex++;
                    }
                    else
                    {
                        bonds[bondIndex].BeginX = (float)bond.Begin.Position.X;
                        bonds[bondIndex].BeginY = (float)bond.Begin.Position.Y;
                        bonds[bondIndex].BeginZ = (float)bond.Begin.Position.Z;
                        bonds[bondIndex].EndX = (float)bond.End.Position.X;
                        bonds[bondIndex].EndY = (float)bond.End.Position.Y;
                        bonds[bondIndex].EndZ = (float)bond.End.Position.Z;
                        bonds[bondIndex].Material = 0;
                        bondIndex++;
                    }
                }
            }

            // Sort by materials to improve performance
            // NOTE: sorting can be made in more optimal way
            Array.Sort(bonds, (a, b) => a.Material.CompareTo(b.Material));

            // Upload data
            GCHandle bondsHandle = GCHandle.Alloc(bonds, GCHandleType.Pinned);
            try { SetBonds(handle, bondsHandle.AddrOfPinnedObject(), bonds.Length); }
            finally { bondsHandle.Free(); }
        }

        #endregion

        #region Bond Render Options Uploading

        [StructLayout(LayoutKind.Sequential)]
        struct BondRenderOptions
        {
            // Size of bonds
            public float BondSize;
            // Material of the bonds
            public MaterialRenderData Material;
            // Whether we heve to use single material
            // (as boolean value)
            public byte UseSingleMaterial;
        }

        void UploadBondRenderOptions()
        {
            BondRenderOptions options = new BondRenderOptions();
            options.BondSize = 0.35f * (float)(style.GeometryStyle.BondSize * style.GeometryStyle.AtomSize);
            options.UseSingleMaterial = (byte)(style.ColorStyle.UseSingleBondMaterial ? 1 : 0);
            options.Material = GetMaterialRenderData(style.ColorStyle.BondMaterial);

            // Upload data
            GCHandle gcHandle = GCHandle.Alloc(options, GCHandleType.Pinned);
            try { SetBondRenderOptions(handle, gcHandle.AddrOfPinnedObject()); }
            finally { gcHandle.Free(); }
        }

        #endregion

        #region Material Uploading
        
        struct MaterialRenderData
        {
            // Colors
            public Int32 Ambient;
            public Int32 Diffuse;
            public Int32 Specular;
            public Int32 Emissive;

            // Parameters
            public float Glossiness;
            public float SpecularPower;
            public float ReflectionLevel;
            public float BumpLevel;
            public float EmissiveLevel;
        }

        void UploadElementMaterials()
        {
            MaterialRenderData[] materialArray = new MaterialRenderData[Element.Elements.Length];

            for (int i = 0; i < materialArray.Length; i++)
            {
                Material material = style.ColorStyle.ColorScheme[Element.Elements[i]];
                materialArray[i] = GetMaterialRenderData(material);
            }

            GCHandle arrayHandle = GCHandle.Alloc(materialArray, GCHandleType.Pinned);
            try { SetElementMaterials(handle, arrayHandle.AddrOfPinnedObject(), materialArray.Length); }
            finally { arrayHandle.Free(); }
        }

        void UploadResidueMaterials()
        {
            MaterialRenderData[] materialArray = new MaterialRenderData[3];

            materialArray[0] = GetMaterialRenderData(style.ColorStyle.TurnMaterial);
            materialArray[1] = GetMaterialRenderData(style.ColorStyle.SheetMaterial);
            materialArray[2] = GetMaterialRenderData(style.ColorStyle.HelixMaterial);
            
            
            GCHandle arrayHandle = GCHandle.Alloc(materialArray, GCHandleType.Pinned);
            try { SetResidueMaterials(handle, arrayHandle.AddrOfPinnedObject(), materialArray.Length); }
            finally { arrayHandle.Free(); }
        }

        static Int32 ToInt32(Color color)
        {
            return BitConverter.ToInt32(new byte[] {color.B, color.G, color.R, color.A}, 0);
        }

        static MaterialRenderData GetMaterialRenderData(Material material)
        {
            MaterialRenderData data = new MaterialRenderData
            {
                Ambient = ToInt32(material.Ambient),
                Diffuse = ToInt32(material.Diffuse),
                Specular = ToInt32(material.Specular),
                Emissive = ToInt32(material.Emissive),
                Glossiness = (float)material.Glossiness,
                SpecularPower = (float)material.SpecularPower,
                ReflectionLevel = (float)material.ReflectionLevel,
                BumpLevel = (float)material.BumpLevel,
                EmissiveLevel = (float)material.EmissiveLevel
            };

            return data;
        }

        #endregion

        #endregion
    }
}

