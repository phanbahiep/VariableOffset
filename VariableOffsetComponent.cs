using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Clipper2Lib;
using VariableOffset.Utilities;
using Rhino;
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

        protected override Bitmap Icon
        {
            get
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream("vo.png");
                return new Bitmap(stream);
            }
        }


        public override Guid ComponentGuid => new Guid("1b67ca3a-7ba9-4511-b14e-417503459e7b");
    }
}