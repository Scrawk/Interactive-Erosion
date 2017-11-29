
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace InterativeErosionProject
{
    enum Layers
    {
        stone, cobble, clay, sand,

    }
    public class InfoWindow : DragPanel
    {
        public Text text;
        public ErosionSim sim;
        //public ControlPanel controlPanel;
        public override void Refresh()
        {
            if (ControlPanel.selectedPoint != null)
            {
                var sb = new StringBuilder();
                sb.Append("Selected point: ").Append(ControlPanel.selectedPoint);
                //sb.Append("Water velocity: ").Append(sim.getDataRGHalf(sim.m_waterVelocity[0], Player.selectedPoint));

                var vector4 = sim.getTerrainLayers(ControlPanel.selectedPoint);
                var terrainHeight = vector4.x + vector4.y + vector4.z + vector4.w;

                sb.Append("\nTerrain height: ").Append(terrainHeight);
                sb.Append("\n").Append((Layers)0).Append(" height: ").Append(vector4.x);
                sb.Append("\n").Append((Layers)1).Append(" height: ").Append(vector4.y);
                sb.Append("\n").Append((Layers)2).Append(" height: ").Append(vector4.z);
                sb.Append("\n").Append((Layers)3).Append(" height: ").Append(vector4.w);

                sb.Append("\nSand in water: ").Append(sim.getSandInWater(ControlPanel.selectedPoint));

                text.text = sb.ToString();
            }
            else
                text.text = "select point";
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

            Refresh();
        }
    }
}