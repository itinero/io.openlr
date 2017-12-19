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

using Itinero.Data.Network;
using OpenLR.Referenced.Locations;
using System.Collections.Generic;

namespace Itinero.IO.OpenLR
{
    /// <summary>
    /// Contains extension method for the referenced line.
    /// </summary>
    public static class ReferencedLineExtensions
    {
        /// <summary>
        /// Gets covered edges, once an edge is covered by more then the given tolerance it's returned.
        /// </summary>
        public static IEnumerable<long> GetCoveredEdges(this ReferencedLine line, RouterDb routerDb, float tolerancePercentage = 1f)
        {
            if (line.PositiveOffsetPercentage == 0 &&
                line.NegativeOffsetPercentage == 0)
            {
                foreach (var e in line.Edges)
                {
                    yield return e;
                }
            }
            else
            {
                var lengths = new float[line.Edges.Length];
                var totalLength = 0f;
                for (var i = 0; i < line.Edges.Length; i++)
                {
                    lengths[i] = routerDb.Network.GetEdge(line.Edges[i]).Data.Distance;
                    totalLength += lengths[i];
                }

                var offset = 0f;
                for (var i = 0; i < line.Edges.Length; i++)
                {
                    var endOffset = offset + lengths[i];

                    var startPercentage = (offset / totalLength) * 100;
                    var endPercentage = (endOffset / totalLength) * 100;

                    var startDiff = startPercentage - line.PositiveOffsetPercentage;
                    if (System.Math.Abs(startDiff) < tolerancePercentage)
                    {
                        startDiff = 0;
                    }
                    var endDiff = (100 - endPercentage) - line.NegativeOffsetPercentage;
                    if (System.Math.Abs(endDiff) < tolerancePercentage)
                    {
                        endDiff = 0;
                    }

                    if (startDiff >= 0 && endDiff >= 0)
                    {
                        yield return line.Edges[i];
                    }

                    offset = endOffset;
                }
            }
        }
    }
}