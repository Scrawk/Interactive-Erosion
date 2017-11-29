using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace InterativeErosionProject
{
    public class Point
    {
        public int x, y;
        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public override string ToString()
        {
            return "x = " + x + "; y = " + y;
        }
    }
    public class Action
    {
        private readonly string name;
        static private readonly List<Action> list = new List<Action>();
        static public Action Nothing = new Action("Nothing");
        static public Action Add = new Action("Add");
        static public Action Remove = new Action("Remove");
        static public Action Info = new Action("Info");
        private Action(string name)
        {
            this.name = name;
            list.Add(this);
        }
        static public IEnumerable<Action> getAllPossible()
        {
            foreach (var item in list)
            {
                yield return item;
            }
        }
        public override string ToString()
        {
            return name;
        }

        internal static Action getById(int value)
        {
            return list[value];
        }
    }
    public class Player : MonoBehaviour
    {
        public GameObject mapPointer;
        public DragPanel infoWindow;
        public ErosionSim sim;

        [SerializeField]
        private Plane referencePlane = new Plane(Vector3.up, Vector3.zero);

        static public Point selectedPoint;
        static public Action selectedAction;
        internal static Materials selectedMaterial;

        void Update()
        {
            if (selectedAction != Action.Nothing && Input.GetMouseButton(0))
            {
                var clickedPosition = raycastSelectedPoint();

                if (selectedPoint == null)
                    mapPointer.SetActive(false);
                else
                {
                    mapPointer.SetActive(true);
                    mapPointer.transform.position = clickedPosition;

                    // lift pointer at terrain height
                    var height = sim.getTerrainLevel(Player.selectedPoint);                     
                    height *= (float)ErosionSim.TOTAL_GRID_SIZE / (float)ErosionSim.TEX_SIZE;
                    height += 12f;
                    mapPointer.transform.position = new Vector3(mapPointer.transform.position.x, mapPointer.transform.position.y + height, mapPointer.transform.position.z);
                    if (Input.GetMouseButton(0))
                    {
                        if (selectedAction == Action.Add)
                        {
                            if (selectedMaterial == Materials.water)
                                sim.addWater(new Vector2((float)selectedPoint.x / ErosionSim.MAX_TEX_INDEX, (float)selectedPoint.y / ErosionSim.MAX_TEX_INDEX), 0.001f, 10f);
                        }
                        if (selectedAction == Action.Remove)
                        {
                            if (selectedMaterial == Materials.water)
                                sim.addWater(new Vector2((float)selectedPoint.x / ErosionSim.MAX_TEX_INDEX, (float)selectedPoint.y / ErosionSim.MAX_TEX_INDEX), 0.001f, -10f);
                        }
                        else if (selectedAction == Action.Info)
                            infoWindow.Show();
                    }
                }
            }
        }
        static private bool runSimulation = false;
        static public void StartSimulation()
        {
            runSimulation = true;
        }
        static public void PauseSimulation()
        {
            runSimulation = false;
        }
        static public bool isSimulationOn()
        {
            return runSimulation;
        }
        private Vector3 raycastSelectedPoint()
        {
            Vector3 clickedPosition = default(Vector3);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float rayDistance;
            if (referencePlane.Raycast(ray, out rayDistance))
            {
                // convert this to texture UV
                clickedPosition = ray.GetPoint(rayDistance);

                int xInTexture = (int)(clickedPosition.x * 2f + ErosionSim.TOTAL_GRID_SIZE);
                int yInTexture = (int)((clickedPosition.z) * 2f + ErosionSim.TOTAL_GRID_SIZE);

                if (xInTexture >= 0 && xInTexture <= ErosionSim.MAX_TEX_INDEX
                    && yInTexture >= 0 && yInTexture <= ErosionSim.MAX_TEX_INDEX)
                    selectedPoint = new Point(xInTexture, yInTexture);
                else
                    selectedPoint = null;
            }
            return clickedPosition;
        }
    }
}

