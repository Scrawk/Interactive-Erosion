
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
                sb.AppendFormat("Selected point: x = {0}, y = {1}", ControlPanel.selectedPoint.x.ToString("F3"), ControlPanel.selectedPoint.y.ToString("F3"));


                var vector4 = sim.getTerrainLayers(ControlPanel.selectedPoint);
                var terrainHeight = vector4.x + vector4.y + vector4.z + vector4.w;
                sb.Append("\nTerrain height: ").Append(terrainHeight);
                sb.Append("\n\t").Append((Layers)0).Append(" height: ").Append(vector4.x);
                sb.Append("\n\t").Append((Layers)1).Append(" height: ").Append(vector4.y);
                sb.Append("\n\t").Append((Layers)2).Append(" height: ").Append(vector4.z);
                sb.Append("\n\t").Append((Layers)3).Append(" height: ").Append(vector4.w);

                //var waterHeight = sim.getWaterLevel(ControlPanel.selectedPoint);
                //sb.Append("\nWater height: ").Append(waterHeight);
                //sb.Append("\nWater height: ").Append(sim.getWaterLevel(ControlPanel.selectedPoint));

                //sb.Append("Water velocity: ").Append(sim.getWaterVelocity(ControlPanel.selectedPoint));

                //sb.Append("\nTotal height: ").Append(waterHeight + terrainHeight);

                //sb.Append("\nSand in water: ").Append(sim.getSedimentInWater(ControlPanel.selectedPoint));

                //Vector4 velocity4 = sim.getWaterVelocity(ControlPanel.selectedPoint);
                //Vector2 velocity2 = new Vector2(velocity4.x, velocity4.y);

                //sb.Append("\nWater velocity: ").Append(velocity2.magnitude);
                //sb.Append("\nWater velocity: ").Append(velocity4);

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