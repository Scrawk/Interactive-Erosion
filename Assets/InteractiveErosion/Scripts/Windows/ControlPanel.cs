using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace InterativeErosionProject
{
    enum Materials
    {
        //bedrock, stone, clay, sand,
        water
    }

    public class ControlPanel : Window
    {
        public Text text;
        public Dropdown actionDD, materialChoiseDD;
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
            foreach (var next in Enum.GetValues(typeof(Materials)))
            {
                materialChoiseDD.options.Add(new Dropdown.OptionData() { text = next.ToString() });
            }
            materialChoiseDD.RefreshShownValue();
        }
        public void onActionDDChanged()
        {
            Player.selectedAction = Action.getById(actionDD.value);
        }
        public void onMaterialChoiseDDChanged()
        {
            Player.selectedMaterial = (Materials)materialChoiseDD.value;
        }
    }
}