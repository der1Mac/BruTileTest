﻿// Copyright (c) BruTile developers team. All rights reserved. See License.txt in the project root for license information.

using System;
using BruTile.Predefined;
using BruTile.Wmsc;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace BruTile.Tests.Wmsc;

[TestFixture]
internal class WmscRequestTests
{
    [Test]
    public void WmscRequestNoVersion()
    {
        var wmscUrlBuilder = new WmscUrlBuilder(new Uri("http://testserver.com"), new GlobalSphericalMercator(YAxis.TMS), ["Layer One"]);
        var ti = new TileInfo { Index = new TileIndex(0, 0, 0) };
        var uri = wmscUrlBuilder.GetUrl(ti);
        StringAssert.DoesNotContain("VERSION=", uri.ToString());
        StringAssert.Contains("SRS=", uri.ToString());
    }

    [Test]
    public void WmscRequestVersion111()
    {
        var request = new WmscUrlBuilder(new Uri("http://testserver.com"), new GlobalSphericalMercator(YAxis.TMS), ["Layer One"], null, null, "1.1.1");
        var ti = new TileInfo { Index = new TileIndex(0, 0, 0) };
        var uri = request.GetUrl(ti);
        StringAssert.Contains("VERSION=1.1.1", uri.ToString());
        StringAssert.Contains("SRS=", uri.ToString());
    }

    [Test]
    public void WmscRequestVersion130()
    {
        var request = new WmscUrlBuilder(new Uri("http://testserver.com"), new GlobalSphericalMercator(YAxis.TMS), ["Layer One"], null, null, "1.3.0");
        var ti = new TileInfo { Index = new TileIndex(0, 0, 0) };
        var uri = request.GetUrl(ti);
        StringAssert.Contains("VERSION=1.3.0", uri.ToString());
        StringAssert.Contains("CRS=", uri.ToString());
    }
}
