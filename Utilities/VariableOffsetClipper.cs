using Clipper2Lib;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VariableOffset.Utilities
{
    // Create this in a new file, separate from the original library
    public class VariableOffsetClipper
    {
        private readonly Dictionary<(int pathIndex, int edgeIndex), double> _edgeOffsets;
        private double _defaultDelta = 1.0;

        public VariableOffsetClipper()
        {
            _edgeOffsets = new Dictionary<(int pathIndex, int edgeIndex), double>();
        }

        public void SetEdgeOffset(int pathIndex, int edgeIndex, double offset)
        {
            _edgeOffsets[(pathIndex, edgeIndex)] = offset;
        }

        public void Execute(Paths64 paths, double defaultDelta, Paths64 solution)
        {
            foreach (Path64 path in paths)
            {
                Path64 offsetPath = new Path64();
                int count = path.Count;
                List<(Point64, Point64)> offsetLines = new List<(Point64, Point64)>();

                // Create offset lines
                for (int i = 0; i < count - 1; i++)
                {
                    double offset = _edgeOffsets.TryGetValue((0, i), out double customDelta) ? customDelta : defaultDelta;
                    

                    var p1 = path[i];
                    var p2 = path[i + 1];

                    // Calculate perpendicular unit vector
                    double dx = p2.X - p1.X;
                    double dy = p2.Y - p1.Y;
                    double length = Math.Sqrt(dx * dx + dy * dy);
                    double nx = -dy / length;
                    double ny = dx / length;

                    // Create offset line
                    var offsetP1 = new Point64(
                        p1.X + (long)(nx * offset * 100),
                        p1.Y + (long)(ny * offset * 100)
                    );
                    var offsetP2 = new Point64(
                        p2.X + (long)(nx * offset * 100),
                        p2.Y + (long)(ny * offset * 100)
                    );

                    offsetLines.Add((offsetP1, offsetP2));
                }

               

                // Find intersections
                for (int i = 0; i < offsetLines.Count; i++)
                {
                    var line1 = offsetLines[i];
                    var line2 = offsetLines[(i + 1) % offsetLines.Count];

                    var intersection = IntersectLines(line1.Item1, line1.Item2, line2.Item1, line2.Item2);
                    offsetPath.Add(intersection);
                }

                solution.Add(offsetPath);
            }
        }

        private Point64 IntersectLines(Point64 p1, Point64 p2, Point64 p3, Point64 p4)
        {
            double x1 = p1.X, y1 = p1.Y;
            double x2 = p2.X, y2 = p2.Y;
            double x3 = p3.X, y3 = p3.Y;
            double x4 = p4.X, y4 = p4.Y;

            double denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Math.Abs(denominator) < 1e-10) return p2;

            double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denominator;

            return new Point64(
                (long)(x1 + t * (x2 - x1)),
                (long)(y1 + t * (y2 - y1))
            );
        }
    }
}
