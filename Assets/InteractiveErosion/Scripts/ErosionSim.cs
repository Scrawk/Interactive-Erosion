using UnityEngine;
using System.Collections;

using ImprovedPerlinNoiseProject;

namespace InterativeErosionProject
{
    public enum NOISE_STYLE { FRACTAL = 0, TURBULENCE = 1, RIDGE_MULTI_FRACTAL = 2, WARPED = 3 };

    public class ErosionSim : MonoBehaviour
    {
       

        public GameObject m_sun;
        public Material m_landMat, m_waterMat;
        public Material m_initTerrainMat, m_noiseMat, m_waterInputMat;
        public Material m_evaprationMat, m_outFlowMat, m_fieldUpdateMat;
        public Material m_waterVelocityMat, m_diffuseVelocityMat, m_tiltAngleMat;
        public Material m_erosionAndDepositionMat, m_advectSedimentMat, m_processMacCormackMat;
        public Material m_slippageHeightMat, m_slippageOutflowMat, m_slippageUpdateMat;
        public Material m_disintegrateAndDepositMat, m_applyFreeSlipMat;

        public float m_waterInputSpeed = 0.01f;
        public Vector2 m_waterInputPoint = new Vector2(0.5f, 0.5f);
        public float m_waterInputAmount = 2.0f;
        public float m_waterInputRadius = 0.008f;

        //Noise settings. Each Component of vector is the setting for a layer
        //ie x is setting for layer 0, y is setting for layer 1 etc
        public int m_seed = 2;
        public Vector4 m_octaves = new Vector4(8, 8, 8, 8); //Higher octaves give more finer detail
        public Vector4 m_frequency = new Vector4(2.0f, 100.0f, 200.0f, 200.0f); //A lower value gives larger scale details
        public Vector4 m_lacunarity = new Vector4(2.0f, 3.0f, 3.0f, 2.0f); //Rate of change of the noise amplitude. Should be between 1 and 3 for fractal noise
        public Vector4 m_gain = new Vector4(0.5f, 0.5f, 0.5f, 0.5f); //Rate of chage of the noise frequency
        public Vector4 m_amp = new Vector4(2.0f, 0.01f, 0.01f, 0.001f); //Amount of terrain in a layer
        public Vector4 m_offset = new Vector4(0.0f, 10.0f, 20.0f, 30.0f);

        //The settings for the erosion. If the value is a vector4 each component is for a layer
        public Vector4 m_dissolvingConstant = new Vector4(0.01f, 0.04f, 0.2f, 0.2f); //How easily the layer dissolves
        public float m_sedimentCapacity = 0.2f; //How much sediment the water can carry
        public float m_depositionConstant = 0.015f; //Rate the sediment is deposited on top layer
        public float m_evaporationConstant = 0.01f; //Evaporation rate of water
        public float m_minTiltAngle = 0.1f; //A higher value will increase erosion on flat areas
        public float m_regolithDamping = 0.85f; //Viscosity of regolith
        public float m_waterDamping = 0.0f; //Viscosity of water
        public float m_maxRegolith = 0.008f; //Higher number will increase dissolution rate
        public Vector4 m_talusAngle = new Vector4(45.0f, 20.0f, 15.0f, 15.0f); //The angle that slippage will occur

        private GameObject[] m_gridLand, m_gridWater;

        private RenderTexture[] m_terrainField, m_waterOutFlow, m_waterVelocity;
        private RenderTexture[] m_advectSediment, m_waterField, m_sedimentField;
        private RenderTexture m_tiltAngle, m_slippageHeight, m_slippageOutflow;
        private RenderTexture[] m_regolithField, m_regolithOutFlow;

        private Rect m_rectLeft, m_rectRight, m_rectTop, m_rectBottom;

        //The number of layers used in the simulation. Must be 1, 2, 3 or, 4
        private const int TERRAIN_LAYERS = 3;
        //The resolution of the textures used for the simulation. You can change this to any number
        //Does not have to be a pow2 number. You will run out of GPU memory if made to high.
        private const int TEX_SIZE = 1024;
        //The height of the terrain. You can change this
        private const int TERRAIN_HEIGHT = 128;
        //This is the size and resolution of the terrain mesh you see
        //You can change this but must be a pow2 number, ie 256, 512, 1024 etc
        private const int TOTAL_GRID_SIZE = 512;
        //You can make this smaller but not larger
        private const float TIME_STEP = 0.1f;

        private const int GRID_SIZE = 128;
        private const float PIPE_LENGTH = 1.0f;
        private const float CELL_LENGTH = 1.0f;
        private const float CELL_AREA = 1.0f; //CELL_LENGTH*CELL_LENGTH
        private const float GRAVITY = 9.81f;
        private const int READ = 0;
        private const int WRITE = 1;

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

        private void Start()
        {
            m_waterDamping = Mathf.Clamp01(m_waterDamping);
            m_regolithDamping = Mathf.Clamp01(m_regolithDamping);

            float u = 1.0f / (float)TEX_SIZE;

            m_rectLeft = new Rect(0.0f, 0.0f, u, 1.0f);
            m_rectRight = new Rect(1.0f - u, 0.0f, u, 1.0f);

            m_rectBottom = new Rect(0.0f, 0.0f, 1.0f, u);
            m_rectTop = new Rect(0.0f, 1.0f - u, 1.0f, u);

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

        private void WaterInput()
        {

            if (Input.GetKey(KeyCode.DownArrow)) m_waterInputPoint.y -= m_waterInputSpeed * Time.deltaTime;

            if (Input.GetKey(KeyCode.UpArrow)) m_waterInputPoint.y += m_waterInputSpeed * Time.deltaTime;

            if (Input.GetKey(KeyCode.LeftArrow)) m_waterInputPoint.x -= m_waterInputSpeed * Time.deltaTime;

            if (Input.GetKey(KeyCode.RightArrow)) m_waterInputPoint.x += m_waterInputSpeed * Time.deltaTime;

            if (m_waterInputAmount > 0.0f)
            {
                m_waterInputMat.SetVector("_Point", m_waterInputPoint);
                m_waterInputMat.SetFloat("_Radius", m_waterInputRadius);
                m_waterInputMat.SetFloat("_Amount", m_waterInputAmount);

                Graphics.Blit(m_waterField[READ], m_waterField[WRITE], m_waterInputMat);
                RTUtility.Swap(m_waterField);
            }

            if (m_evaporationConstant > 0.0f)
            {
                m_evaprationMat.SetFloat("_EvaporationConstant", m_evaporationConstant);

                Graphics.Blit(m_waterField[READ], m_waterField[WRITE], m_evaprationMat);
                RTUtility.Swap(m_waterField);
            }
        }

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

        private void WaterVelocity()
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

        private void ErosionAndDeposition()
        {
            m_tiltAngleMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_tiltAngleMat.SetFloat("_Layers", TERRAIN_LAYERS);
            m_tiltAngleMat.SetTexture("_TerrainField", m_terrainField[READ]);

            Graphics.Blit(null, m_tiltAngle, m_tiltAngleMat);

            m_erosionAndDepositionMat.SetTexture("_TerrainField", m_terrainField[READ]);
            m_erosionAndDepositionMat.SetTexture("_SedimentField", m_sedimentField[READ]);
            m_erosionAndDepositionMat.SetTexture("_VelocityField", m_waterVelocity[READ]);
            m_erosionAndDepositionMat.SetTexture("_TiltAngle", m_tiltAngle);
            m_erosionAndDepositionMat.SetFloat("_MinTiltAngle", m_minTiltAngle);
            m_erosionAndDepositionMat.SetFloat("_SedimentCapacity", m_sedimentCapacity);
            m_erosionAndDepositionMat.SetVector("_DissolvingConstant", m_dissolvingConstant);
            m_erosionAndDepositionMat.SetFloat("_DepositionConstant", m_depositionConstant);
            m_erosionAndDepositionMat.SetFloat("_Layers", (float)TERRAIN_LAYERS);

            RenderTexture[] terrainAndSediment = new RenderTexture[2] { m_terrainField[WRITE], m_sedimentField[WRITE] };

            RTUtility.MultiTargetBlit(terrainAndSediment, m_erosionAndDepositionMat);
            RTUtility.Swap(m_terrainField);
            RTUtility.Swap(m_sedimentField);
        }

        private void AdvectSediment()
        {
            m_advectSedimentMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_advectSedimentMat.SetFloat("T", TIME_STEP);
            m_advectSedimentMat.SetFloat("_VelocityFactor", 1.0f);
            m_advectSedimentMat.SetTexture("_VelocityField", m_waterVelocity[READ]);

            Graphics.Blit(m_sedimentField[READ], m_advectSediment[0], m_advectSedimentMat);

            m_advectSedimentMat.SetFloat("_VelocityFactor", -1.0f);
            Graphics.Blit(m_advectSediment[0], m_advectSediment[1], m_advectSedimentMat);

            m_processMacCormackMat.SetFloat("_TexSize", (float)TEX_SIZE);
            m_processMacCormackMat.SetFloat("T", TIME_STEP);
            m_processMacCormackMat.SetTexture("_VelocityField", m_waterVelocity[READ]);
            m_processMacCormackMat.SetTexture("_InterField1", m_advectSediment[0]);
            m_processMacCormackMat.SetTexture("_InterField2", m_advectSediment[1]);

            Graphics.Blit(m_sedimentField[READ], m_sedimentField[WRITE], m_processMacCormackMat);
            RTUtility.Swap(m_sedimentField);
        }

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

        private void Update()
        {

            RTUtility.SetToPoint(m_terrainField);
            RTUtility.SetToPoint(m_waterField);

            WaterInput();

            ApplyFreeSlip(m_terrainField);
            ApplyFreeSlip(m_sedimentField);
            ApplyFreeSlip(m_waterField);
            ApplyFreeSlip(m_regolithField);

            OutFlow(m_waterField, m_waterOutFlow, m_waterDamping);

            WaterVelocity();

            ErosionAndDeposition();
            ApplyFreeSlip(m_terrainField);
            ApplyFreeSlip(m_sedimentField);

            AdvectSediment();

            DisintegrateAndDeposit();
            ApplyFreeSlip(m_terrainField);
            ApplyFreeSlip(m_regolithField);

            OutFlow(m_regolithField, m_regolithOutFlow, m_regolithDamping);

            ApplySlippage();

            RTUtility.SetToBilinear(m_terrainField);
            RTUtility.SetToBilinear(m_waterField);

            //if the size of the mesh does not match the size of the teture 
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

                    m_gridWater[idx] = new GameObject("Grid Water " + idx.ToString());
                    m_gridWater[idx].AddComponent<MeshFilter>();
                    m_gridWater[idx].AddComponent<MeshRenderer>();
                    m_gridWater[idx].GetComponent<Renderer>().material = m_waterMat;
                    m_gridWater[idx].GetComponent<MeshFilter>().mesh = mesh;
                    m_gridWater[idx].transform.localPosition = new Vector3(-TOTAL_GRID_SIZE / 2 + posX, 0, -TOTAL_GRID_SIZE / 2 + posY);

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

    }
}
