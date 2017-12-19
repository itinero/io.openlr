/*
 *  Licensed to SharpSoftware under one or more contributor
 *  license agreements. See the NOTICE file distributed with this work for 
 *  additional information regarding copyright ownership.
 * 
 *  SharpSoftware licenses this file to you under the Apache License, 
 *  Version 2.0 (the "License"); you may not use this file except in 
 *  compliance with the License. You may obtain a copy of the License at
 * 
 *       http://www.apache.org/licenses/LICENSE-2.0
 * 
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using Itinero.Attributes;
using Itinero.IO.Osm;
using OpenLR;
using OpenLR.Osm;
using System.Collections.Generic;
using System.IO;

namespace Itinero.IO.OpenLR.Test.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            // STAGING: setup a routerdb.
            Download.ToFile("http://files.itinero.tech/data/OSM/planet/europe/luxembourg-latest.osm.pbf", "luxembourg-latest.osm.pbf").Wait();

            // build routerdb from raw OSM data.
            // check this for more info on RouterDb's: https://github.com/itinero/routing/wiki/RouterDb
            var routerDb = new RouterDb();
            using (var sourceStream = File.OpenRead("luxembourg-latest.osm.pbf"))
            {
                routerDb.LoadOsmData(sourceStream, Itinero.Osm.Vehicles.Vehicle.Car);
            }

            // create coder.
            var coder = new Coder(routerDb, new OsmCoderProfile());

            // STAGING: create some test linestrings: encode edge(s) and pair them off with tags.
            var constructionAttributes = new AttributeCollection();
            constructionAttributes.AddOrReplace("custom:blocked", "yes");
            var testEdges = new Dictionary<string, IAttributeCollection>();
            testEdges.Add(coder.EncodeClosestEdge(new Itinero.LocalGeo.Coordinate(49.719760878136874f, 6.117909550666809f)),
                constructionAttributes);
            testEdges.Add(coder.EncodeClosestEdge(new Itinero.LocalGeo.Coordinate(49.722292626965510f, 6.130585670471190f)),
                constructionAttributes);
            testEdges.Add(coder.EncodeClosestEdge(new Itinero.LocalGeo.Coordinate(49.724021423428425f, 6.126272678375244f)),
                constructionAttributes);
            testEdges.Add(coder.EncodeClosestEdge(new Itinero.LocalGeo.Coordinate(49.718278183113870f, 6.126626729965210f)),
                constructionAttributes);

            // TEST: decode edges and augment routerdb.
            foreach (var pair in testEdges)
            {
                var encodedLine = pair.Key;
                var attributes = pair.Value;

                coder.DecodeLine(encodedLine, attributes);
            }
        }
    }
}