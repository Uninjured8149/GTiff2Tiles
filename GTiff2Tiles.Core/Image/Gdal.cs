﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GTiff2Tiles.Core.Exceptions.Image;
using GTiff2Tiles.Core.Helpers;
using GTiff2Tiles.Core.Localization;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace GTiff2Tiles.Core.Image
{
    /// <summary>
    /// Gdal's method to work with input files.
    /// </summary>
    public static class Gdal
    {
        #region GdalApps

        /// <summary>
        /// Runs GdalWarp with passed parameters.
        /// </summary>
        /// <param name="inputFileInfo">Input GeoTiff file.</param>
        /// <param name="outputFileInfo">Output file.</param>
        /// <param name="options">Array of string parameters.</param>
        /// <param name="callback">Delegate for progress reporting from Gdal.</param>
        /// <returns></returns>
        public static async ValueTask Warp(FileInfo inputFileInfo, FileInfo outputFileInfo,
                                string[] options,
                                OSGeo.GDAL.Gdal.GDALProgressFuncDelegate callback = null)
        {
            #region Parameters checking

            CheckHelper.CheckFile(inputFileInfo, true);
            CheckHelper.CheckFile(outputFileInfo, false);
            CheckHelper.CheckDirectory(outputFileInfo.Directory);

            #endregion

            //Initialize Gdal, if needed.
            ConfigureGdal();

            using (Dataset inputDataset = OSGeo.GDAL.Gdal.Open(inputFileInfo.FullName, Access.GA_ReadOnly))
            {
                GCHandle gcHandle =
                    GCHandle.Alloc(new[] {Dataset.getCPtr(inputDataset).Handle}, GCHandleType.Pinned);
                SWIGTYPE_p_p_GDALDatasetShadow gdalDatasetShadow =
                    new SWIGTYPE_p_p_GDALDatasetShadow(gcHandle.AddrOfPinnedObject(), false, null);
                // ReSharper disable once UnusedVariable
                await Task.Run(() =>
                {
                    using (Dataset resultDataset =
                        OSGeo.GDAL.Gdal.wrapper_GDALWarpDestName(outputFileInfo.FullName, 1,
                                                                 gdalDatasetShadow,
                                                                 new GDALWarpAppOptions(options),
                                                                 callback, string.Empty))
                    {
                        gcHandle.Free();
                    }
                });
            }

            //Was file created?
            CheckHelper.CheckFile(outputFileInfo, true);
        }

        /// <summary>
        /// Runs GdalInfo with passed parameters.
        /// </summary>
        /// <param name="inputFileInfo">Input GeoTiff file.</param>
        /// <param name="options">Array of string parameters for GdalInfo.</param>
        /// <returns><see cref="string"/> from GdalInfo.</returns>
        public static string Info(FileInfo inputFileInfo, string[] options = null)
        {
            #region Parameters checking

            CheckHelper.CheckFile(inputFileInfo, true);

            #endregion

            //Initialize Gdal, if needed.
            ConfigureGdal();

            using (Dataset inputDataset = OSGeo.GDAL.Gdal.Open(inputFileInfo.FullName, Access.GA_ReadOnly))
            {
                string gdalInfoString = OSGeo.GDAL.Gdal.GDALInfo(inputDataset, new GDALInfoOptions(options));

                if (string.IsNullOrWhiteSpace(gdalInfoString))
                    throw new GdalException(string.Format(Strings.StringIsEmpty, nameof(gdalInfoString)));

                return gdalInfoString;
            }
        }

        #endregion

        #region Initialize Gdal/Ogr

        /// <summary>
        /// Initialize Gdal, if it hadn't been initialized yet.
        /// </summary>
        private static void ConfigureGdal()
        {
            if (!GdalHelper.Usable)
                GdalHelper.Initialize();
            if (GdalHelper.IsGdalConfigured) return;
            GdalHelper.ConfigureGdal();
        }

        /// <summary>
        /// Initialize Ogr, if it hadn't been initialized yet.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private static void ConfigureOgr()
        {
            if (!GdalHelper.Usable)
                GdalHelper.Initialize();
            if (GdalHelper.IsOgrConfigured) return;
            GdalHelper.ConfigureOgr();
        }

        #endregion

        #region Image

        #region Private

        /// <summary>
        /// Gets the coordinates and pixel sizes of image.
        /// </summary>
        /// <param name="inputFileInfo">Input GeoTiff file.</param>
        /// <returns>Array of double coordinates and pixel sizes.</returns>
        private static double[] GetGeoTransform(FileInfo inputFileInfo)
        {
            #region Parameters checking

            CheckHelper.CheckFile(inputFileInfo, true);

            #endregion

            //Initialize Gdal, if needed.
            ConfigureGdal();

            using (Dataset inputDataset = OSGeo.GDAL.Gdal.Open(inputFileInfo.FullName, Access.GA_ReadOnly))
            {
                double[] geoTransform = new double[6];
                inputDataset.GetGeoTransform(geoTransform);
                return geoTransform;
            }
        }

        #endregion

        #region Internal

        /// <summary>
        /// Gets proj4 string of input file.
        /// </summary>
        /// <param name="inputFileInfo">Input GeoTiff file.</param>
        /// <returns>Proj4 string.</returns>
        internal static string GetProj4String(FileInfo inputFileInfo)
        {
            #region Parameters checking

            CheckHelper.CheckFile(inputFileInfo, true);

            #endregion

            //Initialize Gdal, if needed.
            ConfigureGdal();

            using (Dataset dataset = OSGeo.GDAL.Gdal.Open(inputFileInfo.FullName, Access.GA_ReadOnly))
            {
                string wkt = dataset.GetProjection();
                using (SpatialReference spatialReference = new SpatialReference(wkt))
                {
                    spatialReference.ExportToProj4(out string proj4String);

                    if (string.IsNullOrWhiteSpace(proj4String))
                        throw new GdalException(string.Format(Strings.StringIsEmpty, nameof(proj4String)));

                    return proj4String;
                }
            }
        }

        /// <summary>
        /// Gets the coordinates borders of the input Geotiff file.
        /// </summary>
        /// <param name="inputFileInfo">Input GeoTiff file.</param>
        /// <param name="rasterXSize">Raster's width.</param>
        /// <param name="rasterYSize">Raster's height.</param>
        /// <returns><see cref="ValueTuple{T1, T2, T3, T4}"/> with WGS84 coordinates.</returns>
        internal static (double minX, double minY, double maxX, double maxY) GetImageBorders(
            FileInfo inputFileInfo, int rasterXSize, int rasterYSize)
        {
            #region Parameters checking

            CheckHelper.CheckFile(inputFileInfo, true);
            if (rasterXSize < 0) throw new GdalException(string.Format(Strings.LesserThan, nameof(rasterXSize), 0));
            if (rasterYSize < 0) throw new GdalException(string.Format(Strings.LesserThan, nameof(rasterYSize), 0));

            #endregion

            double[] geoTransform = GetGeoTransform(inputFileInfo);

            double minX = geoTransform[0];
            double minY = geoTransform[3] - rasterYSize * geoTransform[1];
            double maxX = geoTransform[0] + rasterXSize * geoTransform[1];
            double maxY = geoTransform[3];

            return (minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Gets image sizes in pixels.
        /// </summary>
        /// <param name="inputFileInfo">Input GeoTiff file.</param>
        /// <returns><see cref="ValueTuple{T1, T2}"/> with image sizes in pixels.</returns>
        internal static (int rasterXSize, int rasterYSize) GetImageSizes(FileInfo inputFileInfo)
        {
            #region Parameters checking.

            CheckHelper.CheckFile(inputFileInfo, true);

            #endregion

            //Initialize Gdal, if needed.
            ConfigureGdal();

            using (Dataset inputDataset = OSGeo.GDAL.Gdal.Open(inputFileInfo.FullName, Access.GA_ReadOnly))
            {
                return (inputDataset.RasterXSize, inputDataset.RasterYSize);
            }
        }

        #endregion

        #endregion
    }
}
