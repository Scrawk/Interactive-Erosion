using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace InterativeErosionProject
{
    enum MaterialsForEditing
    {
        stone,
        //cobble, clay, sand,
        water, watersource, waterdrain, ocean
    }

    public class ControlPanel : Window
    {
        public Text text;
        public Dropdown actionDD, materialChoiseDD;

        public GameObject mapPointer;
        public DragPanel infoWindow;
        public ErosionSim sim;

        [SerializeField]
        private Plane referencePlane = new Plane(Vector3.up, Vector3.zero);

        static public Point selectedPoint;
        static public Action selectedAction = Action.Info;
        internal static MaterialsForEditing selectedMaterial;

        public override void Refresh()
        {

        }

        // Use this for initialization
        void Start()
        {
            rebuildDropDown();
        }

        // Update is called once per frame
        void Update()
        {
            Refresh();
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
                    var height = sim.getTerrainLevel(selectedPoint);
                    height *= (float)ErosionSim.TOTAL_GRID_SIZE / (float)ErosionSim.TEX_SIZE;
                    height += 12f;
                    mapPointer.transform.position = new Vector3(mapPointer.transform.position.x, mapPointer.transform.position.y + height, mapPointer.transform.position.z);
                    if (Input.GetMouseButton(0))
                    {
                        if (selectedAction == Action.Add)
                        {
                            if (selectedMaterial == MaterialsForEditing.water)
                                sim.AddWater(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.watersource)
                                sim.MoveWaterSource(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.waterdrain)
                                sim.MoveWaterDrainage(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.stone)
                                sim.AddToTerrainLayer(1, selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.ocean)
                                sim.AddOcean(selectedPoint);
                        }
                        if (selectedAction == Action.Remove)
                        {
                            if (selectedMaterial == MaterialsForEditing.water)
                                sim.RemoveWater(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.watersource)
                                sim.MoveWaterSource(null);
                            else if (selectedMaterial == MaterialsForEditing.waterdrain)
                                sim.MoveWaterDrainage(null);
                            else if (selectedMaterial == MaterialsForEditing.stone)
                                sim.RemoveFromTerrainLayer(1, selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.ocean)
                                sim.RemoveOcean(selectedPoint);
                        }
                        else if (selectedAction == Action.Info)
                            infoWindow.Show();


                    }
                }
            }
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
        void rebuildDropDown()
        {
            //actionDD.interactable = true;
            actionDD.ClearOptions();

            foreach (var next in Action.getAllPossible())
            {
                actionDD.options.Add(new Dropdown.OptionData() { text = next.ToString() });
            }
            actionDD.RefreshShownValue();
            //onActionDDChanged();

            materialChoiseDD.ClearOptions();
            foreach (var next in Enum.GetValues(typeof(MaterialsForEditing)))
            {
                materialChoiseDD.options.Add(new Dropdown.OptionData() { text = next.ToString() });
            }
            materialChoiseDD.RefreshShownValue();
        }
        public void onActionDDChanged()
        {
            selectedAction = Action.getById(actionDD.value);
            if (selectedAction == Action.Nothing)
                mapPointer.SetActive(false);
        }
        public void onMaterialChoiseDDChanged()
        {
            selectedMaterial = (MaterialsForEditing)materialChoiseDD.value;
        }

    }
}