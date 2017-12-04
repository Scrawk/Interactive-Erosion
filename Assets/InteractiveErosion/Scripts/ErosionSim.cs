// good browni 51381BFF
// good red 552710FF


// add texture of rain and evaporation amount? Will give oceans? No, it wouldn't
// add multiple water sources than?

// tilt angel breaks erosion? - min title did
// add lava
// add layers of material on init?
// InteractiveErosion class

// check meanders
// try grainy map - failed
// check regolith

// simplify model
// make WorldSides class



using UnityEngine;
using System.Collections;

using ImprovedPerlinNoiseProject;
using System;
using System.Collections.Generic;

namespace InterativeErosionProject
{
    public enum NOISE_STYLE { FRACTAL = 0, TURBULENCE = 1, RIDGE_MULTI_FRACTAL = 2, WARPED = 3 };

    public class ErosionSim : MonoBehaviour
    {
        public GameObject m_sun;
        public Material m_landMat, m_waterMat;
        public Material m_initTerrainMat, m_noiseMat;
        public Material m_outFlowMat;
        ///<summary> Updates field according to outflow</summary>
        public Material m_fieldUpdateMat;
        public Material m_waterVelocityMat, m_diffuseVelocityMat;

        /// <summary> Calculates angle for each cell </summary>
        public Material m_tiltAngleMat;
        ///<summary> Calculates layer erosion basing on the forces that are caused by the running water</summary>
        public Material m_processMacCormackMat;
        public Material m_erosionAndDepositionMat, m_advectSedimentMat;
        public Material m_slippageHeightMat, m_slippageOutflowMat, m_slippageUpdateMat;
        public Material m_disintegrateAndDepositMat, m_applyFreeSlipMat;
        public Material setFloatValueMat, changeValueMat, changeValueZeroControlMat, getValueMat, changValueGaussMat, changValueGaussZeroControlMat;


        /// <summary> Movement speed of point of water source</summary>
        //private float m_waterInputSpeed = 0.01f;
        private Vector2 m_waterInputPoint = new Vector2(-1f, 1f);
        private float m_waterInputAmount = 0f;
        private float m_waterInputRadius = 0.008f;

        private Vector2 waterDrainagePoint = new Vector2(-1f, 1f);
        private float waterDrainageAmount = 0f;
        private float waterDrainageRadius = 0.008f;

        private int m_seed = 0;

        //The number of layers used in the simulation. Must be 1, 2, 3 or, 4
        private const int TERRAIN_LAYERS = 4;



        //This will allow you to set a noise style for each terrain layer
        private NOISE_STYLE[] m_layerStyle = new NOISE_STYLE[]
        {
            NOISE_STYLE.FRACTAL,
            NOISE_STYLE.FRACTAL,
            NOISE_STYLE.FRACTAL,
            NOISE_STYLE.FRACTAL
        };
        //This will take the abs value of the final noise is set to true
        //This will make the fractal or warped noise look different.
        //It will have no effect on turbulence or ridged noise as they are all ready abs
        private bool[] m_finalNosieIsAbs = new bool[]
        {
            true,
            false,
            false,
            false
        };
        //Noise settings. Each Component of vector is the setting for a layer
        //ie x is setting for layer 0, y is setting for layer 1 etc

        private Vector4 m_octaves = new Vector4(8, 6, 4, 8); //Higher octaves give more finer detail



        private Vector4 m_frequency = new Vector4(4f, 2f, 2f, 2f); //A lower value gives larger scale details
        private Vector4 m_lacunarity = new Vector4(2.5f, 2.3f, 2.0f, 2.0f); //Rate of change of the noise amplitude. Should be between 1 and 3 for fractal noise
        private Vector4 m_gain = new Vector4(0.5f, 0.5f, 0.5f, 0.5f); //Rate of change of the noise frequency
        static private float terrainAmountScale = 0.3f;
        //private Vector4 m_amp = new Vector4(6.0f * terrainAmountScale, 3f * terrainAmountScale, 6f * terrainAmountScale, 0.15f * terrainAmountScale); //Amount of terrain in a layer        
        private Vector4 m_amp = new Vector4(2f * terrainAmountScale, 1f * terrainAmountScale, 2f * terrainAmountScale, 1f * terrainAmountScale); //Amount of terrain in a layer        

        //original sets by Scrawk
        //public Vector4 m_octaves = new Vector4(8, 8, 8, 8); //Higher octaves give more finer detail
        //public Vector4 m_frequency = new Vector4(2.0f, 100.0f, 200.0f, 200.0f); //A lower value gives larger scale details
        //public Vector4 m_lacunarity = new Vector4(2.0f, 3.0f, 3.0f, 2.0f); //Rate of change of the noise amplitude. Should be between 1 and 3 for fractal noise
        //public Vector4 m_gain = new Vector4(0.5f, 0.5f, 0.5f, 0.5f); //Rate of chage of the noise frequency
        //public Vector4 m_amp = new Vector4(2.0f, 0.01f, 0.01f, 0.001f); //Amount of terrain in a layer


        private Vector4 m_offset = new Vector4(0.0f, 10.0f, 20.0f, 30.0f);

        /// <summary>
        /// The settings for the erosion. If the value is a vector4 each component is for a layer
        /// How easily the layer dissolves
        /// </summary>
        ///  rain
        public Vector4 m_dissolvingConstant = new Vector4(0.001f, 0.002f, 0.008f, 0.012f);
        //private Vector4 m_dissolvingConstant = new Vector4(0.01f, 0.015f, 0.8f, 1.2f);// stream


        /// <summary>The angle that slippage will occur </summary>        
        //private Vector4 m_talusAngle = new Vector4(80f, 35f, 60f, 10f); looked good
        public Vector4 m_talusAngle = new Vector4(70f, 45f, 60f, 30f);

        /// <summary>
        /// A higher value will increase erosion on flat areas
        /// Used as limit for surface tilt
        /// Meaning that even flat area will erode as slightly tilted area
        /// Not used now
        /// </summary>
        public float m_minTiltAngle = 5f;//0.1f;

        /// <summary> How much sediment the water can carry per 1 unit of water </summary>
        public float m_sedimentCapacity = 0.2f;

        /// <summary> Rate the sediment is deposited on top layer </summary>
        public float m_depositionConstant = 0.015f;

        /// <summary> Terrain wouldn't dissolve if water level in cell is lower than this</summary>
        public float dissolveLimit = 0.001f;

        /// <summary>Evaporation rate of water</summary>
        private float m_evaporationConstant = 0.001f;

        /// <summary> Movement speed of point of water source</summary>
        private float m_rainInputAmount = 0.0011f;


        /// <summary>Viscosity of regolith</summary>
        public float m_regolithDamping = 0.85f;

        /// <summary> Viscosity of water</summary>        
        public float m_waterDamping = 0.0f;

        /// <summary>Higher number will increase dissolution rate</summary>
        public float m_maxRegolith = 0.008f;

        public float oceanDestroySedimentsLevel = 0f;
        public float oceanDepth = 4f;
        public float oceanWaterLevel = 20f;
        public int oceanWidth = 110;

        ///<summary> Meshes</summary>
        private GameObject[] m_gridLand, m_gridWater;

        ///<summary> Contains all 4 layers in ARGB</summary>
        public RenderTexture[] m_terrainField;

        ///<summary>sediment transport capacity? How much sediment water can hold?</summary>        
        public RenderTexture[] m_advectSediment;

        ///<summary> Actual amount of dissolved sediment in water</summary>
        public RenderTexture[] m_sedimentField;

        ///<summary> Contains regolith amount.Regolith is quasi-liquid at the bottom of water flow</summary>
        public RenderTexture[] m_regolithField;

        ///<summary> Moved regolith amount in format ARGB : A - flowLeft, R - flowR, G -  flowT, B - flowB</summary>
        public RenderTexture[] m_regolithOutFlow;

        ///<summary> Contains water amount. Can't be negative!!</summary>
        public RenderTexture[] m_waterField;

        ///<summary> Moved water amount in format ARGB : A - flowLeft, R - flowR, G -  flowT, B - flowB</summary>
        public RenderTexture[] m_waterOutFlow;

        ///<summary> Water speed (1 channel)</summary>
        public RenderTexture[] m_waterVelocity;

        ///<summary> Contains surface angels for each point</summary>
        public RenderTexture m_tiltAngle;

        ///<summary> Used for non-water erosion aka slippering of material</summary>
        public RenderTexture m_slippageHeight;
        ///<summary> Used for non-water erosion aka slippering of material. ARGB in format: A - flowLeft, R - flowR, G -  flowT, B - flowB</summary></summary>
        public RenderTexture m_slippageOutflow;


        private Rect m_rectLeft, m_rectRight, m_rectTop, m_rectBottom, withoutEdges, entireMap;

        //The resolution of the textures used for the simulation. You can change this to any number
        //Does not have to be a pow2 number. You will run out of GPU memory if made to high.
        public const int TEX_SIZE = 1024;//2048;
        public const int MAX_TEX_INDEX = 1023;//2047;

        ///<summary>The height of the terrain. You can change this</summary>
        private const int TERRAIN_HEIGHT = 128;
        //This is the size and resolution of the terrain mesh you see
        //You can change this but must be a pow2 number, ie 256, 512, 1024 etc
        public const int TOTAL_GRID_SIZE = 512;
        //You can make this smaller but not larger
        private const float TIME_STEP = 0.1f;

        ///<summary>Size of 1 mesh in meters</summary>
        private const int GRID_SIZE = 128;
        private const float PIPE_LENGTH = 1.0f;
        private const float CELL_LENGTH = 1.0f;
        private const float CELL_AREA = 1.0f; //CELL_LENGTH*CELL_LENGTH
        public const float GRAVITY = 9.81f;
        private const int READ = 0;
        private const int WRITE = 1;



        //public Texture2D bufferRGHalfTexture;
        //public Texture2D bufferARGBFloatTexture;
        //public Texture2D bufferRFloatTexture;

        //private readonly
        public List<WorldSides> oceans = new List<WorldSides>();

        RenderTexture tempRTARGB, tempRTRFloat;
        Texture2D tempT2DRGBA, tempT2DRFloat;
        private void Start()
        {
            Application.runInBackground = true;
            m_seed = UnityEngine.Random.Range(0, int.MaxValue);

            tempRTARGB = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGBFloat);
            tempRTARGB.wrapMode = TextureWrapMode.Clamp;
            tempRTARGB.filterMode = FilterMode.Point;
            tempRTARGB.Create();

            tempT2DRGBA = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
            tempT2DRGBA.wrapMode = TextureWrapMode.Clamp;
            tempT2DRGBA.filterMode = FilterMode.Point;

            tempRTRFloat = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat);
            tempRTRFloat.wrapMode = TextureWrapMode.Clamp;
            tempRTRFloat.filterMode = FilterMode.Point;
            tempRTRFloat.Create();

            tempT2DRFloat = new Texture2D(1, 1, TextureFormat.RFloat, false);
            tempT2DRFloat.wrapMode = TextureWrapMode.Clamp;
            tempT2DRFloat.filterMode = FilterMode.Point;

            //bufferRGHalfTexture = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGHalf, false);
            //bufferRGHalfTexture.wrapMode = TextureWrapMode.Clamp;
            //bufferRGHalfTexture.filterMode = FilterMode.Bilinear;

            //bufferARGBFloatTexture = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBAFloat, false);
            //bufferARGBFloatTexture.wrapMode = TextureWrapMode.Clamp;
            //bufferARGBFloatTexture.filterMode = FilterMode.Point;

            //bufferRFloatTexture = new Texture2D(1, 1, TextureFormat.RFloat, false);
            //bufferRFloatTexture.wrapMode = TextureWrapMode.Clamp;
            //bufferRFloatTexture.filterMode = FilterMode.Point;

            m_waterDamping = Mathf.Clamp01(m_waterDamping);
            m_regolithDamping = Mathf.Clamp01(m_regolithDamping);

            float u = 1.0f / (float)TEX_SIZE;

            m_rectLeft = new Rect(0.0f, 0.0f, u, 1.0f);
            m_rectRight = new Rect(1.0f - u, 0f, u, 1f);
            //m_rectRight = new Rect(1.0f , 0.0f, 1, 1.0f);


            m_rectBottom = new Rect(0.0f, 0.0f, 1.0f, u);
            m_rectTop = new Rect(0.0f, 1f - u, 1.0f, u);
            //m_rectTop = new Rect(0.0f, 1.0f, 1.0f, 1-u);


            withoutEdges = new Rect(0.0f + u, 0.0f + u, 1.0f - u, 1.0f - u);
            entireMap = new Rect(0f, 0f, 1f, 1f);


            m_terrainField = new RenderTexture[2];
            m_waterOutFlow = new RenderTexture[2];
            m_waterVelocity = new RenderTexture[2];
            m_advectSediment = new RenderTexture[2];
            m_waterField = new RenderTexture[2];
            m_sedimentField = new RenderTexture[2];
            m_regolithField = new RenderTexture[2];
            m_regolithOutFlow = new RenderTexture[2];

            m_terrainField[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBFloat);
            m_terrainField[0].wrapMode = TextureWrapMode.Clamp;
            m_terrainField[0].filterMode = FilterMode.Point;
            m_terrainField[0].name = "Terrain Field 0";
            m_terrainField[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBFloat);
            m_terrainField[1].wrapMode = TextureWrapMode.Clamp;
            m_terrainField[1].filterMode = FilterMode.Point;
            m_terrainField[1].name = "Terrain Field 1";


            m_waterOutFlow[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
            m_waterOutFlow[0].wrapMode = TextureWrapMode.Clamp;
            m_waterOutFlow[0].filterMode = FilterMode.Point;
            m_waterOutFlow[0].name = "Water outflow 0";
            m_waterOutFlow[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
            m_waterOutFlow[1].wrapMode = TextureWrapMode.Clamp;
            m_waterOutFlow[1].filterMode = FilterMode.Point;
            m_waterOutFlow[1].name = "Water outflow 1";

            m_waterVelocity[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RGHalf);// was RGHalf
            m_waterVelocity[0].wrapMode = TextureWrapMode.Clamp;
            m_waterVelocity[0].filterMode = FilterMode.Bilinear;
            m_waterVelocity[0].name = "Water Velocity 0";
            m_waterVelocity[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RGHalf);// was RGHalf
            m_waterVelocity[1].wrapMode = TextureWrapMode.Clamp;
            m_waterVelocity[1].filterMode = FilterMode.Bilinear;
            m_waterVelocity[1].name = "Water Velocity 1";

            m_waterField[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
            m_waterField[0].wrapMode = TextureWrapMode.Clamp;
            m_waterField[0].filterMode = FilterMode.Point;
            m_waterField[0].name = "Water Field 0";
            m_waterField[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
            m_waterField[1].wrapMode = TextureWrapMode.Clamp;
            m_waterField[1].filterMode = FilterMode.Point;
            m_waterField[1].name = "Water Field 1";

            m_regolithField[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
            m_regolithField[0].wrapMode = TextureWrapMode.Clamp;
            m_regolithField[0].filterMode = FilterMode.Point;
            m_regolithField[0].name = "Regolith Field 0";
            m_regolithField[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
            m_regolithField[1].wrapMode = TextureWrapMode.Clamp;
            m_regolithField[1].filterMode = FilterMode.Point;
            m_regolithField[1].name = "Regolith Field 1";

            m_regolithOutFlow[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
            m_regolithOutFlow[0].wrapMode = TextureWrapMode.Clamp;
            m_regolithOutFlow[0].filterMode = FilterMode.Point;
            m_regolithOutFlow[0].name = "Regolith outflow 0";
            m_regolithOutFlow[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
            m_regolithOutFlow[1].wrapMode = TextureWrapMode.Clamp;
            m_regolithOutFlow[1].filterMode = FilterMode.Point;
            m_regolithOutFlow[1].name = "Regolith outflow 1";

            m_sedimentField[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);// was RHalf
            m_sedimentField[0].wrapMode = TextureWrapMode.Clamp;
            m_sedimentField[0].filterMode = FilterMode.Bilinear;
            m_sedimentField[0].name = "Sediment Field 0";
            m_sedimentField[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);// was RHalf
            m_sedimentField[1].wrapMode = TextureWrapMode.Clamp;
            m_sedimentField[1].filterMode = FilterMode.Bilinear;
            m_sedimentField[1].name = "Sediment Field 1";

            m_advectSediment[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);// was RHalf
            m_advectSediment[0].wrapMode = TextureWrapMode.Clamp;
            m_advectSediment[0].filterMode = FilterMode.Bilinear;
            m_advectSediment[0].name = "Advect Sediment 0";
            m_advectSediment[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);// was RHalf
            m_advectSediment[1].wrapMode = TextureWrapMode.Clamp;
            m_advectSediment[1].filterMode = FilterMode.Bilinear;
            m_advectSediment[1].name = "Advect Sediment 1";

            m_tiltAngle = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
            m_tiltAngle.wrapMode = TextureWrapMode.Clamp;
            m_tiltAngle.filterMode = FilterMode.Point;
            m_tiltAngle.name = "Tilt Angle";

            m_slippageHeight = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);// was RHalf
            m_slippageHeight.wrapMode = TextureWrapMode.Clamp;
            m_slippageHeight.filterMode = FilterMode.Point;
            m_slippageHeight.name = "Slippage Height";

            m_slippageOutflow = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.ARGBHalf);
            m_slippageOutflow.wrapMode = TextureWrapMode.Clamp;
            m_slippageOutflow.filterMode = FilterMode.Point;
            m_slippageOutflow.name = "Slippage Outflow";

            MakeGrids();

            InitMaps();

        }
        // put it in separate class
        private void SetValue(RenderTexture[] field, Vector4 value, Rect rect)
        {
            //Graphics.Blit(field[READ], field[WRITE]);
            setFloatValueMat.SetVector("_Value", value);
            RTUtility.Blit(field[READ], field[WRITE], setFloatValueMat, rect, 0, false);
            RTUtility.Swap(field);
        }
        private void ChangeValueGauss(RenderTexture[] where, Vector2 point, float radius, float amount, Vector4 layerMask)
        {
            if (amount != 0f)
            {
                changValueGaussMat.SetVector("_Point", point);
                changValueGaussMat.SetFloat("_Radius", radius);
                changValueGaussMat.SetFloat("_Amount", amount);
                changValueGaussMat.SetVector("_LayerMask", layerMask);

                Graphics.Blit(where[READ], where[WRITE], changValueGaussMat);
                RTUtility.Swap(where);
            }
        }

        private void ChangeValueGaussZeroControl(RenderTexture[] where, Vector2 point, float radius, float amount, Vector4 layerMask)
        {
            if (amount != 0f)
            {
                changValueGaussZeroControlMat.SetVector("_Point", point);
                changValueGaussZeroControlMat.SetFloat("_Radius", radius);
                changValueGaussZeroControlMat.SetFloat("_Amount", amount);
                changValueGaussZeroControlMat.SetVector("_LayerMask", layerMask);

                Graphics.Blit(where[READ], where[WRITE], changValueGaussZeroControlMat);
                RTUtility.Swap(where);
            }
        }
        private void ChangeValue(RenderTexture[] field, Vector4 value, Rect rect)
        {
            //Graphics.Blit(field[READ], field[WRITE]); // don't know why but need it
            changeValueMat.SetVector("_Value", value);
            RTUtility.Blit(field[READ], field[WRITE], changeValueMat, rect, 0, false);
            RTUtility.Swap(field);
        }
        private void ChangeValueZeroControl(RenderTexture[] field, float value, Rect rect)
        {
            changeValueZeroControlMat.SetFloat("_Value", m_evaporationConstant * -1f);
            Graphics.Blit(m_waterField[READ], m_waterField[WRITE], changeValueZeroControlMat);
            RTUtility.Swap(m_waterField);
        }


        /// <summary>
        ///  Calculates flow of field        

        /// </summary>
        private void FlowLiquid(RenderTexture[] liquidField, RenderTexture[] outFlow, float damping)
        {
            m_outFlowMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_outFlowMat.SetFloat("T", TIME_STEP);
            m_outFlowMat.SetFloat("L", PIPE_LENGTH);
            m_outFlowMat.SetFloat("A", CELL_AREA);
            m_outFlowMat.SetFloat("G", GRAVITY);
            m_outFlowMat.SetFloat("_Layers", TERRAIN_LAYERS);
            m_outFlowMat.SetFloat("_Damping", 1.0f - damping);
            m_outFlowMat.SetTexture("_TerrainField", m_terrainField[READ]);
            m_outFlowMat.SetTexture("_Field", liquidField[READ]);

            Graphics.Blit(outFlow[READ], outFlow[WRITE], m_outFlowMat);

            RTUtility.Swap(outFlow);

            m_fieldUpdateMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_fieldUpdateMat.SetFloat("T", TIME_STEP);
            m_fieldUpdateMat.SetFloat("L", PIPE_LENGTH);
            m_fieldUpdateMat.SetTexture("_OutFlowField", outFlow[READ]);

            Graphics.Blit(liquidField[READ], liquidField[WRITE], m_fieldUpdateMat);
            RTUtility.Swap(liquidField);
        }
        /// <summary>
        ///  Calculates how much ground should go in sediment flow aka force-based erosion
        ///  Transfers m_terrainField to m_sedimentField basing on
        ///  m_waterVelocity, m_sedimentCapacity, m_dissolvingConstant,
        ///  m_depositionConstant, m_tiltAngle, m_minTiltAngle
        /// Also calculates m_tiltAngle
        /// </summary>
        private void ErosionAndDeposition()
        {
            m_tiltAngleMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_tiltAngleMat.SetFloat("_Layers", TERRAIN_LAYERS);
            m_tiltAngleMat.SetTexture("_TerrainField", m_terrainField[READ]);

            Graphics.Blit(null, m_tiltAngle, m_tiltAngleMat);

            m_erosionAndDepositionMat.SetTexture("_TerrainField", m_terrainField[READ]);
            m_erosionAndDepositionMat.SetTexture("_SedimentField", m_sedimentField[READ]);
            m_erosionAndDepositionMat.SetTexture("_VelocityField", m_waterVelocity[READ]);
            m_erosionAndDepositionMat.SetTexture("_WaterField", m_waterField[READ]);
            m_erosionAndDepositionMat.SetTexture("_TiltAngle", m_tiltAngle);
            m_erosionAndDepositionMat.SetFloat("_MinTiltAngle", m_minTiltAngle);
            m_erosionAndDepositionMat.SetFloat("_SedimentCapacity", m_sedimentCapacity);
            m_erosionAndDepositionMat.SetVector("_DissolvingConstant", m_dissolvingConstant);
            m_erosionAndDepositionMat.SetFloat("_DepositionConstant", m_depositionConstant);
            m_erosionAndDepositionMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);
            m_erosionAndDepositionMat.SetFloat("_DissolveLimit", dissolveLimit); //nash added it            

            RenderTexture[] terrainAndSediment = new RenderTexture[2] { m_terrainField[WRITE], m_sedimentField[WRITE] };

            RTUtility.MultiTargetBlit(terrainAndSediment, m_erosionAndDepositionMat);
            RTUtility.Swap(m_terrainField);
            RTUtility.Swap(m_sedimentField);
        }
        /// <summary>
        /// Transfers ground to regolith basing on water level, regolith level, max_regolith
        /// aka dissolution based erosion
        /// </summary>
        private void DisintegrateAndDeposit()
        {
            m_disintegrateAndDepositMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);
            m_disintegrateAndDepositMat.SetTexture("_TerrainField", m_terrainField[READ]);
            m_disintegrateAndDepositMat.SetTexture("_WaterField", m_waterField[READ]);
            m_disintegrateAndDepositMat.SetTexture("_RegolithField", m_regolithField[READ]);
            m_disintegrateAndDepositMat.SetFloat("_MaxRegolith", m_maxRegolith);

            RenderTexture[] terrainAndRegolith = new RenderTexture[2] { m_terrainField[WRITE], m_regolithField[WRITE] };

            RTUtility.MultiTargetBlit(terrainAndRegolith, m_disintegrateAndDepositMat);
            RTUtility.Swap(m_terrainField);
            RTUtility.Swap(m_regolithField);
        }
        /// <summary>
        ///  Calculates water velocity
        /// </summary>
        private void CalcWaterVelocity()
        {
            m_waterVelocityMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_waterVelocityMat.SetFloat("L", CELL_LENGTH);
            m_waterVelocityMat.SetTexture("_WaterField", m_waterField[READ]);
            m_waterVelocityMat.SetTexture("_WaterFieldOld", m_waterField[WRITE]);
            m_waterVelocityMat.SetTexture("_OutFlowField", m_waterOutFlow[READ]);

            Graphics.Blit(null, m_waterVelocity[READ], m_waterVelocityMat);

            const float viscosity = 10.5f;
            const int iterations = 2;

            m_diffuseVelocityMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_diffuseVelocityMat.SetFloat("_Alpha", CELL_AREA / (viscosity * TIME_STEP));

            for (int i = 0; i < iterations; i++)
            {
                Graphics.Blit(m_waterVelocity[READ], m_waterVelocity[WRITE], m_diffuseVelocityMat);
                RTUtility.Swap(m_waterVelocity);
            }
        }

        /// <summary>
        ///  Calculates sediment movement?
        /// </summary>
        private void AdvectSediment()
        {
            m_advectSedimentMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_advectSedimentMat.SetFloat("T", TIME_STEP);
            m_advectSedimentMat.SetFloat("_VelocityFactor", 1.0f);
            m_advectSedimentMat.SetTexture("_VelocityField", m_waterVelocity[READ]);

            //is bug? No its no
            Graphics.Blit(m_sedimentField[READ], m_advectSediment[0], m_advectSedimentMat);

            m_advectSedimentMat.SetFloat("_VelocityFactor", -1.0f);
            Graphics.Blit(m_advectSediment[READ], m_advectSediment[WRITE], m_advectSedimentMat);

            m_processMacCormackMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_processMacCormackMat.SetFloat("T", TIME_STEP);
            m_processMacCormackMat.SetTexture("_VelocityField", m_waterVelocity[READ]);
            m_processMacCormackMat.SetTexture("_InterField1", m_advectSediment[0]);
            m_processMacCormackMat.SetTexture("_InterField2", m_advectSediment[1]);

            Graphics.Blit(m_sedimentField[READ], m_sedimentField[WRITE], m_processMacCormackMat);
            RTUtility.Swap(m_sedimentField);
        }
        /// <summary>
        /// Erodes all ground layers based on it m_talusAngle, water isn't evolved
        /// </summary>
        private void ApplySlippage()
        {
            for (int i = 0; i < TERRAIN_LAYERS; i++)
            {
                if (m_talusAngle[i] < 90.0f)
                {
                    float talusAngle = (Mathf.PI * m_talusAngle[i]) / 180.0f;
                    float maxHeightDif = Mathf.Tan(talusAngle) * CELL_LENGTH;

                    m_slippageHeightMat.SetFloat("_TexSize", (float)TEX_SIZE);
                    m_slippageHeightMat.SetFloat("_Layers", (float)(i + 1));
                    m_slippageHeightMat.SetFloat("_MaxHeightDif", maxHeightDif);
                    m_slippageHeightMat.SetTexture("_TerrainField", m_terrainField[READ]);

                    Graphics.Blit(null, m_slippageHeight, m_slippageHeightMat);

                    m_slippageOutflowMat.SetFloat("_TexSize", (float)TEX_SIZE);
                    m_slippageOutflowMat.SetFloat("_Layers", (float)(i + 1));
                    m_slippageOutflowMat.SetFloat("T", TIME_STEP);
                    m_slippageOutflowMat.SetTexture("_MaxSlippageHeights", m_slippageHeight);
                    m_slippageOutflowMat.SetTexture("_TerrainField", m_terrainField[READ]);

                    Graphics.Blit(null, m_slippageOutflow, m_slippageOutflowMat);

                    m_slippageUpdateMat.SetFloat("T", TIME_STEP);
                    m_slippageUpdateMat.SetFloat("_TexSize", (float)TEX_SIZE);
                    m_slippageUpdateMat.SetFloat("_Layers", (float)(i + 1));
                    m_slippageUpdateMat.SetTexture("_SlippageOutflow", m_slippageOutflow);

                    Graphics.Blit(m_terrainField[READ], m_terrainField[WRITE], m_slippageUpdateMat);
                    RTUtility.Swap(m_terrainField);
                }
            }
        }

        private void Simulate()
        {
            RTUtility.SetToPoint(m_terrainField);
            RTUtility.SetToPoint(m_waterField);

            if (simulateWaterFlow)
            {
                /// Evaporate water everywhere 
                if (m_evaporationConstant > 0.0f)
                {
                    ChangeValueZeroControl(m_waterField, m_evaporationConstant * -1f, entireMap);
                }
                if (m_rainInputAmount > 0.0f)
                {
                    ChangeValue(m_waterField, new Vector4(m_rainInputAmount, 0f, 0f, 0f), entireMap);
                }


                if (m_waterInputAmount > 0f)
                    ChangeValueGaussZeroControl(m_waterField, m_waterInputPoint, m_waterInputRadius, m_waterInputAmount, new Vector4(1f, 0f, 0f, 0f));// WaterInput();
                if (waterDrainageAmount > 0f)
                {
                    ChangeValueGaussZeroControl(m_waterField, waterDrainagePoint, waterDrainageRadius, waterDrainageAmount * -1f, new Vector4(1f, 0f, 0f, 0f));
                    ChangeValueGaussZeroControl(m_terrainField, waterDrainagePoint, waterDrainageRadius, waterDrainageAmount * -1f, new Vector4(0f, 0f, 0f, 1f));
                }

                // set specified levels of water and terrain at oceans
                foreach (var item in oceans)
                {
                    Rect rect = getPartOfMap(item, 1);
                    SetValue(m_waterField, new Vector4(oceanWaterLevel, 0f, 0f, 0f), rect);
                    SetValue(m_terrainField, new Vector4(oceanDestroySedimentsLevel, 0f, 0f, 0f), rect);
                }


                FlowLiquid(m_waterField, m_waterOutFlow, m_waterDamping);
                CalcWaterVelocity();
            }

            if (simulateWaterErosion)
            {
                ErosionAndDeposition();
                AdvectSediment();
            }
            if (simulateRigolith)
            {
                DisintegrateAndDeposit();
                FlowLiquid(m_regolithField, m_regolithOutFlow, m_regolithDamping);
            }
            if (simulateSlippage)
                ApplySlippage();

            RTUtility.SetToBilinear(m_terrainField);
            RTUtility.SetToBilinear(m_waterField);
        }
        private void Update()
        {

            Simulate();
            UpdateMesh();
        }
        private void UpdateMesh()
        {
            // updating meshes
            //if the size of the mesh does not match the size of the texture 
            //the y axis needs to be scaled 
            float scaleY = (float)TOTAL_GRID_SIZE / (float)TEX_SIZE;

            m_landMat.SetFloat("_ScaleY", scaleY);
            m_landMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_landMat.SetTexture("_MainTex", m_terrainField[READ]);
            m_landMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);

            m_waterMat.SetTexture("_SedimentField", m_sedimentField[READ]);
            m_waterMat.SetTexture("_VelocityField", m_waterVelocity[READ]);
            m_waterMat.SetFloat("_ScaleY", scaleY);
            m_waterMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_waterMat.SetTexture("_WaterField", m_waterField[READ]);
            m_waterMat.SetTexture("_MainTex", m_terrainField[READ]);
            m_waterMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);
            m_waterMat.SetVector("_SunDir", m_sun.transform.forward * -1.0f);
            m_waterMat.SetVector("_SedimentColor", new Vector4(1f - 0.808f, 1f - 0.404f, 1f - 0.00f, 1f));
            //foreach (var item in m_gridLand)
            //{
            //    item.GetComponent<MeshCollider>().sharedMesh = item.GetComponent<MeshFilter>().mesh;
            //}            
        }
        private void InitMaps()
        {
            RTUtility.ClearColor(m_terrainField);
            RTUtility.ClearColor(m_waterOutFlow);
            RTUtility.ClearColor(m_waterVelocity);
            RTUtility.ClearColor(m_advectSediment);
            RTUtility.ClearColor(m_waterField);
            RTUtility.ClearColor(m_sedimentField);
            RTUtility.ClearColor(m_regolithField);
            RTUtility.ClearColor(m_regolithOutFlow);

            RenderTexture[] noiseTex = new RenderTexture[2];

            noiseTex[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
            noiseTex[0].wrapMode = TextureWrapMode.Clamp;
            noiseTex[0].filterMode = FilterMode.Bilinear;

            noiseTex[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat);
            noiseTex[1].wrapMode = TextureWrapMode.Clamp;
            noiseTex[1].filterMode = FilterMode.Bilinear;

            GPUPerlinNoise perlin = new GPUPerlinNoise(m_seed);
            perlin.LoadResourcesFor2DNoise();

            m_noiseMat.SetTexture("_PermTable1D", perlin.PermutationTable1D);
            m_noiseMat.SetTexture("_Gradient2D", perlin.Gradient2D);

            for (int j = 0; j < TERRAIN_LAYERS; j++)
            {
                m_noiseMat.SetFloat("_Offset", m_offset[j]);

                float amp = 0.5f;
                float freq = m_frequency[j];

                //Must clear noise from last pass
                RTUtility.ClearColor(noiseTex);

                //write noise into texture with the settings for this layer
                for (int i = 0; i < m_octaves[j]; i++)
                {
                    m_noiseMat.SetFloat("_Frequency", freq);
                    m_noiseMat.SetFloat("_Amp", amp);
                    m_noiseMat.SetFloat("_Pass", (float)i);

                    Graphics.Blit(noiseTex[READ], noiseTex[WRITE], m_noiseMat, (int)m_layerStyle[j]);
                    RTUtility.Swap(noiseTex);

                    freq *= m_lacunarity[j];
                    amp *= m_gain[j];
                }

                float useAbs = 0.0f;
                if (m_finalNosieIsAbs[j]) useAbs = 1.0f;

                //Mask the layers that we dont want to write into
                Vector4 mask = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                mask[j] = 1.0f;

                m_initTerrainMat.SetFloat("_Amp", m_amp[j]);
                m_initTerrainMat.SetFloat("_UseAbs", useAbs);
                m_initTerrainMat.SetVector("_Mask", mask);
                m_initTerrainMat.SetTexture("_NoiseTex", noiseTex[READ]);
                m_initTerrainMat.SetFloat("_Height", TERRAIN_HEIGHT);

                //Apply the noise for this layer to the terrain field
                Graphics.Blit(m_terrainField[READ], m_terrainField[WRITE], m_initTerrainMat);
                RTUtility.Swap(m_terrainField);
            }

            //dont need this tex anymore
            noiseTex[0] = null;
            noiseTex[1] = null;

        }

        private void OnDestroy()
        {

            Destroy(m_terrainField[0]);
            Destroy(m_terrainField[1]);
            Destroy(m_waterOutFlow[0]);
            Destroy(m_waterOutFlow[1]);
            Destroy(m_waterVelocity[0]);
            Destroy(m_waterVelocity[1]);
            Destroy(m_waterField[0]);
            Destroy(m_waterField[1]);
            Destroy(m_regolithField[0]);
            Destroy(m_regolithField[1]);
            Destroy(m_regolithOutFlow[0]);
            Destroy(m_regolithOutFlow[1]);
            Destroy(m_sedimentField[0]);
            Destroy(m_sedimentField[1]);
            Destroy(m_advectSediment[0]);
            Destroy(m_advectSediment[1]);
            Destroy(m_tiltAngle);
            Destroy(m_slippageHeight);
            Destroy(m_slippageOutflow);

            int numGrids = TOTAL_GRID_SIZE / GRID_SIZE;

            for (int x = 0; x < numGrids; x++)
            {
                for (int y = 0; y < numGrids; y++)
                {
                    int idx = x + y * numGrids;

                    Destroy(m_gridLand[idx]);
                    Destroy(m_gridWater[idx]);

                }
            }

        }

        private void MakeGrids()
        {
            int numGrids = TOTAL_GRID_SIZE / GRID_SIZE;

            m_gridLand = new GameObject[numGrids * numGrids];
            m_gridWater = new GameObject[numGrids * numGrids];

            for (int x = 0; x < numGrids; x++)
            {
                for (int y = 0; y < numGrids; y++)
                {
                    int idx = x + y * numGrids;

                    int posX = x * (GRID_SIZE - 1);
                    int posY = y * (GRID_SIZE - 1);

                    Mesh mesh = MakeMesh(GRID_SIZE, TOTAL_GRID_SIZE, posX, posY);

                    mesh.bounds = new Bounds(new Vector3(GRID_SIZE / 2, 0, GRID_SIZE / 2), new Vector3(GRID_SIZE, TERRAIN_HEIGHT * 2, GRID_SIZE));

                    m_gridLand[idx] = new GameObject("Grid Land " + idx.ToString());
                    m_gridLand[idx].AddComponent<MeshFilter>();
                    m_gridLand[idx].AddComponent<MeshRenderer>();
                    m_gridLand[idx].GetComponent<Renderer>().material = m_landMat;
                    m_gridLand[idx].GetComponent<MeshFilter>().mesh = mesh;
                    m_gridLand[idx].AddComponent<MeshCollider>();
                    //m_gridLand[idx].GetComponent<MeshCollider>().gameObject.layer = 8;
                    m_gridLand[idx].GetComponent<MeshCollider>().sharedMesh = mesh;

                    m_gridLand[idx].transform.localPosition = new Vector3(-TOTAL_GRID_SIZE / 2 + posX, 0, -TOTAL_GRID_SIZE / 2 + posY);
                    m_gridLand[idx].transform.SetParent(this.transform);

                    m_gridWater[idx] = new GameObject("Grid Water " + idx.ToString());
                    m_gridWater[idx].AddComponent<MeshFilter>();
                    m_gridWater[idx].AddComponent<MeshRenderer>();
                    m_gridWater[idx].GetComponent<Renderer>().material = m_waterMat;
                    m_gridWater[idx].GetComponent<MeshFilter>().mesh = mesh;
                    m_gridWater[idx].transform.localPosition = new Vector3(-TOTAL_GRID_SIZE / 2 + posX, 0, -TOTAL_GRID_SIZE / 2 + posY);
                    m_gridWater[idx].transform.SetParent(this.transform);
                }
            }
        }

        private Mesh MakeMesh(int size, int totalSize, int posX, int posY)
        {

            Vector3[] vertices = new Vector3[size * size];
            Vector2[] texcoords = new Vector2[size * size];
            Vector3[] normals = new Vector3[size * size];
            int[] indices = new int[size * size * 6];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Vector2 uv = new Vector3((posX + x) / (totalSize - 1.0f), (posY + y) / (totalSize - 1.0f));
                    Vector3 pos = new Vector3(x, 0.0f, y);
                    Vector3 norm = new Vector3(0.0f, 1.0f, 0.0f);

                    texcoords[x + y * size] = uv;
                    vertices[x + y * size] = pos;
                    normals[x + y * size] = norm;
                }
            }

            int num = 0;
            for (int x = 0; x < size - 1; x++)
            {
                for (int y = 0; y < size - 1; y++)
                {
                    indices[num++] = x + y * size;
                    indices[num++] = x + (y + 1) * size;
                    indices[num++] = (x + 1) + y * size;

                    indices[num++] = x + (y + 1) * size;
                    indices[num++] = (x + 1) + (y + 1) * size;
                    indices[num++] = (x + 1) + y * size;
                }
            }

            Mesh mesh = new Mesh();

            mesh.vertices = vertices;
            mesh.uv = texcoords;
            mesh.triangles = indices;
            mesh.normals = normals;

            return mesh;
        }

        private Vector4 getDataRGBAFloatEF(RenderTexture source, Vector2 point)
        {
            Graphics.CopyTexture(source, 0, 0, (int)(point.x * MAX_TEX_INDEX), (int)(point.y * MAX_TEX_INDEX), 1, 1, tempRTARGB, 0, 0, 0, 0);
            tempT2DRGBA = GetRTPixels(tempRTARGB, tempT2DRGBA);
            var res = tempT2DRGBA.GetPixel(0, 0);
            return res;
        }
        private Vector4 getDataRFloatEF(RenderTexture source, Point point)
        {
            Graphics.CopyTexture(source, 0, 0, point.x, point.y, 1, 1, tempT2DRFloat, 0, 0, 0, 0);
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
        /// <summary>
        /// Not supported
        /// </summary>   
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

        public void AddToTerrainLayer(MaterialsForEditing layer, Vector2 point)
        {
            Vector4 layerMask = default(Vector4);
            if (layer == MaterialsForEditing.stone)
                layerMask = new Vector4(1f, 0f, 0f, 0f);
            else if (layer == MaterialsForEditing.cobble)
                layerMask = new Vector4(0f, 1f, 0f, 0f);
            else if (layer == MaterialsForEditing.clay)
                layerMask = new Vector4(0f, 0f, 1f, 0f);
            else if (layer == MaterialsForEditing.sand)
                layerMask = new Vector4(0f, 0f, 0f, 1f);
            ChangeValueGauss(m_terrainField, point, brushSize, brushPower, layerMask);
        }
        public void RemoveFromTerrainLayer(MaterialsForEditing layer, Vector2 point)
        {
            Vector4 layerMask = default(Vector4);
            if (layer == MaterialsForEditing.stone)
            {
                layerMask = new Vector4(1f, 0f, 0f, 0f);
                ChangeValueGauss(m_terrainField, point, brushSize, brushPower * -1f, layerMask);
                return;
            }
            else if (layer == MaterialsForEditing.cobble)
                layerMask = new Vector4(0f, 1f, 0f, 0f);
            else if (layer == MaterialsForEditing.clay)
                layerMask = new Vector4(0f, 0f, 1f, 0f);
            else if (layer == MaterialsForEditing.sand)
                layerMask = new Vector4(0f, 0f, 0f, 1f);
            ChangeValueGaussZeroControl(m_terrainField, point, brushSize, brushPower * -1f, layerMask);
        }
        public void AddWater(Vector2 point)
        {
            ChangeValueGauss(m_waterField, point, brushSize, brushPower, new Vector4(1f, 0f, 0f, 0f));
        }
        public void RemoveWater(Vector2 point)
        {
            ChangeValueGaussZeroControl(m_waterField, point, brushSize, brushPower * -1f, new Vector4(1f, 0f, 0f, 0f));
        }
        public void AddSediment(Vector2 point)
        {
            ChangeValueGauss(m_sedimentField, point, brushSize, brushPower / 50f, new Vector4(1f, 0f, 0f, 0f));
        }
        public void RemoveSediment(Vector2 point)
        {
            ChangeValueGaussZeroControl(m_sedimentField, point, brushSize, brushPower * -1f / 50f, new Vector4(1f, 0f, 0f, 0f));
        }
        internal void MoveWaterSource(Vector2 point)
        {
            if (point == null)
            {
                m_waterInputAmount = 0f;
            }
            else
            {
                m_waterInputPoint = point;
                //m_waterInputPoint.x = point.x / (float)TEX_SIZE;
                //m_waterInputPoint.y = point.y / (float)TEX_SIZE;
                m_waterInputRadius = brushSize;
                m_waterInputAmount = brushPower;
            }
        }
        internal void MoveWaterDrainage(Vector2 point)
        {
            if (point == null)
            {
                waterDrainageAmount = 0f;
            }
            else
            {
                waterDrainagePoint = point;
                //waterDrainagePoint.x = selectedPoint.x / (float)TEX_SIZE;
                //waterDrainagePoint.y = selectedPoint.y / (float)TEX_SIZE;
                waterDrainageRadius = brushSize;
                waterDrainageAmount = brushPower;
            }
        }
        /// <summary>
        /// get rect-part of world texture according to world side
        /// </summary>
        private Rect getPartOfMap(WorldSides side, int width)
        {
            float offest = width / (float)TEX_SIZE;
            Rect rect = default(Rect);
            if (side == WorldSides.North)
            {
                rect = m_rectTop;
                rect.height += offest;// *-1f;                
                rect.y -= offest;
            }
            else if (side == WorldSides.South)
            {
                rect = m_rectBottom;
                rect.height += offest;
            }
            else if (side == WorldSides.East)
            {
                rect = m_rectRight;
                rect.x -= offest;
                rect.width += offest;
            }
            else if (side == WorldSides.West)
            {
                rect = m_rectLeft;
                rect.width += offest;// * -1f;
            }
            return rect;
        }
        /// <summary>
        /// returns which side of map is closer to point - north, south, etc
        /// </summary>
        private WorldSides getSideOfWorld(Vector2 point)
        {
            // find to which border it's closer 
            WorldSides side = default(WorldSides);
            int distToNorth = (int)Math.Abs(0 - point.x);
            int distToSouth = (int)Math.Abs(MAX_TEX_INDEX - point.x);
            int distToWest = (int)Math.Abs(0 - point.y);
            int distToEast = (int)Math.Abs(MAX_TEX_INDEX - point.y);

            if (distToEast == Math.Min(Math.Min(Math.Min(distToWest, distToEast), distToNorth), distToSouth))
                side = WorldSides.North;
            else if (distToWest == Math.Min(Math.Min(Math.Min(distToWest, distToEast), distToNorth), distToSouth))
                side = WorldSides.South;
            else if (distToSouth == Math.Min(Math.Min(Math.Min(distToWest, distToEast), distToNorth), distToSouth))
                side = WorldSides.East;
            else if (distToNorth == Math.Min(Math.Min(Math.Min(distToWest, distToEast), distToNorth), distToSouth))
                side = WorldSides.West;

            return side;
        }

        public void AddOcean(Vector2 point)
        {
            var side = getSideOfWorld(point);
            if (!oceans.Contains(side))
            {
                oceans.Add(side);
                //oceans = oceans & side;
                // clear ocean bottom
                ChangeValue(m_terrainField, new Vector4(oceanDepth * -1f, 0f, 0, 0f), getPartOfMap(side, oceanWidth));
            }
        }
        public void RemoveOcean(Vector2 point)
        {
            var side = getSideOfWorld(point);
            if (oceans.Contains(side))
            {
                oceans.Remove(side);
                ChangeValue(m_terrainField, new Vector4(oceanDepth, 0f, 0f, 0f), getPartOfMap(side, oceanWidth));
            }
        }

        public float getTerrainLevel(Vector2 point)
        {
            var vector4 = getDataRGBAFloatEF(m_terrainField[READ], point);
            return vector4.x + vector4.y + vector4.z + vector4.w;
        }
        public Vector4 getTerrainLayers(Vector2 point)
        {
            //return getData4Float32bits(m_terrainField[READ], point);
            return getDataRGBAFloatEF(m_terrainField[READ], point);
        }
        internal Vector4 getSedimentInWater(Point selectedPoint)
        {
            return getDataRFloatEF(m_sedimentField[READ], selectedPoint);
        }
        internal Vector4 getWaterLevel(Point selectedPoint)
        {

            return getDataRFloatEF(m_waterField[READ], selectedPoint);

            //Vector4 value = new Vector4(0f,1f,2,3f);
            //getValueMat.SetVector("_Coords", selectedPoint.getVector2(TEX_SIZE));
            //getValueMat.SetVector("_Output",  value);
            //Graphics.Blit(m_waterField[READ], null, getValueMat);            

            //return getValueMat.GetVector("_Output");
        }
        //internal Vector4 getWaterVelocity(Point selectedPoint)
        //{
        //    return getDataRGBAFloatEF(m_waterVelocity[READ], selectedPoint);
        //}

        private bool simulateWaterFlow = false;
        public void SetSimulateWater(bool value)
        {
            simulateWaterFlow = value;
        }

        public void SetWaterVisability(bool value)
        {
            if (value)
                //m_waterMat.SetVector("_WaterAbsorption", new Vector4(0.259f, 0.086f, 0.113f, 1000f));
                foreach (var item in m_gridWater)
                {
                    item.GetComponent<Renderer>().enabled = true;
                }
            else
                //m_waterMat.SetVector("_WaterAbsorption", new Vector4(0f, 0f, 0f, 0f));
                foreach (var item in m_gridWater)
                {
                    item.GetComponent<Renderer>().enabled = false;
                }
        }
        private bool simulateRigolith;
        public void SetSimulateRegolith(bool value)
        {
            simulateRigolith = value;
        }
        private bool simulateSlippage;
        public void SetSimulateSlippage(bool value)
        {
            simulateSlippage = value;
        }
        private bool simulateWaterErosion;
        public void SetSimulateWaterErosion(bool value)
        {
            simulateWaterErosion = value;
        }

        private float brushSize = 0.001f;
        public void SetBrushSize(float value)
        {
            brushSize = value;
        }
        private float brushPower = 0.5f;
        public void SetBrushPower(float value)
        {
            brushPower = value;
        }
        public void SetWaterZFighting(float value)
        {
            //todo save initial value
            m_waterMat.SetFloat("_MinWaterHt", value);
        }
        public void SetRainPower(float value)
        {
            m_rainInputAmount = value;
        }
        public void SetEvaporationPower(float value)
        {
            m_evaporationConstant = value;
        }
    }
}
