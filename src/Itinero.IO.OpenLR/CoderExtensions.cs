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
using OpenLR;
using OpenLR.Referenced;
using OpenLR.Referenced.Locations;
using System;

namespace Itinero.IO.OpenLR
{
    /// <summary>
    /// A collection of extension methods for the openlr coder.
    /// </summary>
    public static class CoderExtensions
    {
        /// <summary>
        /// Decodes the given line and augments all covered edges with the given attributes.
        /// </summary>
        /// <returns>True if the line is decoded properly and edges have been augmented with new data.</returns>
        public static bool DecodeLine(this Coder coder, string encodedLine, IAttributeCollection attributes, 
            Func<IAttributeCollection, IAttributeCollection> reverse = null)
        {
            // check if routerdb is readonly or not.
            if (coder.Router.Db.EdgeProfiles.IsReadonly)
            {
                throw new ArgumentException("Cannot augment routerdb, EdgeProfiles are readonly.");
            }
            if (coder.Router.Db.EdgeMeta.IsReadonly)
            {
                throw new ArgumentException("Cannot augment routerdb, EdgeMeta is readonly.");
            }

            var vehicle = coder.Profile.Profile.Parent;
            var success = false;

            // decode line.
            var decoded = coder.Decode(encodedLine);
            var line = decoded as ReferencedLine;
            if (line == null)
            {
                return false;
            }

            // loop over all covered edges.
            var coveredEdges = line.GetCoveredEdges(coder.Router.Db);
            foreach (var directedEdgeId in coveredEdges)
            {
                // get the edge id and set forward flag.
                uint edgeId = (uint)(directedEdgeId - 1);
                var forward = true;
                if (directedEdgeId < 0)
                {
                    edgeId = (uint)((-directedEdgeId) - 1);
                    forward = false;
                }

                // reverse if needed and possible.
                var attributesToApply = attributes;
                if (!forward && reverse != null)
                {
                    attributesToApply = reverse(attributes);
                }

                // get edge details.
                var edge = coder.Router.Db.Network.GetEdge(edgeId);
                var profile = coder.Router.Db.EdgeProfiles.Get(edge.Data.Profile);
                var meta = coder.Router.Db.EdgeMeta.Get(edge.Data.MetaId);
                var existingAttributes = new AttributeCollection(meta);
                existingAttributes.AddOrReplace(profile);

                // add new attributes
                existingAttributes.AddOrReplace(attributesToApply);

                // update edge.
                var newProfile = new AttributeCollection();
                var newMeta = new AttributeCollection();
                foreach (var a in existingAttributes)
                {
                    if (vehicle.ProfileWhiteList.Contains(a.Key))
                    {
                        newProfile.AddOrReplace(a.Key, a.Value);
                    }
                    else
                    {
                        newMeta.AddOrReplace(a.Key, a.Value);
                    }
                }

                // add data to routerdb.
                var profileId = edge.Data.Profile;
                if (!newProfile.ContainsSame(profile))
                {
                    profileId = (ushort)coder.Router.Db.EdgeProfiles.Add(newProfile);
                }
                var metaId = edge.Data.MetaId;
                if (!newMeta.ContainsSame(meta))
                {
                    metaId = coder.Router.Db.EdgeMeta.Add(newMeta);
                }

                // update edge in place.
                coder.Router.Db.Network.UpdateEdgeData(edgeId, new Itinero.Data.Network.Edges.EdgeData()
                {
                    Distance = edge.Data.Distance,
                    MetaId = metaId,
                    Profile = profileId
                });

                success = true;
            }
            return success;
        }

        /// <summary>
        /// Encodes the edge that the given location resolves on.
        /// </summary>
        public static string EncodeClosestEdge(this Coder coder, Itinero.LocalGeo.Coordinate location)
        {
            return coder.Encode(coder.BuildEdge(
                coder.Router.Resolve(coder.Profile.Profile, location).EdgeIdDirected()));
        }
    }
}