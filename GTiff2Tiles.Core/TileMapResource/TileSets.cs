﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using GTiff2Tiles.Core.Enums;
using GTiff2Tiles.Core.Localization;

// TODO: tests

namespace GTiff2Tiles.Core.TileMapResource
{
    /// <summary>
    /// TileMap's TileSets
    /// </summary>
    [Serializable]
    [XmlRoot(ElementName = "TileSets")]
    public record TileSets
    {
        #region Properties

        /// <summary>
        /// Collection of <see cref="TileSet"/>s
        /// </summary>
        [XmlElement(ElementName = "TileSet")]
        public HashSet<TileSet> TileSetCollection { get; }

        /// <summary>
        /// Tile's profile
        /// </summary>
        [XmlAttribute(AttributeName = "profile")]
        public string Profile { get; init; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public TileSets() { }

        /// <summary>
        /// Initialize a new tile sets
        /// </summary>
        /// <param name="tileSetCollection">Collection of <see cref="TileSet"/>s</param>
        /// <param name="profile">Tile's profle</param>
        public TileSets(HashSet<TileSet> tileSetCollection, string profile) => (TileSetCollection, Profile) = (tileSetCollection, profile);

        /// <summary>
        /// Initialize a new tile sets
        /// </summary>
        /// <param name="tileSetCollection">Collection of <see cref="TileSet"/>s</param>
        /// <param name="coordinateSystem">Tile's <see cref="CoordinateSystem"/></param>
        /// <exception cref="NotSupportedException"/>
        public TileSets(HashSet<TileSet> tileSetCollection, CoordinateSystem coordinateSystem)
        {
            TileSetCollection = tileSetCollection;

            Profile = coordinateSystem switch
            {
                CoordinateSystem.Epsg4326 => "geodetic",
                CoordinateSystem.Epsg3857 => "mercator",
                _ => throw new NotSupportedException(string.Format(Strings.Culture, Strings.NotSupported,
                                                                   coordinateSystem))
            };
        }

        #endregion
    }
}
