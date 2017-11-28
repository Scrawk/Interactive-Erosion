
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

                var vector4 = sim.getData4Float32bits(sim.m_terrainField[0], Player.selectedPoint);
                var height = vector4.x + vector4.y + vector4.z + vector4.w;


                sb.Append("\nGround height: ").Append(height);
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