﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using GTiff2Tiles.Core.Coordinates;
using GTiff2Tiles.Core.Enums;
using GTiff2Tiles.Core.GeoTiffs;
using GTiff2Tiles.Core.Images;
using GTiff2Tiles.Core.Localization;
using GTiff2Tiles.Core.Tiles;

// TODO: tests, changelog entry

namespace GTiff2Tiles.Core.TileMapResource
{
    /// <summary>
    /// TileMapResource's root
    /// <remarks><para/>Based on produced by MapTiler tilemapresource.xml</remarks>
    /// </summary>
    [Serializable]
    [XmlRoot(ElementName = "TileMap")]
    public record TileMap
    {
        #region Properties

        #region Private

        /// <summary>
        /// 1.0.0
        /// </summary>
        private const string DefaultVersion = "1.0.0";

        /// <summary>
        /// http://tms.osgeo.org/1.0.0
        /// </summary>
        private const string DefaultTileMapServiceLink = "http://tms.osgeo.org/1.0.0";

        /// <summary>
        /// Xml serializer
        /// </summary>
        private static XmlSerializer Serializer { get; } = new(typeof(TileMap));

        #endregion

        /// <summary>
        /// TileMap's standard version
        /// </summary>
        [XmlAttribute(AttributeName = "version")]
        public string Version { get; init; } = DefaultVersion;

        /// <summary>
        /// Link to standard
        /// </summary>
        [XmlAttribute(AttributeName = "tilemapservice")]
        public string TileMapServiceLink { get; init; } = DefaultTileMapServiceLink;

        /// <summary>
        /// Coordinate system
        /// </summary>
        [XmlElement(ElementName = "SRS")]
        public string Srs { get; init; }

        /// <summary>
        /// Bounding box
        /// </summary>
        [XmlElement(ElementName = "BoundingBox")]
        public BoundingBox BoundingBox { get; init; } = new();

        /// <summary>
        /// Origin
        /// </summary>
        [XmlElement(ElementName = "Origin")]
        public Origin Origin { get; init; } = new();

        /// <summary>
        /// Tile format
        /// </summary>
        [XmlElement(ElementName = "TileFormat")]
        public TileFormat TileFormat { get; init; } = new();

        /// <summary>
        ///  Tiles sets
        /// </summary>
        [XmlElement(ElementName = "TileSets")]
        public TileSets TileSets { get; init; } = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public TileMap() { }

        /// <summary>
        /// Initialize a new tile map
        /// </summary>
        /// <param name="srs">Coordinate system</param>
        /// <param name="boundingBox">Bounding box</param>
        /// <param name="origin">Origin</param>
        /// <param name="tileFormat">Tile format</param>
        /// <param name="tileSets">Tiles sets</param>
        /// <param name="version">Standard version
        /// <remarks><para/>1.0.0 by default</remarks></param>
        /// <param name="tileMapServiceLink">Link to standard
        /// <remarks><para/>http://tms.osgeo.org/1.0.0 by default</remarks></param>
        public TileMap(string srs, BoundingBox boundingBox, Origin origin, TileFormat tileFormat, TileSets tileSets,
                       string version = DefaultVersion, string tileMapServiceLink = DefaultTileMapServiceLink) =>
            (Version, TileMapServiceLink, Srs, BoundingBox, Origin, TileFormat, TileSets) =
            (version, tileMapServiceLink, srs, boundingBox, origin, tileFormat, tileSets);

        /// <summary>
        /// Initialize a new tile map
        /// </summary>
        /// <param name="minCoordinate">Minimal coordinate for bounding box</param>
        /// <param name="maxCoordinate">Maximal coordinate for bounding box</param>
        /// <param name="tileSize">Tile's size</param>
        /// <param name="tileExtension">Tile's extension</param>
        /// <param name="tileSetCollection">Collection of <see cref="TileSet"/>s</param>
        /// <param name="coordinateSystem">Tile's coordinate system</param>
        /// <param name="version">Standard version
        /// <remarks><para/>1.0.0 by default</remarks></param>
        /// <param name="tileMapServiceLink">Link to standard
        /// <remarks><para/>http://tms.osgeo.org/1.0.0 by default</remarks></param>
        /// <param name="originCoordinate">Origin coordinate
        /// <remarks><para/>-180.0, -90.0 by default</remarks></param>
        /// <exception cref="NotSupportedException"/>
        public TileMap(ICoordinate minCoordinate, ICoordinate maxCoordinate,
                       Size tileSize, TileExtension tileExtension, HashSet<TileSet> tileSetCollection, CoordinateSystem coordinateSystem,
                       string version = DefaultVersion, string tileMapServiceLink = DefaultTileMapServiceLink, ICoordinate originCoordinate = null)
        {
            (Version, TileMapServiceLink) = (version, tileMapServiceLink);

            Srs = coordinateSystem switch
            {
                CoordinateSystem.Epsg4326 => nameof(CoordinateSystem.Epsg4326).ToUpperInvariant(),
                CoordinateSystem.Epsg3857 => nameof(CoordinateSystem.Epsg3857).ToUpperInvariant(),
                _ => throw new NotSupportedException(string.Format(Strings.Culture, Strings.NotSupported, coordinateSystem))
            };

            BoundingBox = new(minCoordinate, maxCoordinate);
            Origin = originCoordinate == null ? new() : new(originCoordinate);
            TileFormat = new(tileSize, tileExtension);
            TileSets = new(tileSetCollection, coordinateSystem);
        }

        /// <summary>
        /// Initialize a new tile map
        /// </summary>
        /// <param name="geoTiff"><see cref="IGeoTiff"/> for which tiles were created</param>
        /// <param name="tileInfo">Info about <see cref="ITile"/>, that were created</param>
        /// <param name="tileSetCollection">Collection of <see cref="TileSet"/>s</param>
        /// <param name="version">Standard version
        /// <remarks><para/>1.0.0 by default</remarks></param>
        /// <param name="tileMapServiceLink">Link to standard
        /// <remarks><para/>http://tms.osgeo.org/1.0.0 by default</remarks></param>
        /// <param name="originCoordinate">Origin coordinate
        /// <remarks><para/>-180.0, -90.0 by default</remarks></param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="NotSupportedException"/>
        public TileMap(IGeoTiff geoTiff, ITile tileInfo, HashSet<TileSet> tileSetCollection,
                       string version = DefaultVersion, string tileMapServiceLink = DefaultTileMapServiceLink, ICoordinate originCoordinate = null)
        {
            #region Preconditions checks

            if (geoTiff is null) throw new ArgumentNullException(nameof(geoTiff));
            if (tileInfo is null) throw new ArgumentNullException(nameof(tileInfo));

            #endregion

            (Version, TileMapServiceLink) = (version, tileMapServiceLink);

            Srs = geoTiff.GeoCoordinateSystem switch
            {
                CoordinateSystem.Epsg4326 => nameof(CoordinateSystem.Epsg4326).ToUpperInvariant(),
                CoordinateSystem.Epsg3857 => nameof(CoordinateSystem.Epsg3857).ToUpperInvariant(),
                _ => throw new NotSupportedException(string.Format(Strings.Culture, Strings.NotSupported, geoTiff.GeoCoordinateSystem))
            };

            BoundingBox = new BoundingBox(geoTiff.MinCoordinate, geoTiff.MaxCoordinate);
            Origin = originCoordinate == null ? new() : new(originCoordinate);
            TileFormat = new(tileInfo.Size, tileInfo.Extension);
            TileSets = new(tileSetCollection, geoTiff.GeoCoordinateSystem);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Serializes this <see cref="TileMap"/> into <see cref="Stream"/>
        /// </summary>
        /// <param name="stream">Target <see cref="Stream"/></param>
        /// <exception cref="ArgumentNullException"/>
        public void Serialize(Stream stream)
        {
            #region Precondition checks

            if (stream is null) throw new ArgumentNullException(nameof(stream));

            #endregion

            Serializer.Serialize(stream, this);
        }

        /// <summary>
        /// Deserializes <see cref="TileMap"/> from <see cref="Stream"/>
        /// </summary>
        /// <param name="stream"><see cref="Stream"/>, from which the xml is taken</param>
        /// <returns>Deserialized <see cref="TileMap"/></returns>
        /// <exception cref="ArgumentNullException"/>
        public static TileMap Deserialize(Stream stream)
        {
            #region Precondition checks

            if (stream is null) throw new ArgumentNullException(nameof(stream));

            #endregion

            using XmlReader xmlReader = XmlReader.Create(stream);

            return Serializer.Deserialize(xmlReader) as TileMap;
        }

        #endregion
    }
}
