// good browni 51381BFF
// good red 552710FF

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
        ///<summary> Used for rendering</summary>
        public Material m_landMat, m_waterMat;
        ///<summary> Used for rendering</summary>
        public Material[] overlays;

        public Material m_initTerrainMat, m_noiseMat;
        public Material m_outFlowMat;
        ///<summary> Updates field according to outflow</summary>
        public Material m_fieldUpdateMat;
        public Material m_waterVelocityMat, m_diffuseVelocityMat;

        /// <summary> Calculates angle for each cell </summary>
        public Material m_tiltAngleMat;
        ///<summary> Calculates layer erosion basing on the forces that are caused by the running water</summary>
        public Material m_processMacCormackMat;
        public Material m_erosionAndDepositionMat;
        ///<summary> Creates new texture based on smoothed sediment data and size of texture(?)</summary>
        public Material m_advectSedimentMat;
        public Material m_slippageHeightMat, m_slippageOutflowMat, m_slippageUpdateMat;
        public Material m_disintegrateAndDepositMat, m_applyFreeSlipMat;
        public Material moveByLiquidMat;



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
        static private float terrainAmountScale = 0.5f;
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
        private float m_rainInputAmount = 0.001f;


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

        [SerializeField]
        public DataTexture m_terrainField;

        ///<summary></summary>        
        public DataTexture m_advectSediment;

        ///<summary> Actual amount of dissolved sediment in water</summary>
        public DataTexture m_sedimentField;
        public RenderTexture sedimentOutFlow;

        ///<summary> Actual amount of dissolved sediment in water</summary>
        public DataTexture sedimentDeposition;

        ///<summary> Contains regolith amount.Regolith is quasi-liquid at the bottom of water flow</summary>
        public DataTexture m_regolithField;

        ///<summary> Moved regolith amount in format ARGB : A - flowLeft, R - flowR, G -  flowT, B - flowB</summary>
        public DataTexture m_regolithOutFlow;

        ///<summary> Contains water amount. Can't be negative!!</summary>
        public DataTexture m_waterField;

        ///<summary> Moved water amount in format ARGB : A - flowLeft, R - flowR, G -  flowT, B - flowB. Keeps only positive numbers</summary>
        public DataTexture m_waterOutFlow;

        ///<summary> Water speed (1 channel)</summary>
        public DataTexture m_waterVelocity;

        ///<summary> Contains surface angels for each point. Used in water erosion only (Why?)</summary>
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

        private Overlay currentOverlay = Overlay.Default;

        //private readonly
        public List<WorldSides> oceans = new List<WorldSides>();
        private readonly Color[] layersColors = new Color[4] {
            new Vector4(123,125,152,155).normalized,
            new Vector4(91f, 91f, 99f, 355f).normalized,
            new Vector4(113,52,21,355).normalized,
            new Vector4(157,156,0, 255).normalized };

        public RenderTexture sediment;

        private void Start()
        {
            layersColors[0].a = 0.98f;
            layersColors[1].a = 0.98f;
            layersColors[2].a = 0.99f;
            layersColors[3].a = 0.9f;
            Application.runInBackground = true;
            //m_seed = UnityEngine.Random.Range(0, int.MaxValue);

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


            InitLayers();
            MakeGrids();
            InitMaps();
        }

        private void InitLayers()
        {
            m_terrainField = new DataTexture("Terrain Height Field", TEX_SIZE, RenderTextureFormat.ARGBFloat, FilterMode.Point);

            m_waterField = new DataTexture("Water Field", TEX_SIZE, RenderTextureFormat.RFloat, FilterMode.Point);
            m_waterOutFlow = new DataTexture("Water outflow", TEX_SIZE, RenderTextureFormat.ARGBHalf, FilterMode.Point);
            m_waterVelocity = new DataTexture("Water Velocity", TEX_SIZE, RenderTextureFormat.RGHalf, FilterMode.Bilinear);// was RGHalf

            m_sedimentField = new DataTexture("Sediment Field", TEX_SIZE, RenderTextureFormat.RHalf, FilterMode.Bilinear);// was RHalf
            m_advectSediment = new DataTexture("Sediment Advection", TEX_SIZE, RenderTextureFormat.RHalf, FilterMode.Bilinear);// was RHalf
            sedimentDeposition = new DataTexture("Sediment Deposition", TEX_SIZE, RenderTextureFormat.RHalf, FilterMode.Point);// was RHalf
            sedimentOutFlow = DataTexture.Create("sedimentOutFlow", TEX_SIZE, RenderTextureFormat.ARGBHalf, FilterMode.Point);// was RHalf

            //m_regolithField = new DataTexture("Regolith Field", TEX_SIZE, RenderTextureFormat.RFloat, FilterMode.Point);
            // m_regolithOutFlow = new DataTexture("Regolith outflow", TEX_SIZE, RenderTextureFormat.ARGBHalf, FilterMode.Point);


            m_tiltAngle = DataTexture.Create("Tilt Angle", TEX_SIZE, RenderTextureFormat.RHalf, FilterMode.Point);// was RHalf
            m_slippageHeight = DataTexture.Create("Slippage Height", TEX_SIZE, RenderTextureFormat.RHalf, FilterMode.Point);// was RHalf
            m_slippageOutflow = DataTexture.Create("Slippage Outflow", TEX_SIZE, RenderTextureFormat.ARGBHalf, FilterMode.Point);// was ARGBHalf

            sediment = m_sedimentField.READ;
        }

        /// <summary>
        ///  Calculates flow of field        

        /// </summary>
        private void FlowLiquid(DataTexture liquidField, DataTexture outFlow, float damping)
        {
            m_outFlowMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_outFlowMat.SetFloat("T", TIME_STEP);
            m_outFlowMat.SetFloat("L", PIPE_LENGTH);
            m_outFlowMat.SetFloat("A", CELL_AREA);
            m_outFlowMat.SetFloat("G", GRAVITY);
            m_outFlowMat.SetFloat("_Layers", TERRAIN_LAYERS);
            m_outFlowMat.SetFloat("_Damping", 1.0f - damping);
            m_outFlowMat.SetTexture("_TerrainField", m_terrainField.READ);
            m_outFlowMat.SetTexture("_Field", liquidField.READ);

            Graphics.Blit(outFlow.READ, outFlow.WRITE, m_outFlowMat);

            outFlow.Swap(); ;

            m_fieldUpdateMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_fieldUpdateMat.SetFloat("T", TIME_STEP);
            m_fieldUpdateMat.SetFloat("L", PIPE_LENGTH);
            m_fieldUpdateMat.SetTexture("_OutFlowField", outFlow.READ);

            Graphics.Blit(liquidField.READ, liquidField.WRITE, m_fieldUpdateMat);
            liquidField.Swap();
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
            m_tiltAngleMat.SetTexture("_TerrainField", m_terrainField.READ);

            Graphics.Blit(null, m_tiltAngle, m_tiltAngleMat);

            m_erosionAndDepositionMat.SetTexture("_TerrainField", m_terrainField.READ);
            m_erosionAndDepositionMat.SetTexture("_SedimentField", m_sedimentField.READ);
            m_erosionAndDepositionMat.SetTexture("_VelocityField", m_waterVelocity.READ);
            m_erosionAndDepositionMat.SetTexture("_WaterField", m_waterField.READ);
            m_erosionAndDepositionMat.SetTexture("_TiltAngle", m_tiltAngle);
            m_erosionAndDepositionMat.SetFloat("_MinTiltAngle", m_minTiltAngle);
            m_erosionAndDepositionMat.SetFloat("_SedimentCapacity", m_sedimentCapacity);
            m_erosionAndDepositionMat.SetVector("_DissolvingConstant", m_dissolvingConstant);
            m_erosionAndDepositionMat.SetFloat("_DepositionConstant", m_depositionConstant);
            m_erosionAndDepositionMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);
            m_erosionAndDepositionMat.SetFloat("_DissolveLimit", dissolveLimit); //nash added it            

            RenderTexture[] terrainAndSediment = new RenderTexture[3] { m_terrainField.WRITE, m_sedimentField.WRITE, sedimentDeposition.WRITE };

            RTUtility.MultiTargetBlit(terrainAndSediment, m_erosionAndDepositionMat);
            m_terrainField.Swap();
            m_sedimentField.Swap();
            sedimentDeposition.Swap();
        }
        /// <summary>
        /// Transfers ground to regolith basing on water level, regolith level, max_regolith
        /// aka dissolution based erosion
        /// </summary>
        private void DisintegrateAndDeposit()
        {
            m_disintegrateAndDepositMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);
            m_disintegrateAndDepositMat.SetTexture("_TerrainField", m_terrainField.READ);
            m_disintegrateAndDepositMat.SetTexture("_WaterField", m_waterField.READ);
            m_disintegrateAndDepositMat.SetTexture("_RegolithField", m_regolithField.READ);
            m_disintegrateAndDepositMat.SetFloat("_MaxRegolith", m_maxRegolith);

            RenderTexture[] terrainAndRegolith = new RenderTexture[2] { m_terrainField.WRITE, m_regolithField.WRITE };

            RTUtility.MultiTargetBlit(terrainAndRegolith, m_disintegrateAndDepositMat);
            m_terrainField.Swap();
            m_regolithField.Swap();
        }
        /// <summary>
        ///  Calculates water velocity
        /// </summary>
        private void CalcWaterVelocity()
        {
            m_waterVelocityMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_waterVelocityMat.SetFloat("L", CELL_LENGTH);
            m_waterVelocityMat.SetTexture("_WaterField", m_waterField.READ);
            m_waterVelocityMat.SetTexture("_WaterFieldOld", m_waterField.WRITE);
            m_waterVelocityMat.SetTexture("_OutFlowField", m_waterOutFlow.READ);

            Graphics.Blit(null, m_waterVelocity.READ, m_waterVelocityMat);

            const float viscosity = 10.5f;
            const int iterations = 2;

            m_diffuseVelocityMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_diffuseVelocityMat.SetFloat("_Alpha", CELL_AREA / (viscosity * TIME_STEP));

            for (int i = 0; i < iterations; i++)
            {
                Graphics.Blit(m_waterVelocity.READ, m_waterVelocity.WRITE, m_diffuseVelocityMat);
                m_waterVelocity.Swap();
            }
        }
        /// <summary>
        ///  Moves sediment 
        /// </summary>
        private void AlternativeAdvectSediment()
        {
            //m_advectSedimentMat.SetFloat("_TexSize", (float)TEX_SIZE);
            //moveByLiquidMat.SetFloat("T", TIME_STEP/ 10000);
            ////m_advectSedimentMat.SetFloat("_VelocityFactor", 1.0f);
            //moveByLiquidMat.SetTexture("_VelocityField", m_waterVelocity.READ);            

            //Graphics.Blit(m_sedimentField.READ, m_sedimentField.WRITE, moveByLiquidMat);
            //m_sedimentField.Swap();

            ////moveByLiquidMat.SetFloat("T", TIME_STEP * -1f);
            ////Graphics.Blit(m_sedimentField.WRITE, m_sedimentField.READ, moveByLiquidMat);
            ////m_sedimentField.Swap();


            moveByLiquidMat.SetFloat("T", TIME_STEP);
            moveByLiquidMat.SetTexture("_OutFlow", m_waterOutFlow.READ);
            moveByLiquidMat.SetTexture("_LuquidLevel", m_waterField.READ);

            Graphics.Blit(m_sedimentField.READ, sedimentOutFlow, moveByLiquidMat);

            m_fieldUpdateMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_fieldUpdateMat.SetFloat("T", TIME_STEP);
            m_fieldUpdateMat.SetFloat("L", PIPE_LENGTH);
            m_fieldUpdateMat.SetTexture("_OutFlowField", sedimentOutFlow);

            Graphics.Blit(m_sedimentField.READ, m_sedimentField.WRITE, m_fieldUpdateMat);
            m_sedimentField.Swap();

        }
        /// <summary>
        ///  Moves sediment 
        /// </summary>
        private void AdvectSediment()
        {
            m_advectSedimentMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_advectSedimentMat.SetFloat("T", TIME_STEP);
            m_advectSedimentMat.SetFloat("_VelocityFactor", 1.0f);
            m_advectSedimentMat.SetTexture("_VelocityField", m_waterVelocity.READ);

            //is bug? No its no
            Graphics.Blit(m_sedimentField.READ, m_advectSediment.READ, m_advectSedimentMat);

            m_advectSedimentMat.SetFloat("_VelocityFactor", -1.0f);
            Graphics.Blit(m_advectSediment.READ, m_advectSediment.WRITE, m_advectSedimentMat);

            m_processMacCormackMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_processMacCormackMat.SetFloat("T", TIME_STEP);
            m_processMacCormackMat.SetTexture("_VelocityField", m_waterVelocity.READ);
            m_processMacCormackMat.SetTexture("_InterField1", m_advectSediment.READ);
            m_processMacCormackMat.SetTexture("_InterField2", m_advectSediment.WRITE);

            Graphics.Blit(m_sedimentField.READ, m_sedimentField.WRITE, m_processMacCormackMat);
            m_sedimentField.Swap();
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
                    m_slippageHeightMat.SetTexture("_TerrainField", m_terrainField.READ);

                    Graphics.Blit(null, m_slippageHeight, m_slippageHeightMat);

                    m_slippageOutflowMat.SetFloat("_TexSize", (float)TEX_SIZE);
                    m_slippageOutflowMat.SetFloat("_Layers", (float)(i + 1));
                    m_slippageOutflowMat.SetFloat("T", TIME_STEP);
                    m_slippageOutflowMat.SetTexture("_MaxSlippageHeights", m_slippageHeight);
                    m_slippageOutflowMat.SetTexture("_TerrainField", m_terrainField.READ);

                    Graphics.Blit(null, m_slippageOutflow, m_slippageOutflowMat);

                    m_slippageUpdateMat.SetFloat("T", TIME_STEP);
                    m_slippageUpdateMat.SetFloat("_TexSize", (float)TEX_SIZE);
                    m_slippageUpdateMat.SetFloat("_Layers", (float)(i + 1));
                    m_slippageUpdateMat.SetTexture("_SlippageOutflow", m_slippageOutflow);

                    Graphics.Blit(m_terrainField.READ, m_terrainField.WRITE, m_slippageUpdateMat);
                    m_terrainField.Swap();
                }
            }
        }

        private void Simulate()
        {
            m_terrainField.SetFilterMode(FilterMode.Point);
            m_waterField.SetFilterMode(FilterMode.Point); ;
            sedimentDeposition.SetFilterMode(FilterMode.Point);

            if (simulateWaterFlow)
            {
                /// Evaporate water everywhere 
                if (m_evaporationConstant > 0.0f)
                {
                    m_waterField.ChangeValueZeroControl(m_evaporationConstant * -1f, entireMap);
                }
                if (m_rainInputAmount > 0.0f)
                {
                    m_waterField.ChangeValue(new Vector4(m_rainInputAmount, 0f, 0f, 0f), entireMap);
                }


                if (m_waterInputAmount > 0f)
                    m_waterField.ChangeValueGaussZeroControl(m_waterInputPoint, m_waterInputRadius, m_waterInputAmount, new Vector4(1f, 0f, 0f, 0f));// WaterInput();
                if (waterDrainageAmount > 0f)
                {
                    m_waterField.ChangeValueGaussZeroControl(waterDrainagePoint, waterDrainageRadius, waterDrainageAmount * -1f, new Vector4(1f, 0f, 0f, 0f));
                    m_terrainField.ChangeValueGaussZeroControl(waterDrainagePoint, waterDrainageRadius, waterDrainageAmount * -1f, new Vector4(0f, 0f, 0f, 1f));
                }

                // set specified levels of water and terrain at oceans
                foreach (var item in oceans)
                {
                    Rect rect = getPartOfMap(item, 1);
                    m_waterField.SetValue(new Vector4(oceanWaterLevel, 0f, 0f, 0f), rect);
                    m_terrainField.SetValue(new Vector4(oceanDestroySedimentsLevel, 0f, 0f, 0f), rect);
                }


                FlowLiquid(m_waterField, m_waterOutFlow, m_waterDamping);
                CalcWaterVelocity();
            }

            if (simulateWaterErosion)
            {
                ErosionAndDeposition();
                AdvectSediment();
                //AlternativeAdvectSediment();
            }
            if (simulateRigolith)
            {
                DisintegrateAndDeposit();
                FlowLiquid(m_regolithField, m_regolithOutFlow, m_regolithDamping);
            }
            if (simulateSlippage)
                ApplySlippage();

            m_terrainField.SetFilterMode(FilterMode.Bilinear);
            m_waterField.SetFilterMode(FilterMode.Bilinear);
            sedimentDeposition.SetFilterMode(FilterMode.Bilinear);
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

            if (currentOverlay == Overlay.Default)
            {
                overlays[currentOverlay.getID()].SetVector("_LayerColor0", layersColors[0]);
                overlays[currentOverlay.getID()].SetVector("_LayerColor1", layersColors[1]);
                overlays[currentOverlay.getID()].SetVector("_LayerColor2", layersColors[2]);
                overlays[currentOverlay.getID()].SetVector("_LayerColor3", layersColors[3]);

                m_landMat.SetFloat("_ScaleY", scaleY);
                m_landMat.SetFloat("_TexSize", (float)TEX_SIZE);
                m_landMat.SetTexture("_MainTex", m_terrainField.READ);
                m_landMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);
            }
            else if (currentOverlay == Overlay.Deposition)
            {
                overlays[currentOverlay.getID()].SetVector("_LayerColor0", layersColors[0]);
                overlays[currentOverlay.getID()].SetVector("_LayerColor1", layersColors[1]);
                overlays[currentOverlay.getID()].SetVector("_LayerColor2", layersColors[2]);
                overlays[currentOverlay.getID()].SetVector("_LayerColor3", layersColors[3]);

                overlays[currentOverlay.getID()].SetFloat("_ScaleY", scaleY);
                overlays[currentOverlay.getID()].SetFloat("_TexSize", (float)TEX_SIZE);
                overlays[currentOverlay.getID()].SetTexture("_MainTex", m_terrainField.READ);
                overlays[currentOverlay.getID()].SetTexture("_SedimentDepositionField", sedimentDeposition.READ);
                overlays[currentOverlay.getID()].SetFloat("_Layers", (float)TERRAIN_LAYERS);
            }

            m_waterMat.SetTexture("_SedimentField", m_sedimentField.READ);
            m_waterMat.SetTexture("_VelocityField", m_waterVelocity.READ);
            m_waterMat.SetFloat("_ScaleY", scaleY);
            m_waterMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_waterMat.SetTexture("_WaterField", m_waterField.READ);
            m_waterMat.SetTexture("_MainTex", m_terrainField.READ);
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
            m_terrainField.ClearColor();
            m_waterOutFlow.ClearColor();
            m_waterVelocity.ClearColor();
            m_advectSediment.ClearColor();
            m_waterField.ClearColor();
            m_sedimentField.ClearColor();
            //m_regolithField.ClearColor();
            //m_regolithOutFlow.ClearColor();

            DataTexture noiseTex;

            noiseTex = new DataTexture("", TEX_SIZE, RenderTextureFormat.RFloat, FilterMode.Bilinear);

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
                noiseTex.ClearColor();

                //write noise into texture with the settings for this layer
                for (int i = 0; i < m_octaves[j]; i++)
                {
                    m_noiseMat.SetFloat("_Frequency", freq);
                    m_noiseMat.SetFloat("_Amp", amp);
                    m_noiseMat.SetFloat("_Pass", (float)i);

                    Graphics.Blit(noiseTex.READ, noiseTex.WRITE, m_noiseMat, (int)m_layerStyle[j]);
                    noiseTex.Swap();

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
                m_initTerrainMat.SetTexture("_NoiseTex", noiseTex.READ);
                m_initTerrainMat.SetFloat("_Height", TERRAIN_HEIGHT);

                //Apply the noise for this layer to the terrain field
                Graphics.Blit(m_terrainField.READ, m_terrainField.WRITE, m_initTerrainMat);
                m_terrainField.Swap();
            }

            //dont need this tex anymore
            noiseTex.Destroy();

        }

        private void OnDestroy()
        {
            DataTexture.DestroyAll();
            Destroy(m_tiltAngle);
            Destroy(m_slippageHeight);
            Destroy(m_slippageOutflow);
            Destroy(sedimentOutFlow);


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
                    //m_gridLand[idx].AddComponent<MeshCollider>();
                    //m_gridLand[idx].GetComponent<MeshCollider>().gameObject.layer = 8;
                    //m_gridLand[idx].GetComponent<MeshCollider>().sharedMesh = mesh;

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
            m_terrainField.ChangeValueGauss(point, brushSize, brushPower, layerMask);
        }
        public void RemoveFromTerrainLayer(MaterialsForEditing layer, Vector2 point)
        {
            Vector4 layerMask = default(Vector4);
            if (layer == MaterialsForEditing.stone)
            {
                layerMask = new Vector4(1f, 0f, 0f, 0f);
                m_terrainField.ChangeValueGauss(point, brushSize, brushPower * -1f, layerMask);
                return;
            }
            else if (layer == MaterialsForEditing.cobble)
                layerMask = new Vector4(0f, 1f, 0f, 0f);
            else if (layer == MaterialsForEditing.clay)
                layerMask = new Vector4(0f, 0f, 1f, 0f);
            else if (layer == MaterialsForEditing.sand)
                layerMask = new Vector4(0f, 0f, 0f, 1f);
            m_terrainField.ChangeValueGaussZeroControl(point, brushSize, brushPower * -1f, layerMask);
        }
        public void AddWater(Vector2 point)
        {
            m_waterField.ChangeValueGauss(point, brushSize, brushPower, new Vector4(1f, 0f, 0f, 0f));
        }
        public void RemoveWater(Vector2 point)
        {
            m_waterField.ChangeValueGaussZeroControl(point, brushSize, brushPower * -1f, new Vector4(1f, 0f, 0f, 0f));
        }
        public void AddSediment(Vector2 point)
        {
            m_sedimentField.ChangeValueGauss(point, brushSize, brushPower / 50f, new Vector4(1f, 0f, 0f, 0f));
        }
        public void RemoveSediment(Vector2 point)
        {
            m_sedimentField.ChangeValueGaussZeroControl(point, brushSize, brushPower * -1f / 50f, new Vector4(1f, 0f, 0f, 0f));
        }
        internal void RemoveWaterSource()
        {
            m_waterInputAmount = 0f;
        }
        internal void MoveWaterSource(Vector2 point)
        {
            m_waterInputPoint = point;
            //m_waterInputPoint.x = point.x / (float)TEX_SIZE;
            //m_waterInputPoint.y = point.y / (float)TEX_SIZE;
            m_waterInputRadius = brushSize;
            m_waterInputAmount = brushPower;

        }
        internal void RemoveWaterDrainage()
        {
            waterDrainageAmount = 0f;
        }
        internal void MoveWaterDrainage(Vector2 point)
        {
            waterDrainagePoint = point;
            //waterDrainagePoint.x = selectedPoint.x / (float)TEX_SIZE;
            //waterDrainagePoint.y = selectedPoint.y / (float)TEX_SIZE;
            waterDrainageRadius = brushSize;
            waterDrainageAmount = brushPower;
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
                m_terrainField.ChangeValue(new Vector4(oceanDepth * -1f, 0f, 0, 0f), getPartOfMap(side, oceanWidth));
            }
        }
        public void RemoveOcean(Vector2 point)
        {
            var side = getSideOfWorld(point);
            if (oceans.Contains(side))
            {
                oceans.Remove(side);
                m_terrainField.ChangeValue(new Vector4(oceanDepth, 0f, 0f, 0f), getPartOfMap(side, oceanWidth));
            }
        }

        public float getTerrainLevel(Vector2 point)
        {
            var vector4 = m_terrainField.getDataRGBAFloatEF(point);
            return vector4.x + vector4.y + vector4.z + vector4.w;
        }
        public Vector4 getTerrainLayers(Vector2 point)
        {
            //return getData4Float32bits(m_terrainField.READ, point);
            return m_terrainField.getDataRGBAFloatEF(point);
        }
        internal Vector4 getSedimentInWater(Point selectedPoint)
        {
            return m_sedimentField.getDataRFloatEF(selectedPoint);
        }
        internal Vector4 getWaterLevel(Point selectedPoint)
        {

            return m_waterField.getDataRFloatEF(selectedPoint);

            //Vector4 value = new Vector4(0f,1f,2,3f);
            //getValueMat.SetVector("_Coords", selectedPoint.getVector2(TEX_SIZE));
            //getValueMat.SetVector("_Output",  value);
            //Graphics.Blit(m_waterField.READ, null, getValueMat);            

            //return getValueMat.GetVector("_Output");
        }
        //internal Vector4 getWaterVelocity(Point selectedPoint)
        //{
        //    return getDataRGBAFloatEF(m_waterVelocity.READ, selectedPoint);
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
        public void SetOverlay(Overlay overlay)
        {
            this.currentOverlay = overlay;
            foreach (var item in m_gridLand)
            {
                item.GetComponent<Renderer>().material = overlays[overlay.getID()];
            }
        }
    }
}
