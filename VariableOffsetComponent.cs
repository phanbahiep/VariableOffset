using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using VariableOffset.Utilities;
using System.Linq;
using System.Drawing;

namespace VariableOffsetComponent
{
    public class VariableOffsetComponent : GH_Component
    {
        private readonly VariableOffsetRhino _variableOffset;

        public VariableOffsetComponent()
          : base("Variable Edge Offset", "EdgeOffset",
              "Offsets each edge of a closed curve by specified amounts",
              "Curve", "Util")
        {
            _variableOffset = new VariableOffsetRhino();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
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

            // Find curve direction and flip to ensure negative values offset inward
            CurveOrientation direction = inputCurve.ClosedCurveOrientation();
            if (direction == CurveOrientation.Clockwise)
            {
                inputCurve.Reverse();
            }

            // Convert curve to polyline points
            var polyline = inputCurve.ToPolyline(0.1, 0.1, 0, 0).ToPolyline();

            // Prepare input for variable offset
            var inputPoints = new List<Point3d>(polyline);
            var paths = new List<List<Point3d>> { inputPoints };

            // Apply edge offsets
            for (int j = 0; j < edgeOffsets.Count; j++)
            {
                _variableOffset.SetEdgeOffset(0, j, edgeOffsets[j]);
            }

            // Execute offset operation
            var offsetPaths = _variableOffset.Execute(paths, 0);

            // Convert result to curve
            if (offsetPaths.Count > 0)
            {
                var outputPoints = offsetPaths[0];
                outputPoints.Add(outputPoints[0]); // Close the curve
                var outputCurve = new PolylineCurve(outputPoints);

                // Set outputs
                DA.SetData(0, outputCurve);
                DA.SetDataList(1, outputPoints);
            }
        }

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