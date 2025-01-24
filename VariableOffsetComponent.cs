using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Clipper2Lib;
using VariableOffset.Utilities;
using System.Linq;
using System.Drawing;

namespace VariableOffsetComponent
{
    public class VariableOffsetComponent : GH_Component
    {
        private readonly VariableOffsetClipper _variableClipper;
        private readonly Paths64 _paths;
        private readonly Paths64 _solution;

        public VariableOffsetComponent()
          : base("Variable Edge Offset", "EdgeOffset",
              "Offsets each edge of a closed curve by specified amounts",
              "Curve", "Util")
        {
            _variableClipper = new VariableOffsetClipper();
            _paths = new Paths64();
            _solution = new Paths64();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            // Changed from list to item access for curves
            pManager.AddCurveParameter("Curve", "C", "Closed curve to offset", GH_ParamAccess.item);
            pManager.AddNumberParameter("Edge Offsets", "D", "Offset distance per edge", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Offset Curves", "O", "Resulting offset curves", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "P", "Points", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Modified to handle single curve input
            Curve inputCurve = null;
            List<double> edgeOffsets = new List<double>();

            // Get and verify input data
            if (!DA.GetData(0, ref inputCurve) || !DA.GetDataList(1, edgeOffsets)) return;
            if (inputCurve == null) return;
            if (edgeOffsets.Count == 0) return;

            if (!inputCurve.IsClosed)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input curve must be closed.");
                return;
            }

            _paths.Clear();
            _solution.Clear();
            _paths.Add(RhinoCurveToPath64(inputCurve));

            // Apply edge offsets
            for (int j = 0; j < edgeOffsets.Count; j++)
            {
                _variableClipper.SetEdgeOffset(0, j, edgeOffsets[j]);
            }

            _variableClipper.Execute(_paths, 0, _solution);
            var outputCurve = Paths64ToRhinoCurves(_solution);

            // Set single curve output
            DA.SetData(0, outputCurve);
        }

        /// <summary>
        /// Converts a Paths64 object to a Rhino PolylineCurve.
        /// </summary>
        /// <param name="paths">The input Paths64 object.</param>
        /// <returns>A PolylineCurve representing the input paths.</returns>
        private static PolylineCurve Paths64ToRhinoCurves(Paths64 paths)
        {
            const double scale = 0.01; // 1/100
            var points = new List<Point3d>();

            foreach (Path64 path in paths)
            {
                points.Clear();
                points.Capacity = path.Count + 1;

                foreach (Point64 pt in path)
                {
                    points.Add(new Point3d(pt.X * scale, pt.Y * scale, 0));
                }
                points.Add(new Point3d(path[0].X * scale, path[0].Y * scale, 0));
            }

            var plCurve = new PolylineCurve(points);
            plCurve.MakeClosed(0);
            return plCurve;
        }

        /// <summary>
        /// Converts a Rhino curve to a Path64 object.
        /// </summary>
        /// <param name="curve">The input Rhino curve.</param>
        /// <param name="tolerance">The tolerance for converting the curve to a polyline.</param>
        /// <returns>A Path64 object representing the polyline approximation of the input curve.</returns>
        private static Path64 RhinoCurveToPath64(Curve curve, double tolerance = 0.1)
        {
            var polyline = curve.ToPolyline(tolerance, tolerance, 0, 0).ToPolyline();
            var path = new Path64(polyline.Count);

            foreach (Point3d pt in polyline)
            {
                path.Add(new Point64((long)(pt.X * 100), (long)(pt.Y * 100)));
            }

            return path;
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels. Icons must be embedded
        /// You can add image files to your project resources
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                {
                    var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith("vo.png"));
                    var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null) return new System.Drawing.Bitmap(stream);
                }
                return null;
            }
        }


        public override Guid ComponentGuid => new Guid("1b67ca3a-7ba9-4511-b14e-417503459e7b");
    }
}