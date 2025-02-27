﻿// Copyright (c) BruTile developers team. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using BruTile.Wmts;
using NUnit.Framework;

namespace EpsgAccessDatabaseTests;

public class CrsUnitOfMeasureRegistryTest
{
    private const string EpsgAccessDatabase = @"D:\GIS\EPSG_v8_3.mdb";

    private readonly string SqlLength = "SELECT [UOM_CODE], [UNIT_OF_MEAS_NAME], uom.FACTOR_B, uom.FACTOR_C" +
                               " FROM [Unit of Measure] AS uom WHERE (((uom.TARGET_UOM_CODE)=9001));";

    private readonly string SqlAngle = "SELECT [UOM_CODE], [UNIT_OF_MEAS_NAME], uom.FACTOR_B, uom.FACTOR_C" +
                              " FROM [Unit of Measure] AS uom WHERE (((uom.TARGET_UOM_CODE)=9101));";

    private readonly string SqlEpsgToUom =
        @"SELECT [Coordinate Reference System].COORD_REF_SYS_CODE, [Coordinate Axis].UOM_CODE, [Unit of Measure].TARGET_UOM_CODE
FROM [Coordinate Reference System] INNER JOIN ([Unit of Measure] INNER JOIN [Coordinate Axis] ON [Unit of Measure].UOM_CODE = [Coordinate Axis].UOM_CODE) ON [Coordinate Reference System].COORD_SYS_CODE = [Coordinate Axis].COORD_SYS_CODE
WHERE ((([Coordinate Axis].ORDER)=1))
ORDER BY [Coordinate Reference System].COORD_REF_SYS_CODE;";

    /// <summary>
    /// Earth radius as defined for WGS84
    /// </summary>
    private const double EarthRadius = 6378137;

    private const double EarthCircumference = 2 * EarthRadius * Math.PI;

    [Test, Explicit]
    public void TestCreateRegistryEntries()
    {
        if (!File.Exists(EpsgAccessDatabase))
            Assert.Pass("Epsg Access Database not found");

        using var cn = new System.Data.OleDb.OleDbConnection(
            $"Provider=Microsoft.Jet.OLEDB.4.0; Data Source={EpsgAccessDatabase};");
        cn.Open();
        var cmd = cn.CreateCommand();
        cmd.CommandText = SqlLength;
        using (var dr = cmd.ExecuteReader())
            while (dr.Read())
                Console.WriteLine("_registry.Add({0}, new UnitOfMeasure(\"{1}\", {2}d/{3}d));",
                    dr.GetInt32(0), dr.GetString(1),
                    dr.GetDouble(2).ToString(NumberFormatInfo.InvariantInfo),
                    dr.GetDouble(3).ToString(NumberFormatInfo.InvariantInfo));
        cmd.CommandText = SqlAngle;
        using (var dr = cmd.ExecuteReader())
            while (dr.Read())
                Console.WriteLine(
                    "_registry.Add({0}, new UnitOfMeasure(\"{1}\", 0.5 * EarthCircumference * {2}d/{3}d));",
                    dr.GetInt32(0), dr.GetString(1),
                    dr.GetDouble(2).ToString(NumberFormatInfo.InvariantInfo),
                    dr.GetDouble(3).ToString(NumberFormatInfo.InvariantInfo));
    }

    [Test, Explicit]
    public void TestCreateEpsgToUom()
    {
        if (!File.Exists(EpsgAccessDatabase))
            Assert.Pass("Epsg Access Database not found");

        using var cn = new System.Data.OleDb.OleDbConnection(
            $"Provider=Microsoft.Jet.OLEDB.4.0; Data Source={EpsgAccessDatabase};");
        cn.Open();
        var cmd = cn.CreateCommand();
        cmd.CommandText = SqlEpsgToUom;
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);
        using (var dr = cmd.ExecuteReader())
            while (dr.Read())
            {
                if ((int)dr[0] > 32768) break;

                //Console.WriteLine("{0} -> {1}", dr[0], dr[1]);
                var uom = Convert.ToInt16(dr[1]);
                if (uom == 9001) continue;
                if (Convert.ToInt16(dr[2]) == 9102)
                    uom = 9102;

                var epsg = Convert.ToUInt16(dr[0]);
                bw.Write(epsg);
                bw.Write(uom);
            }

        ms.Seek(0, SeekOrigin.Begin);
        var br = new BinaryReader(ms);
        var count = 0;
        while (ms.Position < ms.Length)
        {
            var byteValue = br.ReadByte();
            Console.Write("{0}, ", byteValue);
            count++;
            if (count % 16 == 0) Console.Write("\n");

        }
    }

    [Test]
    public void TestUnitDefinitions()
    {
        var unit = new CrsUnitOfMeasureRegistry();
        Assert.Multiple(() =>
        {
            Assert.That(unit[9001].ToMeter, Is.EqualTo(1));
            Assert.That(unit[1025].ToMeter, Is.EqualTo(0.001));
            Assert.That(unit[1033].ToMeter, Is.EqualTo(0.01));
            Assert.That(Math.Abs(360 * unit[9102].ToMeter - EarthCircumference), Is.LessThanOrEqualTo(1e-7));
        });
    }

    [Test]
    [Ignore("Use this one when finally resolving the axis order issue")]
    public void TestEpsgCodeUom()
    {
        if (!File.Exists(EpsgAccessDatabase))
            throw new IgnoreException("Epsg Access Database not found");

        var connectionString = $"Provider=Microsoft.Jet.OLEDB.4.0; Data Source={EpsgAccessDatabase};";

        using var connection = new System.Data.OleDb.OleDbConnection(connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = SqlEpsgToUom;
        var registry = new CrsUnitOfMeasureRegistry();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            if ((int)dr[0] > 32768) break;
            if (!CrsIdentifier.TryParse($"urn:ogc:def:crs:EPSG::{dr.GetInt32(0)}", out var crs)) continue;

            var uom = new UnitOfMeasure();
            Assert.DoesNotThrow(() => uom = registry[crs], "Getting unit of measure failed for {0}", crs);

            var uomCode = dr.GetInt32(1);
            if (uomCode == 9001 || uomCode == 1024)
                Assert.That(uom.ToMeter, Is.EqualTo(1d), () => $"Unit of measure ToMeter is not 1d: {crs}");
            else
                Assert.That(uom.ToMeter, Is.Not.EqualTo(1d), () => $"Unit of measure ToMeter should not be 1d: {crs}");
        }
    }
}
