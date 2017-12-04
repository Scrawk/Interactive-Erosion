using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InterativeErosionProject
{
    public enum MaterialsForEditing
    {
        stone,
        cobble, clay, sand,
        water, watersource, waterdrain, ocean, sediment
    }

    public class ControlPanel : Window
    {
        public Text text;
        public Dropdown actionDD, materialChoiseDD;

        public GameObject mapPointer;
        public DragPanel infoWindow;
        public ErosionSim sim;

        [SerializeField]
        //private Plane referencePlane = new Plane(Vector3.up, Vector3.zero);

        static public Vector2 selectedPoint;
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
            if (selectedAction != Action.Nothing && Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                // currently. works as for flat plane
                var clickedPosition = RaycastToMesh();

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
                            else if (selectedMaterial == MaterialsForEditing.ocean)
                                sim.AddOcean(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.sediment)
                                sim.AddSediment(selectedPoint);
                            else //rest of materials
                                sim.AddToTerrainLayer(selectedMaterial, selectedPoint);
                        }
                        if (selectedAction == Action.Remove)
                        {
                            if (selectedMaterial == MaterialsForEditing.water)
                                sim.RemoveWater(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.watersource)
                                sim.MoveWaterSource(default(Vector2));
                            else if (selectedMaterial == MaterialsForEditing.waterdrain)
                                sim.MoveWaterDrainage(default(Vector2));
                            else if (selectedMaterial == MaterialsForEditing.ocean)
                                sim.RemoveOcean(selectedPoint);
                            else if (selectedMaterial == MaterialsForEditing.sediment)
                                sim.RemoveSediment(selectedPoint);
                            else //rest of materials
                                sim.RemoveFromTerrainLayer(selectedMaterial, selectedPoint);
                        }
                        else if (selectedAction == Action.Info)
                            infoWindow.Show();
                    }
                }
            }
        }
        
        private Vector3 RaycastToMesh()
        {
            // Bit shift the index of the layer (8) to get a bit mask
            //var layerMask = 1 << 5;
            // This would cast rays only against colliders in layer 8.
            // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.            
            //layerMask = ~layerMask;
            RaycastHit hit;
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity))//, layerMask
            {
                {
                    //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
                    selectedPoint = default(Vector2);
                    //Debug.Log("Missed");
                    return default(Vector2);// -1;
                }
            }
            //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            MeshCollider meshCollider = hit.collider as MeshCollider;

            if (meshCollider == null || meshCollider.sharedMesh == null)
            {
                selectedPoint = default(Vector2);
                //Debug.Log("Missed");
                return default(Vector2);//2;
            }
            selectedPoint = hit.textureCoord;
            return hit.point;
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
        //private Vector2 RaycastToPlain()
        //{
        //    Vector3 clickedPosition = default(Vector3);
        //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //    float rayDistance;
        //    if (referencePlane.Raycast(ray, out rayDistance))
        //    {
        //        // convert this to texture UV
        //        clickedPosition = ray.GetPoint(rayDistance);

        //        int xInTexture = (int)(clickedPosition.x * 2f + ErosionSim.TOTAL_GRID_SIZE);
        //        int yInTexture = (int)((clickedPosition.z) * 2f + ErosionSim.TOTAL_GRID_SIZE);

        //        if (xInTexture >= 0 && xInTexture <= ErosionSim.MAX_TEX_INDEX
        //            && yInTexture >= 0 && yInTexture <= ErosionSim.MAX_TEX_INDEX)
        //            selectedPoint = new Point(xInTexture, yInTexture);
        //        else
        //            selectedPoint = null;
        //    }
        //    return clickedPosition;
        //}
    }
}