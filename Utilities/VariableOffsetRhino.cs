using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VariableOffset.Utilities
{
    /// <summary>
    /// Provides functionality to create variable offset paths using RhinoCommon geometry types.
    /// </summary>
    public class VariableOffsetRhino
    {
        private readonly Dictionary<(int pathIndex, int edgeIndex), double> _edgeOffsets;
        private double _defaultDelta = 1.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableOffsetRhino"/> class.
        /// </summary>
        public VariableOffsetRhino()
        {
            _edgeOffsets = new Dictionary<(int pathIndex, int edgeIndex), double>();
        }

        /// <summary>
        /// Sets the offset for a specific edge in a path.
        /// </summary>
        /// <param name="pathIndex">The index of the path.</param>
        /// <param name="edgeIndex">The index of the edge within the path.</param>
        /// <param name="offset">The offset value to set.</param>
        public void SetEdgeOffset(int pathIndex, int edgeIndex, double offset)
        {
            _edgeOffsets[(pathIndex, edgeIndex)] = offset;
        }

        /// <summary>
        /// Executes the offset operation on the provided paths.
        /// </summary>
        /// <param name="paths">The input paths to offset as lists of Point3d.</param>
        /// <param name="defaultDelta">The default offset value to use if no specific offset is set for an edge.</param>
        /// <returns>The output paths after applying the offset.</returns>
        public List<List<Point3d>> Execute(List<List<Point3d>> paths, double defaultDelta)
        {
            var solution = new List<List<Point3d>>();

            foreach (var path in paths)
            {
                var offsetPath = new List<Point3d>();
                int count = path.Count;
                var offsetLines = new List<Line>();

                // Create offset lines
                for (int i = 0; i < count - 1; i++)
                {
                    double offset = _edgeOffsets.TryGetValue((0, i), out double customDelta) ? customDelta : defaultDelta;

                    var p1 = path[i];
                    var p2 = path[i + 1];

                    // Calculate perpendicular vector
                    var edge = p2 - p1;
                    var perpendicular = Vector3d.CrossProduct(edge, Vector3d.ZAxis);
                    perpendicular.Unitize();

                    // Create offset line 
                    var offsetP1 = p1 + perpendicular * offset;
                    var offsetP2 = p2 + perpendicular * offset;
                    offsetLines.Add(new Line(offsetP1, offsetP2));
                }

                // Find intersections
                for (int i = 0; i < offsetLines.Count; i++)
                {
                    var line1 = offsetLines[i];
                    var line2 = offsetLines[(i + 1) % offsetLines.Count];

                    var intersection = IntersectLines(line1, line2);
                    offsetPath.Add(intersection);
                }

                solution.Add(offsetPath);
            }

            return solution;
        }

        /// <summary>
        /// Finds the intersection point of two lines using RhinoCommon's intersection methods.
        /// </summary>
        /// <param name="line1">The first line.</param>
        /// <param name="line2">The second line.</param>
        /// <returns>The intersection point of the two lines.</returns>
        private Point3d IntersectLines(Line line1, Line line2)
        {
            double line1Parameter, line2Parameter;
            if (Rhino.Geometry.Intersect.Intersection.LineLine(line1, line2,
                out line1Parameter, out line2Parameter,
                1e-10, // intersection tolerance
                false)) // finite line segments only
            {
                return line1.PointAt(line1Parameter);
            }

            // If no intersection is found, return the end point of the first line
            return line1.To;
        }
    }
}