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
    //public static class IntExtensions
    //{
    //    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    //    {
    //        if (val.CompareTo(min) < 0) return min;
    //        else if (val.CompareTo(max) > 0) return max;
    //        else return val;
    //    }
    //}
    public class Player : MonoBehaviour
    {
        public GameObject mapPointer;
        public DragPanel infoWindow;
        public ErosionSim sim;

        [SerializeField]
        private Plane referencePlane = new Plane(Vector3.up, Vector3.zero);
        public enum Action { nothing, dig, rise, info, spring }
        static public Action selectedAction = Action.info;
        static public Point selectedPoint;


        void Update()
        {
            //HexVertex found = null;

            if (Player.selectedAction != Player.Action.nothing && Input.GetMouseButton(0))
            {
                //CastFindVertex();
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                float rayDistance;
                if (referencePlane.Raycast(ray, out rayDistance))
                {
                    // convert this to texture UV
                    var clickedPosition = ray.GetPoint(rayDistance);
                    int TOTAL_GRID_SIZE = 512;
                    int xInTexture = (int)(clickedPosition.x + TOTAL_GRID_SIZE / 2);
                    int yInTexture = (int)(clickedPosition.y + TOTAL_GRID_SIZE / 2);

                    if (xInTexture >= 0 && xInTexture < TOTAL_GRID_SIZE
                        && yInTexture >= 0 && yInTexture < TOTAL_GRID_SIZE)
                        selectedPoint = new Point(xInTexture, yInTexture);
                    else
                        selectedPoint = null;

                    if (selectedPoint == null)
                        mapPointer.SetActive(false);
                    else
                    {
                        mapPointer.SetActive(true);
                        mapPointer.transform.position = clickedPosition;
                        
                    }
                }

                //var found = map.findVertexByNumber(CastFindVertex());
                //if (found != null && Input.GetMouseButton(0))
                {
                    //selectedVertex = found;
                    switch (Player.selectedAction)
                    {
                        case Player.Action.dig:
                            //found.setGroundLevel(Application.safeRound(found.getGroundLevel() + oneStepGroundChange * -1f));
                            break;

                        case Player.Action.rise:
                            //found.setGroundLevel(Application.safeRound(found.getGroundLevel() + oneStepGroundChange));
                            break;
                        case Player.Action.spring:
                            //found.setWaterLevel(Application.safeRound(found.getWaterLevel() + oneStepWaterChange));
                            break;
                        case Player.Action.info:
                            infoWindow.Show();
                            break;
                    }
                }
                // if (found != null && this.active)
                //Refresh();
            }
        }
    }
}