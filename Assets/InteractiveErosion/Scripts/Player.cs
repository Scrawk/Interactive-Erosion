using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace InterativeErosionProject
{
    //public struct Ocean
    //{
    //    public float waterLevel, terrainLevel;        
    //    public Ocean(float waterLevel, float terrainLevel)
    //    {
    //        this.waterLevel = waterLevel;
    //        this.terrainLevel = terrainLevel;            
    //    }
    //}
    public enum WorldSides
    {
        None = 0, West = 1, North=2 , East= 4, South =8
    }
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

        internal Vector2 getVector2(int tetureSize)
        {
            return new Vector2(x / (float)tetureSize, y / (float)tetureSize);
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
}

