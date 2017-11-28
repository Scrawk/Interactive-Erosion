
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InfoWindow : DragPanel
{
    public Text text;
    //private Camera myCamera;
    public GameObject mapPointer;
    [SerializeField]
    public Plane referencePlane = new Plane(Vector3.up, Vector3.zero);    
    public override void Refresh()
    {
        text.text = "test";
    }

    // Use this for initialization
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {

        // HexVertex found = null;
        
        if (Player.action != Player.Action.nothing && Input.GetMouseButton(0))
        {
            //CastFindVertex();
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float rayDistance;
            if (referencePlane.Raycast(ray, out rayDistance))
                mapPointer.transform.position = ray.GetPoint(rayDistance);

            //var found = map.findVertexByNumber(CastFindVertex());
            //if (found != null && Input.GetMouseButton(0))
            {
                //selectedVertex = found;
                switch (Player.action)
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
                        Show();
                        break;
                }
            }
            // if (found != null && this.active)
            //Refresh();
        }
        Refresh();
    }    
}
