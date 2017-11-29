
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace InterativeErosionProject
{
    public class InfoWindow : DragPanel
    {
        public Text text;
        public ErosionSim sim;
        public override void Refresh()
        {
            if (Player.selectedPoint != null)
            {
                var sb = new StringBuilder();
                sb.Append("Selected point: ").Append(Player.selectedPoint);
                //sb.Append("Water velocity: ").Append(sim.getDataRGHalf(sim.m_waterVelocity[0], Player.selectedPoint));

                var vector4 = sim.getTerrainLayers(Player.selectedPoint);
                var terrainHeight = vector4.x + vector4.y + vector4.z + vector4.w;

                sb.Append("\nTerrain height: ").Append(terrainHeight);
                sb.Append("\n").Append((Materials)0).Append(" height: ").Append(vector4.x);
                sb.Append("\n").Append((Materials)1).Append(" height: ").Append(vector4.y);
                sb.Append("\n").Append((Materials)2).Append(" height: ").Append(vector4.z);
                sb.Append("\n").Append((Materials)3).Append(" height: ").Append(vector4.w);

                sb.Append("\nSand in water: ").Append(sim.getSandInWater(Player.selectedPoint));

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