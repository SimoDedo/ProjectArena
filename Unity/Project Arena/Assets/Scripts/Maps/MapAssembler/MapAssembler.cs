﻿using System.Collections.Generic;
using Others;
using UnityEngine;

namespace Maps.MapAssembler
{
    /// <summary>
    ///     MapAssembler is an abstract class used to implement any kind of map assembler weapon. A map
    ///     assembler is used to generate a physical representation of a map starting from its matrix form.
    /// </summary>
    public abstract class MapAssembler : CoreComponent
    {
        // Wall height.
        [SerializeField] protected float wallHeight = 5f;

        // Map scale.
        [SerializeField] protected float mapScale = 1f;

        public abstract void AssembleMap(char[,] map, char wallChar, char roomChar);

        public abstract void AssembleMap(List<char[,]> maps, char wallChar, char roomChar,
            char voidChar);

        public void SetMapScale(float mapScale)
        {
            this.mapScale = mapScale;
        }

        public float GetMapScale()
        {
            return mapScale;
        }

        public float GetWallHeight()
        {
            return wallHeight;
        }

        public abstract void ClearMap();
    }
}