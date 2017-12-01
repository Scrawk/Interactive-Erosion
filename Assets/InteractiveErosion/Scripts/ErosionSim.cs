// good browni 51381BFF
// good red 552710FF
// add texture of rain and evaporation amount? Will give oceans? No, it wouldn't
// add multiple water sources than?
// add drainage+
// add text for water sources?
// add oceans
// tilt angel breaks erosion? - min title did
// add lava
// remove water light absorption?
// add stone - cobblestone conversion?
// add layers of material on init?
// InteractiveErosion class

// add erosion limit +
// check deltas 
// check river formation +
// check meanders
// output shader
// check regolith
// doesn't draw all map?
// wider output for oceans
// update water drains to drain sand


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
        public Material m_initTerrainMat, m_noiseMat, m_waterInputMat;
        public Material m_evaprationMat, m_outFlowMat, m_fieldUpdateMat;
        public Material m_waterVelocityMat, m_diffuseVelocityMat;

        /// <summary>
        /// Contains angle for each cell
        /// </summary>
        public Material m_tiltAngleMat;
        ///<summary> Calculates layer erosion basing on the forces that are caused by the running water</summary>
        public Material m_processMacCormackMat;
        public Material m_erosionAndDepositionMat, m_advectSedimentMat;
        public Material m_slippageHeightMat, m_slippageOutflowMat, m_slippageUpdateMat;
        public Material m_disintegrateAndDepositMat, m_applyFreeSlipMat;
        public Material setFloatValueMat, changeValueMat, getValueMat;


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
            NOISE_STYLE.TURBULENCE,
            NOISE_STYLE.FRACTAL,
            NOISE_STYLE.FRACTAL,
            NOISE_STYLE.FRACTAL
        };

        //Noise settings. Each Component of vector is the setting for a layer
        //ie x is setting for layer 0, y is setting for layer 1 etc

        private Vector4 m_octaves = new Vector4(8, 6, 4, 8); //Higher octaves give more finer detail
        private Vector4 m_frequency = new Vector4(4f, 2f, 2f, 2f); //A lower value gives larger scale details
        private Vector4 m_lacunarity = new Vector4(2.5f, 2.3f, 2.0f, 2.0f); //Rate of change of the noise amplitude. Should be between 1 and 3 for fractal noise
        private Vector4 m_gain = new Vector4(0.5f, 0.5f, 0.5f, 0.5f); //Rate of change of the noise frequency

        //private Vector4 m_amp = new Vector4(2f, 2f, 0.5f, 0.5f); //Amount of terrain in a layer
        static private float terrainAmountScale = 0.3f;
        //private Vector4 m_amp = new Vector4(6.0f * terrainAmountScale, 3f * terrainAmountScale, 6f * terrainAmountScale, 0.15f * terrainAmountScale); //Amount of terrain in a layer        
        private Vector4 m_amp = new Vector4(2f * terrainAmountScale, 1f * terrainAmountScale, 2f * terrainAmountScale, 1f * terrainAmountScale); //Amount of terrain in a layer        


        //private Vector4 m_amp = new Vector4(0f, 0f, 0f, 2f); //Amount of terrain in a layer
        private Vector4 m_offset = new Vector4(0.0f, 10.0f, 20.0f, 30.0f);




        /// <summary>
        /// The settings for the erosion. If the value is a vector4 each component is for a layer
        /// How easily the layer dissolves
        /// </summary>
        public Vector4 m_dissolvingConstant = new Vector4(0.001f, 0.002f, 0.008f, 0.012f);// rain
        //private Vector4 m_dissolvingConstant = new Vector4(0.01f, 0.015f, 0.8f, 1.2f);// stream
        //private Vector4 m_dissolvingConstant = new Vector4(0.0001f, 0.001f, 0.01f, 0.1f);

        /// <summary>
        /// The angle that slippage will occur
        /// </summary>        
        //private Vector4 m_talusAngle = new Vector4(80f, 35f, 60f, 10f); looked good
        public Vector4 m_talusAngle = new Vector4(80f, 45f, 60f, 20f);

        /// <summary>
        /// A higher value will increase erosion on flat areas
        /// Used as limit for surface tilt
        /// Meaning that even flat area will erode as slightly tilted area
        /// </summary>
        public float m_minTiltAngle = 5f;//0.1f;

        /// <summary>
        /// How much sediment the water can carry per 1 unit of water
        /// </summary>
        public float m_sedimentCapacity = 0.2f;

        /// <summary> Rate the sediment is deposited on top layer </summary>
        public float m_depositionConstant = 0.015f;

        /// <summary> Terrain wouldn't dissolve if water level in cell is lower than this</summary>
        public float dissolveLimit = 0.001f;

        /// <summary>
        /// Evaporation rate of water
        /// </summary>
        public float m_evaporationConstant = 0.0011f;

        /// <summary> Movement speed of point of water source</summary>
        public float m_rainInputAmount = 0.001f;


        /// <summary>
        /// Viscosity of regolith
        /// </summary>
        public float m_regolithDamping = 0.85f;

        /// <summary> Viscosity of water</summary>        
        public float m_waterDamping = 0.0f;

        /// <summary>
        /// Higher number will increase dissolution rate
        /// water penetration depth?
        /// </summary>
        public float m_maxRegolith = 0.008f;

        public float oceanDestroySedimentsLevel = 0f;
        public float oceanDepth = 4f;
        public float oceanWaterLevel = 20f;
        public float oceanWidth = 83f;

        ///<summary> Meshes</summary>
        private GameObject[] m_gridLand, m_gridWater;

        ///<summary> Contains all 4 layers in ARGB</summary>
        public RenderTexture[] m_terrainField;

        ///<summary>sediment transport capacity? How much sediment water can hold?</summary>        
        public RenderTexture[] m_advectSediment;

        ///<summary> Contains sediment amount. What is sediment? Actual amount of sediment in water?</summary>
        public RenderTexture[] m_sedimentField;

        ///<summary> Contains regolith amount.Regolith is quasi-liquid at the bottom of water flow</summary>
        public RenderTexture[] m_regolithField;

        ///<summary> Moved regolith amount?</summary>
        public RenderTexture[] m_regolithOutFlow;

        ///<summary> Contains water amount.</summary>
        public RenderTexture[] m_waterField;

        ///<summary> Moved water amount?</summary>
        public RenderTexture[] m_waterOutFlow;

        ///<summary> Water speed (1 channel)</summary>
        public RenderTexture[] m_waterVelocity;

        ///<summary> Contains surface angels for each point</summary>
        public RenderTexture m_tiltAngle;

        ///<summary> Used for non-water erosion aka slippering of material</summary>
        public RenderTexture m_slippageHeight;
        ///<summary> Used for non-water erosion aka slippering of material. ARGB</summary>
        public RenderTexture m_slippageOutflow;


        private Rect m_rectLeft, m_rectRight, m_rectTop, m_rectBottom;

        //The resolution of the textures used for the simulation. You can change this to any number
        //Does not have to be a pow2 number. You will run out of GPU memory if made to high.
        public const int TEX_SIZE = 1024;
        public const int MAX_TEX_INDEX = 1023;

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
        public Texture2D bufferRGHalfTexture;
        public Texture2D bufferARGBFloatTexture;
        public Texture2D bufferRFloatTexture;

        //private readonly
        public List<WorldSides> oceans = new List<WorldSides>();

        private void Start()
        {
            Application.runInBackground = true;

            bufferRGHalfTexture = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGHalf, false);
            bufferRGHalfTexture.wrapMode = TextureWrapMode.Clamp;
            bufferRGHalfTexture.filterMode = FilterMode.Bilinear;

            bufferARGBFloatTexture = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBAFloat, false);
            bufferRGHalfTexture.wrapMode = TextureWrapMode.Clamp;
            bufferRGHalfTexture.filterMode = FilterMode.Point;

            bufferRFloatTexture = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBAFloat, false);
            bufferRGHalfTexture.wrapMode = TextureWrapMode.Clamp;
            bufferRGHalfTexture.filterMode = FilterMode.Bilinear;

            m_seed = UnityEngine.Random.Range(0, int.MaxValue);

            m_waterDamping = Mathf.Clamp01(m_waterDamping);
            m_regolithDamping = Mathf.Clamp01(m_regolithDamping);

            float u = 1.0f / (float)TEX_SIZE;

            m_rectLeft = new Rect(0.0f, 0.0f, u, 1.0f);
            m_rectRight = new Rect(1.0f - u, 0.0f, -u, 1.0f);
            //m_rectRight = new Rect(1.0f - u, 0.0f, 1.0f, 1.0f);

            m_rectBottom = new Rect(0.0f, 0.0f, 1.0f, u);

            m_rectTop = new Rect(0.0f, 1.0f - u, 1.0f, -u);
            //m_rectTop = new Rect(0.0f, 1.0f - u, 1.0f, 1.0f);

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

            m_waterVelocity[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RGHalf);
            m_waterVelocity[0].wrapMode = TextureWrapMode.Clamp;
            m_waterVelocity[0].filterMode = FilterMode.Bilinear;
            m_waterVelocity[0].name = "Water Velocity 0";
            m_waterVelocity[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RGHalf);
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

            m_sedimentField[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
            m_sedimentField[0].wrapMode = TextureWrapMode.Clamp;
            m_sedimentField[0].filterMode = FilterMode.Bilinear;
            m_sedimentField[0].name = "Sediment Field 0";
            m_sedimentField[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
            m_sedimentField[1].wrapMode = TextureWrapMode.Clamp;
            m_sedimentField[1].filterMode = FilterMode.Bilinear;
            m_sedimentField[1].name = "Sediment Field 1";

            m_advectSediment[0] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
            m_advectSediment[0].wrapMode = TextureWrapMode.Clamp;
            m_advectSediment[0].filterMode = FilterMode.Bilinear;
            m_advectSediment[0].name = "Advect Sediment 0";
            m_advectSediment[1] = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
            m_advectSediment[1].wrapMode = TextureWrapMode.Clamp;
            m_advectSediment[1].filterMode = FilterMode.Bilinear;
            m_advectSediment[1].name = "Advect Sediment 1";

            m_tiltAngle = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
            m_tiltAngle.wrapMode = TextureWrapMode.Clamp;
            m_tiltAngle.filterMode = FilterMode.Point;
            m_tiltAngle.name = "Tilt Angle";

            m_slippageHeight = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RHalf);
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
        private void SetValue(RenderTexture[] field, Vector4 value, Rect rect)
        {
            Graphics.Blit(field[READ], field[WRITE]);
            setFloatValueMat.SetVector("_Value", value);
            RTUtility.Blit(field[READ], field[WRITE], setFloatValueMat, rect, 0, false);
            RTUtility.Swap(field);
        }
        private void ChangeValue(RenderTexture[] where, Vector2 point, float radius, float amount)
        {
            if (amount != 0f)
            {
                m_waterInputMat.SetVector("_Point", point);
                m_waterInputMat.SetFloat("_Radius", radius);
                m_waterInputMat.SetFloat("_Amount", amount);

                Graphics.Blit(where[READ], where[WRITE], m_waterInputMat);
                RTUtility.Swap(where);
            }
        }
        private void ChangeValue(RenderTexture[] field, Vector4 value, Rect rect)
        {
            Graphics.Blit(field[READ], field[WRITE]);
            changeValueMat.SetVector("_Value", value);
            RTUtility.Blit(field[READ], field[WRITE], changeValueMat, rect, 0, false);
            RTUtility.Swap(field);
        }
        /// <summary>
        /// Adds water everywhere 
        /// </summary>
        private void RainInput()
        {
            if (m_rainInputAmount > 0.0f)
            {
                m_evaprationMat.SetFloat("_EvaporationConstant", m_rainInputAmount * -1f);
                Graphics.Blit(m_waterField[READ], m_waterField[WRITE], m_evaprationMat);
                RTUtility.Swap(m_waterField);
            }
            //float totalWaterChange = m_rainInputAmount * -1f + m_evaporationConstant;

            //if (totalWaterChange != 0f)
            //{
            //    m_evaprationMat.SetFloat("_EvaporationConstant", totalWaterChange);

            //    Graphics.Blit(m_waterField[READ], m_waterField[WRITE], m_evaprationMat);
            //    RTUtility.Swap(m_waterField);
            //}
        }
        /// <summary>
        /// Evaporate water everywhere 
        /// </summary>
        private void WaterEvaporate()
        {
            if (m_evaporationConstant > 0.0f)
            {
                m_evaprationMat.SetFloat("_EvaporationConstant", m_evaporationConstant);
                Graphics.Blit(m_waterField[READ], m_waterField[WRITE], m_evaprationMat);
                RTUtility.Swap(m_waterField);
            }

        }

        /// <summary>
        /// Copies data from edge-1 coordinates (by 4 sides) to edge of texture
        /// </summary>        
        private void ApplyFreeSlip(RenderTexture[] field)
        {
            float u = 1.0f / (float)TEX_SIZE;
            Vector2 offset;

            Graphics.Blit(field[READ], field[WRITE]);

            offset = new Vector2(u, 0.0f);
            m_applyFreeSlipMat.SetVector("_Offset", offset);
            RTUtility.Blit(field[READ], field[WRITE], m_applyFreeSlipMat, m_rectLeft, 0, false);

            offset = new Vector2(0.0f, u);
            m_applyFreeSlipMat.SetVector("_Offset", offset);
            RTUtility.Blit(field[READ], field[WRITE], m_applyFreeSlipMat, m_rectBottom, 0, false);

            offset = new Vector2(-u, 0.0f);
            m_applyFreeSlipMat.SetVector("_Offset", offset);
            RTUtility.Blit(field[READ], field[WRITE], m_applyFreeSlipMat, m_rectRight, 0, false);

            offset = new Vector2(0.0f, -u);
            m_applyFreeSlipMat.SetVector("_Offset", offset);
            RTUtility.Blit(field[READ], field[WRITE], m_applyFreeSlipMat, m_rectTop, 0, false);

            RTUtility.Swap(field);
        }

        /// <summary>
        ///  Calculates flow of field
        /// </summary>
        private void OutFlow(RenderTexture[] field, RenderTexture[] outFlow, float damping)
        {
            m_outFlowMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_outFlowMat.SetFloat("T", TIME_STEP);
            m_outFlowMat.SetFloat("L", PIPE_LENGTH);
            m_outFlowMat.SetFloat("A", CELL_AREA);
            m_outFlowMat.SetFloat("G", GRAVITY);
            m_outFlowMat.SetFloat("_Layers", TERRAIN_LAYERS);
            m_outFlowMat.SetFloat("_Damping", 1.0f - damping);
            m_outFlowMat.SetTexture("_TerrainField", m_terrainField[READ]);
            m_outFlowMat.SetTexture("_Field", field[READ]);

            Graphics.Blit(outFlow[READ], outFlow[WRITE], m_outFlowMat);
            RTUtility.Swap(outFlow);

            m_fieldUpdateMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_fieldUpdateMat.SetFloat("T", TIME_STEP);
            m_fieldUpdateMat.SetFloat("L", PIPE_LENGTH);
            m_fieldUpdateMat.SetTexture("_OutFlowField", outFlow[READ]);

            Graphics.Blit(field[READ], field[WRITE], m_fieldUpdateMat);
            RTUtility.Swap(field);
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
        ///  Calculates water velocity?
        /// </summary>
        private void WaterVelocity()
        {
            m_waterVelocityMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_waterVelocityMat.SetFloat("L", CELL_LENGTH);
            m_waterVelocityMat.SetTexture("_WaterField", m_waterField[READ]);
            m_waterVelocityMat.SetTexture("_WaterFieldOld", m_waterField[WRITE]);
            m_waterVelocityMat.SetTexture("_OutFlowField", m_waterOutFlow[READ]);

            Graphics.Blit(null, m_waterVelocity[READ], m_waterVelocityMat);
            //Graphics.Blit(null, m_waterVelocity[WRITE], m_waterVelocityMat);

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

            //is bug??
            Graphics.Blit(m_sedimentField[READ], m_advectSediment[0], m_advectSedimentMat);
            //Graphics.Blit(m_sedimentField[READ], m_advectSediment[WRITE], m_advectSedimentMat);

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
                RainInput();
                ////WaterEvaporate();
                if (m_waterInputAmount > 0f)
                    ChangeValue(m_waterField, m_waterInputPoint, m_waterInputRadius, m_waterInputAmount);// WaterInput();
                if (waterDrainageAmount > 0f)
                    ChangeValue(m_waterField, waterDrainagePoint, waterDrainageRadius, waterDrainageAmount * -1f);
                WaterEvaporate();

                // set specified levels of water and terrain at oceans
                foreach (var item in oceans)
                {
                    Rect rect = getPartOfMap(item, 2f);
                    //item.Value.waterLevel
                    SetValue(m_waterField, new Vector4(oceanWaterLevel, 0f, 0f, 0f), rect);
                    SetValue(m_terrainField, new Vector4(oceanDestroySedimentsLevel, 0f, 0f, 0f), rect);
                }
                ApplyFreeSlip(m_terrainField);
                ApplyFreeSlip(m_sedimentField);
                ApplyFreeSlip(m_waterField);
                ApplyFreeSlip(m_regolithField);


                OutFlow(m_waterField, m_waterOutFlow, m_waterDamping);
                WaterVelocity();
            }

            if (simulateWaterErosion)
            {
                ErosionAndDeposition();
                ApplyFreeSlip(m_terrainField);
                ApplyFreeSlip(m_sedimentField);
                AdvectSediment();
            }
            if (simulateRigolith)
            {
                DisintegrateAndDeposit();
                ApplyFreeSlip(m_terrainField);
                ApplyFreeSlip(m_regolithField);
                OutFlow(m_regolithField, m_regolithOutFlow, m_regolithDamping);
            }
            if (simulateSlippage)
                ApplySlippage();

            RTUtility.SetToBilinear(m_terrainField);
            RTUtility.SetToBilinear(m_waterField);
        }
        private void Update()
        {

            Simulate();

            //updating meshes
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
        //public float getTerrainHeight(Vector3 point)
        //{

        //}

        private Vector4 getData4Float32bits(RenderTexture source, Point point)
        {
            bufferARGBFloatTexture = GetRTPixels(source, bufferARGBFloatTexture);
            var res = bufferARGBFloatTexture.GetPixel(point.x, point.y);
            return res;
        }
        private float getDataRFloat(RenderTexture source, Point point)
        {
            bufferRFloatTexture = GetRTPixels(source, bufferRFloatTexture);
            var res = bufferRFloatTexture.GetPixel(point.x, point.y);
            return res.r;
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

        public void AddToTerrainLayer(int layer, Point point)
        {
            ChangeValue(m_terrainField, point.getVector2(TEX_SIZE), brushSize, brushPower);
        }
        public void RemoveFromTerrainLayer(int layer, Point point)
        {
            ChangeValue(m_terrainField, point.getVector2(TEX_SIZE), brushSize, brushPower * -1f);
        }
        public void AddWater(Point point)
        {
            ChangeValue(m_waterField, point.getVector2(TEX_SIZE), brushSize, brushPower);
        }
        public void RemoveWater(Point point)
        {
            ChangeValue(m_waterField, point.getVector2(TEX_SIZE), brushSize, brushPower * -1f);
        }
        internal void MoveWaterSource(Point selectedPoint)
        {
            if (selectedPoint == null)
            {
                m_waterInputAmount = 0f;
            }
            else
            {
                m_waterInputPoint.x = selectedPoint.x / (float)TEX_SIZE;
                m_waterInputPoint.y = selectedPoint.y / (float)TEX_SIZE;
                m_waterInputRadius = brushSize;
                m_waterInputAmount = brushPower;
            }
        }
        internal void MoveWaterDrainage(Point selectedPoint)
        {
            if (selectedPoint == null)
            {
                waterDrainageAmount = 0f;
            }
            else
            {
                waterDrainagePoint.x = selectedPoint.x / (float)TEX_SIZE;
                waterDrainagePoint.y = selectedPoint.y / (float)TEX_SIZE;
                waterDrainageRadius = brushSize;
                waterDrainageAmount = brushPower;
            }
        }

        /// <summary>
        /// returns which side of map is closer to point - north, south, etc
        /// </summary>
        private WorldSides getSideOfWorld(Point point)
        {
            // find to which border it's closer 
            WorldSides side = default(WorldSides);
            int distToNorth = Math.Abs(0 - point.x);
            int distToSouth = Math.Abs(MAX_TEX_INDEX - point.x);
            int distToWest = Math.Abs(0 - point.y);
            int distToEast = Math.Abs(MAX_TEX_INDEX - point.y);

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
        /// <summary>
        /// get rect-part of world texture according to world side
        /// </summary>
        private Rect getPartOfMap(WorldSides side, float width)
        {
            Rect rect = default(Rect);
            if (side == WorldSides.North)
            {
                // failed here
                rect = m_rectTop;
                rect.height *= width;// *-1f;                
            }
            else if (side == WorldSides.South)
            {
                rect = m_rectBottom;
                rect.height *= width;
            }
            else if (side == WorldSides.East)
            {
                rect = m_rectRight;
                rect.width *= width;
            }
            else if (side == WorldSides.West)
            {
                rect = m_rectLeft;
                rect.width *= width;// * -1f;
            }
            return rect;
        }
        public void AddOcean(Point point)
        {
            var side = getSideOfWorld(point);
            if (!oceans.Contains(side))
            {
                oceans.Add(side);
                //oceans = oceans & side;
                // clear ocean bottom
                ChangeValue(m_terrainField, new Vector4(oceanDepth * -1f, 0f, 0f, 0f), getPartOfMap(side, oceanWidth));
            }
        }
        public void RemoveOcean(Point point)
        {
            var side = getSideOfWorld(point);
            if (oceans.Contains(side))
            {
                oceans.Remove(side);
                ChangeValue(m_terrainField, new Vector4(oceanDepth, 0f, 0f, 0f), getPartOfMap(side, oceanWidth));
            }
        }
        public float getTerrainLevel(Point point)
        {
            var vector4 = getData4Float32bits(m_terrainField[READ], point);
            return vector4.x + vector4.y + vector4.z + vector4.w;
        }
        public Vector4 getTerrainLayers(Point point)
        {
            return getData4Float32bits(m_terrainField[READ], point);
        }
        internal float getSandInWater(Point selectedPoint)
        {
            return getDataRFloat(m_sedimentField[READ], selectedPoint);
        }
        internal Vector4 getWaterLevel(Point selectedPoint)
        {
            return getData4Float32bits(m_waterField[READ], selectedPoint);

            //getValueMat.SetVector("_Coords", selectedPoint.getVector2(TEX_SIZE));            
            //Graphics.Blit(m_waterField[READ], null, getValueMat);
            ////RTUtility.Swap(m_waterField);            

            //return getValueMat.GetColor("_Output");
        }
        internal Vector4 getWaterVelocity(Point selectedPoint)
        {
            return getData4Float32bits(m_waterVelocity[READ], selectedPoint);
        }

        private bool simulateWaterFlow = false;
        public void SetSimulateWater(bool value)
        {
            simulateWaterFlow = value;
        }

        public void SetWaterVisability(bool value)
        {
            if (value)
                m_waterMat.SetVector("_WaterAbsorption", new Vector4(0.259f, 0.086f, 0.113f, 1000f));
            else
                m_waterMat.SetVector("_WaterAbsorption", new Vector4(0f, 0f, 0f, 0f));
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
            m_waterMat.SetFloat("_MinWaterHt", value);
        }
    }
}
