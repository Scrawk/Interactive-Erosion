using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.VersionControl;
using UnityEngine;

namespace InterativeErosionProject
{
    /// <summary>
    /// Encapsulates operations with 1 data matrix (has 2 textures for write-read operations)
    /// </summary>
    public class DataTexture
    {
        static private readonly List<DataTexture> all = new List<DataTexture>();
        static private readonly RenderTexture tempRTARGB, tempRTRFloat;
        static private Texture2D tempT2DRGBA, tempT2DRFloat;
        static private Material setFloatValueMat, changeValueMat, changeValueZeroControlMat, getValueMat, changeValueGaussMat, changeValueGaussZeroControlMat;
        ///<summary> Contains data</summary>
        private readonly RenderTexture[] textures = new RenderTexture[2];
        private readonly int size;
        public RenderTexture READ
        {
            get { return textures[0]; }
        }
        public RenderTexture WRITE
        {
            get { return textures[1]; }
        }
        static DataTexture()
        {
            tempRTARGB = DataTexture.Create("tempRTARGB", 1, RenderTextureFormat.ARGBFloat, FilterMode.Point);

            tempT2DRGBA = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
            tempT2DRGBA.wrapMode = TextureWrapMode.Clamp;
            tempT2DRGBA.filterMode = FilterMode.Point;

            tempRTRFloat = DataTexture.Create("tempRTRFloat", 1, RenderTextureFormat.RFloat, FilterMode.Point);

            tempT2DRFloat = new Texture2D(1, 1, TextureFormat.RFloat, false);
            tempT2DRFloat.wrapMode = TextureWrapMode.Clamp;
            tempT2DRFloat.filterMode = FilterMode.Point;
            LoadMaterials();
        }
        
        public DataTexture(string name, int size, RenderTextureFormat format, FilterMode filterMode)
        {

            for (int i = 0; i < 2; i++)
            {
                textures[i] = new RenderTexture(size, size, 0, format);
                textures[i].wrapMode = TextureWrapMode.Clamp;
                textures[i].filterMode = filterMode;
                textures[i].name = name + " " + i;
            }
            this.size = size;
            all.Add(this);
        }
        static private void LoadMaterials()
        {
            setFloatValueMat = Resources.Load("Materials/UniversalCS/SetFloatValue", typeof(Material)) as Material;
            changeValueMat = Resources.Load("Materials/UniversalCS/ChangeValue", typeof(Material)) as Material;
            changeValueZeroControlMat = Resources.Load("Materials/UniversalCS/ChangeValueZeroControl", typeof(Material)) as Material;
            getValueMat = Resources.Load("Materials/UniversalCS/GetValue", typeof(Material)) as Material;
            changeValueGaussMat = Resources.Load("Materials/UniversalCS/ChangeValueGauss", typeof(Material)) as Material;
            changeValueGaussZeroControlMat = Resources.Load("Materials/UniversalCS/ChangeValueGaussZeroControl", typeof(Material)) as Material;
        }
        public static void DestroyAll()
        {
            foreach (var item in all)
            {
                item.Destroy();
            }
            all.Clear();
        }
        //public static DataTexture CreateDouble(string name, int size, RenderTextureFormat format, FilterMode filterMode)
        //{
        //    //RenderTexture[] res = new RenderTexture[2];
        //    //for (int i = 0; i < 2; i++)
        //    //{
        //    //    res[i] = new RenderTexture(size, size, 0, format);
        //    //    res[i].wrapMode = TextureWrapMode.Clamp;
        //    //    res[i].filterMode = filterMode;
        //    //    res[i].name = name + " " + i;
        //    //}
        //    //return res;
        //    return new DataTexture( name,  size,  format,  filterMode);
        //}
        public static RenderTexture Create(string name, int size, RenderTextureFormat format, FilterMode filterMode)
        {
            var res = new RenderTexture(size, size, 0, format);
            res.wrapMode = TextureWrapMode.Clamp;
            res.filterMode = filterMode;
            res.name = name;
            res.Create();

            return res;
        }
        public void Destroy()
        {
            UnityEngine.Object.Destroy(textures[0]);
            UnityEngine.Object.Destroy(textures[1]);
        }
        public int getMaxIndex()
        {
            return size - 1;
        }
        public Vector4 getDataRGBAFloatEF(Vector2 point)
        {
            Graphics.CopyTexture(this.READ, 0, 0, (int)(point.x * getMaxIndex()), (int)(point.y * getMaxIndex()), 1, 1, tempRTARGB, 0, 0, 0, 0);
            tempT2DRGBA = GetRTPixels(tempRTARGB, tempT2DRGBA);
            var res = tempT2DRGBA.GetPixel(0, 0);
            return res;
        }
        public Vector4 getDataRFloatEF(Point point)
        {
            Graphics.CopyTexture(this.READ, 0, 0, point.x, point.y, 1, 1, tempT2DRFloat, 0, 0, 0, 0);
            var del = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGBHalf);
            del.wrapMode = TextureWrapMode.Clamp;
            del.filterMode = FilterMode.Point;
            del.Create();
            Graphics.ConvertTexture(tempRTRFloat, del);

            tempT2DRGBA = GetRTPixels(tempRTARGB, tempT2DRGBA);
            var res = tempT2DRGBA.GetPixel(0, 0);
            //tempT2DRFloat = GetRTPixels(tempRTRFloat, tempT2DRFloat);
            //var res = tempT2DRFloat.GetPixel(0, 0);
            return res;
        }


        /// Not supported by unity
        //public float getDataRGHalf(RenderTexture source, Point point)
        //{
        //    bufferRGHalfTexture = GetRTPixels(source, bufferRGHalfTexture);
        //    var res = bufferRGHalfTexture.GetPixel(point.x, point.y);
        //    return res.a;
        //}
        // Get the contents of a RenderTexture into a Texture2D
        static public Texture2D GetRTPixels(RenderTexture source, Texture2D destination)
        {
            // Remember currently active render texture
            RenderTexture currentActiveRT = RenderTexture.active;

            // Set the supplied RenderTexture as the active one
            RenderTexture.active = source;

            // Create a new Texture2D and read the RenderTexture image into it
            //Texture2D tex = new Texture2D(source.width, source.height);
            destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);

            // Restore previously active render texture
            RenderTexture.active = currentActiveRT;
            return destination;
        }
        public void SetValue(Vector4 value, Rect rect)
        {
            //Graphics.Blit(field.READ, field.WRITE);
            setFloatValueMat.SetVector("_Value", value);
            RTUtility.Blit(this.READ, this.WRITE, setFloatValueMat, rect, 0, false);
            this.Swap();
        }
        public void ChangeValueGauss(Vector2 point, float radius, float amount, Vector4 layerMask)
        {
            if (amount != 0f)
            {
                changeValueGaussMat.SetVector("_Point", point);
                changeValueGaussMat.SetFloat("_Radius", radius);
                changeValueGaussMat.SetFloat("_Amount", amount);
                changeValueGaussMat.SetVector("_LayerMask", layerMask);

                Graphics.Blit(this.READ, this.WRITE, changeValueGaussMat);
                this.Swap();
            }
        }

        public void ChangeValueGaussZeroControl(Vector2 point, float radius, float amount, Vector4 layerMask)
        {
            if (amount != 0f)
            {
                changeValueGaussZeroControlMat.SetVector("_Point", point);
                changeValueGaussZeroControlMat.SetFloat("_Radius", radius);
                changeValueGaussZeroControlMat.SetFloat("_Amount", amount);
                changeValueGaussZeroControlMat.SetVector("_LayerMask", layerMask);

                Graphics.Blit(this.READ, this.WRITE, changeValueGaussZeroControlMat);
                this.Swap();
            }
        }
        public void ChangeValue(Vector4 value, Rect rect)
        {
            //Graphics.Blit(field.READ, field.WRITE); // don't know why but need it
            changeValueMat.SetVector("_Value", value);
            RTUtility.Blit(this.READ, this.WRITE, changeValueMat, rect, 0, false);
            this.Swap();
        }
        public void ChangeValueZeroControl(float value, Rect rect)
        {
            changeValueZeroControlMat.SetFloat("_Value", value );
            Graphics.Blit(this.READ, this.WRITE, changeValueZeroControlMat);
            this.Swap();
        }

        internal void Swap()
        {
            RenderTexture temp = textures[0];
            textures[0] = textures[1];
            textures[1] = temp;
        }

        internal void ClearColor()
        {
            //if (texture == null) return;
            //if (!SystemInfo.SupportsRenderTextureFormat(texture.format)) return;

            //Graphics.SetRenderTarget(texture);
            //GL.Clear(false, true, Color.clear);
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] == null) continue;
                if (!SystemInfo.SupportsRenderTextureFormat(textures[i].format)) continue;

                Graphics.SetRenderTarget(textures[i]);
                GL.Clear(false, true, Color.clear);
            }
        }
        public void SetFilterMode(FilterMode mode)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i].filterMode = mode;
            }
        }
    }
}
