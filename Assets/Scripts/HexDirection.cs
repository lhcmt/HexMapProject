using UnityEngine;
using System.Collections;

    public enum HexDirection
    {
        //右上角1，右边2，顺时针
        NE, E, SE, SW, W, NW
    }
    //边界种类

    public static class HexDirectionExtensions
    {

        public static HexDirection Opposite(this HexDirection direction)
        {
            return (int)direction < 3 ? (direction + 3) : (direction - 3);
        }
        //前一个，123456
        public static HexDirection Previous(this HexDirection direction)
        {
            return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
        }

        public static HexDirection Next(this HexDirection direction)
        {
            return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
        }
        public static HexDirection Previous2(this HexDirection direction)
        {
            direction -= 2;
            return direction >= HexDirection.NE ? direction : (direction + 6);
        }

        public static HexDirection Next2(this HexDirection direction)
        {
            direction += 2;
            return direction <= HexDirection.NW ? direction : (direction - 6);
        }
    }

