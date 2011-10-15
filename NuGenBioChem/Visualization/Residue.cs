using System;
using System.Collections.Generic; 
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using NuGenBioChem.Data;
using NuGenBioChem.Visualization.Primitives;
using Material = System.Windows.Media.Media3D.Material;

namespace NuGenBioChem.Visualization
{    
    /// <summary>
    /// Visual representation of an amino acid residue
    /// </summary>
    public class Residue : ModelVisual3D
    {
        #region Static Fields
        
        /// <summary>
        /// Normal Sheet Width
        /// </summary>
        public static double SheetWidth = 1.2;

        /// <summary>
        /// Normal Sheet Height
        /// </summary>
        public static double SheetHeight = 0.25;

        /// <summary>
        /// Normal Arrow Width
        /// </summary>
        public static double ArrowWidth = 2.4;

        /// <summary>
        /// Normal Turn Width
        /// </summary>
        public static double TurnWidth = 0.5;

        /// <summary>
        /// Normal Turn Height
        /// </summary>
        public static double TurnHeight = 0.5;

        /// <summary>
        /// Normal Helix Width
        /// </summary>
        public static double HelixWidth = 2.6;

        /// <summary>
        /// Normal Helix Height
        /// </summary>
        public static double HelixHeight = 0.5;

        #endregion

        #region Fields

        // Chain where residue is
        Chain chain = null;
        Data.Style style = null;
        // Residue material
        Material material = null;
    
        // Representation of the residue
        ExtrudedSurface primitive = null;
        Primitive upperCap = null;
        Primitive lowerCap = null;

        #endregion

        #region Properties

        #region Data

        /// <summary>
        /// Data of the residue
        /// </summary>
        public Data.Residue Data
        {
            get 
            {
                return this.chain != null ? this.chain.GetResidueData(this) : null;
            }
        }

        #endregion

        #region Style

        /// <summary>
        /// Gets or sets style of the molecule
        /// </summary>
        public Data.Style Style
        {
            get { return style; }
            set
            {
                if (style == value) return;
                if (style != null)
                {
                    style.GeometryStyle.PropertyChanged -= OnGeometryStyleChanged;
                }
                style = value;
                if (style != null)
                {
                    style.GeometryStyle.PropertyChanged += OnGeometryStyleChanged;
                    UpdateVisualModel();
                }
            }
        }

        void OnGeometryStyleChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Data == null) return;
            Data.SecondaryStructureType type = Data.GetStructureType();

            if (type == SecondaryStructureType.Sheet && 
                (e.PropertyName == "SheetWidth" || 
                e.PropertyName == "SheetHeight" || 
                e.PropertyName == "ArrowWidth")) UpdateVisualModel();

            if (type == SecondaryStructureType.Helix &&
                (e.PropertyName == "HelixWidth" ||
                e.PropertyName == "HelixHeight")) UpdateVisualModel();

            if (type == SecondaryStructureType.NotDefined &&
                (e.PropertyName == "TurnWidth" ||
                e.PropertyName == "TurnHeight")) UpdateVisualModel();
        }

        #endregion

        #region Chain

        /// <summary>
        /// Gets or sets the chain where residue is
        /// Chain must contain the residue
        /// </summary>
        public Chain Chain
        {
            get { return this.chain; }
            set
            {
                if (this.chain != null && this.Data != null) this.Data.PropertyChanged -= OnDataChanged;
                this.chain = value;
                if (this.chain != null && this.Data != null)
                {
                    this.Data.PropertyChanged += OnDataChanged;  
                    UpdateVisualModel();
                }
            }
        }

        #endregion

        #region Material

        /// <summary>
        /// Gets or sets the material of the residue
        /// </summary>
        public Material Material
        {
            get { return this.material; }
            set
            {
                this.material = value;
                if (this.primitive != null) this.primitive.Material = value;
                if (this.lowerCap != null) this.lowerCap.Material = value;
                if (this.upperCap != null) this.upperCap.Material = value;
            }
        }

        #endregion

        #endregion

        #region Initialization

        /// <summary>
        /// Default constructor
        /// </summary>
        public Residue() {}
        
        #endregion

        #region Methods

        #region Extraction Mesh Data

        /// <summary>
        /// Extracts mesh data from this residue
        /// </summary>
        /// <param name="vertices">Vertices (px,py,pz,nx,ny,nz)</param>
        /// <param name="indices">Indices</param>
        public void ExtractMeshData(out float[] vertices, out short[] indices)
        {
            vertices = new float[(ExtractMeshGeometry3D(primitive).Positions.Count + ExtractMeshGeometry3D(lowerCap).Positions.Count + ExtractMeshGeometry3D(upperCap).Positions.Count) * 8 /*(px, py, pz, nx, ny, nz)*/];
            indices = new short[ExtractMeshGeometry3D(primitive).TriangleIndices.Count + ExtractMeshGeometry3D(lowerCap).TriangleIndices.Count + ExtractMeshGeometry3D(upperCap).TriangleIndices.Count];
            int verticesOffset = 0, indicesOffset = 0, verticesAdded, indicesAdded;

            AddMeshData(primitive, vertices, indices, verticesOffset, indicesOffset, out verticesAdded, out indicesAdded);
            verticesOffset += verticesAdded;
            indicesOffset += verticesAdded;
            AddMeshData(lowerCap, vertices, indices, verticesOffset, indicesOffset, out verticesAdded, out indicesAdded);
            verticesOffset += verticesAdded;
            indicesOffset += indicesAdded;
            AddMeshData(upperCap, vertices, indices, verticesOffset, indicesOffset, out verticesAdded, out indicesAdded);

        }

        void AddMeshData(Primitive surface, float[] vertices, short[] indices, int verticesOffset, int indicesOffset, out int verticesAdded, out int indicesAdded)
        {
            MeshGeometry3D mesh = ExtractMeshGeometry3D(surface);
            verticesAdded = mesh.Positions.Count;
            indicesAdded = mesh.TriangleIndices.Count;

            for (int i = 0; i < verticesAdded; i++)
            {
                int offset = i * 8 + verticesOffset * 8;
                vertices[offset + 0] = (float)mesh.Positions[i].X;
                vertices[offset + 1] = (float)mesh.Positions[i].Y;
                vertices[offset + 2] = (float)mesh.Positions[i].Z;
                vertices[offset + 3] = (float)mesh.Normals[i].X;
                vertices[offset + 4] = (float)mesh.Normals[i].Y;
                vertices[offset + 5] = (float)mesh.Normals[i].Z;
                vertices[offset + 6] = mesh.TextureCoordinates.Count == 0 ? 0.0f : (float)mesh.TextureCoordinates[i].X;
                vertices[offset + 7] = mesh.TextureCoordinates.Count == 0 ? 0.0f : (float)mesh.TextureCoordinates[i].Y;
            }

            for (int i = 0; i < indicesAdded; i++)
            {
                indices[i + indicesOffset + 0] = (short)mesh.TriangleIndices[i];
            }
        }

        MeshGeometry3D ExtractMeshGeometry3D(Primitive surface)
        {
            if (surface == null) UpdateVisualModel();
            if (surface == null) return new MeshGeometry3D();
            if (surface.Geometry == null) return new MeshGeometry3D();
            return surface.Geometry as MeshGeometry3D;
        }

        #endregion



        void OnDataChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Material")
            {
                this.Material = this.Data.Material;
            }
            else
            {
                UpdateVisualModel();
            }
        }

        void UpdateVisualModel()
        {
            if (style == null || Data == null) return;

            this.primitive = null;
            this.upperCap = null;
            this.lowerCap = null;
            this.Children.Clear();

            if (this.chain == null) return;

            Point3D[] points;
            Vector3D[] normals;
            Vector3D[] torsions;
            this.chain.Ribbon.GetResidueBackbone(this, out points, out torsions, out normals);

            bool isStructureEnd = this.chain.Ribbon.IsStructureEnd(this);
            bool isStructureBegin = this.chain.Ribbon.IsStructureBegin(this);

            switch (this.Data.GetStructureType())
            {
                case NuGenBioChem.Data.SecondaryStructureType.Sheet:  
                    this.primitive = CreateSheet(points, normals, torsions, isStructureEnd);
                    if (isStructureEnd || isStructureBegin) this.lowerCap = this.primitive.CreateLowerCap();
                    break;
                case NuGenBioChem.Data.SecondaryStructureType.Helix:
                    this.primitive = CreateHelix(points, normals, torsions);
                    if (isStructureEnd) this.upperCap = this.primitive.CreateUpperCap();
                    if (isStructureBegin) this.lowerCap = this.primitive.CreateLowerCap(); 
                    break;
                case NuGenBioChem.Data.SecondaryStructureType.NotDefined:
                    this.primitive = CreateTurn(points, normals, torsions);
                    if (isStructureEnd) this.upperCap = this.primitive.CreateUpperCap();
                    if (isStructureBegin) this.lowerCap = this.primitive.CreateLowerCap(); 
                    break;
            };

            if (this.lowerCap != null)
            {
                this.lowerCap.Material = this.material ?? this.Data.Material;
                this.Children.Add(this.lowerCap);
            }
            if (this.upperCap != null)
            {
                this.upperCap.Material = this.material ?? this.Data.Material;
                this.Children.Add(this.upperCap);
            }
            if (this.primitive != null)
            {
                this.primitive.Material = this.material ?? this.Data.Material;  
                this.Children.Add(this.primitive);
            }
        }

        Tube CreateTurn(Point3D[] points, Vector3D[] normals, Vector3D[] torsions)
        {
            Tube tube = new Tube();
            tube.Centers = points;
            tube.VerticalVectors = normals;
            tube.HorizontalVectors = torsions;
            tube.Width = Residue.TurnWidth * style.GeometryStyle.TurnWidth;
            tube.Height = Residue.TurnHeight * style.GeometryStyle.TurnHeight;
            return tube;
        }

        Tube CreateHelix(Point3D[] points, Vector3D[] normals, Vector3D[] torsions)
        {
            Tube tube = new Tube();
            tube.Centers = points;
            tube.VerticalVectors = normals;
            tube.HorizontalVectors = torsions;
            tube.Width = Residue.HelixWidth * style.GeometryStyle.HelixWidth;
            tube.Height = Residue.HelixHeight * style.GeometryStyle.HelixHeight;
            return tube;
        }

        Sheet CreateSheet(Point3D[] points, Vector3D[] normals, Vector3D[] torsions, bool isArrow)
        {
            Sheet result = new Sheet();
            result.Centers = points;
            result.VerticalVectors = normals;
            result.HorizontalVectors = torsions;
            result.IsArrow = isArrow;
            result.Height = Residue.SheetHeight * style.GeometryStyle.SheetHeight;
            result.Width = isArrow ? 
                Residue.ArrowWidth * style.GeometryStyle.ArrowWidth : 
                Residue.SheetWidth * style.GeometryStyle.SheetWidth;
            return result;
        }


        #endregion
    }
}
