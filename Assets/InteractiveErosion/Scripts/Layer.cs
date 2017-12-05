using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace InterativeErosionProject
{
    class Layer
    {
        public static RenderTexture[] CreateDouble(string name, int size, RenderTextureFormat format, FilterMode filterMode)
        {
            RenderTexture[] res = new RenderTexture[2];
            for (int i = 0; i < 2; i++)
            {
                res[i] = new RenderTexture(size, size, 0, format);
                res[i].wrapMode = TextureWrapMode.Clamp;
                res[i].filterMode = filterMode;
                res[i].name = name + " " + i;
            }
            return res;
        }
        public static RenderTexture Create(string name, int size, RenderTextureFormat format, FilterMode filterMode)
        {
            var res = new RenderTexture(size, size, 0, format);
            res.wrapMode = TextureWrapMode.Clamp;
            res.filterMode = filterMode;
            res.name = name;

            return res;
        }
    }
}
