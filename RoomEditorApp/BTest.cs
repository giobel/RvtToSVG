#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Svg;
using System;
using Autodesk.Revit.UI.Selection;
//using ComponentManager = Autodesk.Windows.ComponentManager;
#endregion

namespace RoomEditorApp
{
    [Transaction( TransactionMode.ReadOnly )]

  public class BTest : IExternalCommand
  {
        static bool _debug_output = false;

        /// <summary>
        /// Add all plan view boundary loops from 
        /// given solid to the list of loops.
        /// The creation application argument is used to
        /// reverse the extrusion analyser output curves
        /// in case they are badly oriented.
        /// </summary>
        /// <returns>Number of loops added</returns>
        int AddLoops(Autodesk.Revit.Creation.Application creapp, JtLoops loops, GeometryObject obj, ref int nExtrusionAnalysisFailures)
        {
            int nAdded = 0;

            Solid solid = obj as Solid;

            if (null != solid
              && 0 < solid.Faces.Size)
            {
                Plane plane = Plane.CreateByOriginAndBasis(XYZ.Zero, XYZ.BasisX, XYZ.BasisY); // >2017
                ExtrusionAnalyzer extrusionAnalyzer = null;

                extrusionAnalyzer = ExtrusionAnalyzer.Create(solid, plane, XYZ.BasisZ);

                Face face = extrusionAnalyzer.GetExtrusionBase();

                foreach (EdgeArray a in face.EdgeLoops)
                {
                    int nEdges = a.Size;

                    List<Curve> curves = new List<Curve>(nEdges);

                    XYZ p0 = null; // loop start point
                    XYZ p; // edge start point
                    XYZ q = null; // edge end point

                    foreach (Edge e in a)
                    {
                        Curve curve = e.AsCurve();

                        if (_debug_output)
                        {
                            p = curve.GetEndPoint(0);
                            q = curve.GetEndPoint(1);
                            Debug.Print("{0} --> {1}",
                              Util.PointString(p),
                              Util.PointString(q));
                        }

                        curves.Add(curve);
                    }

                    CurveUtils.SortCurvesContiguous(creapp, curves, _debug_output);

                    q = null;

                    JtLoop loop = new JtLoop(nEdges);

                    foreach (Curve curve in curves)
                    {
                        // Todo: handle non-linear curve.
                        // Especially: if two long lines have a 
                        // short arc in between them, skip the arc
                        // and extend both lines.

                        p = curve.GetEndPoint(0);

                        loop.Add(new Point2dInt(p));

                        Debug.Assert(null == q
                          || q.IsAlmostEqualTo(p, 1e-05),
                          string.Format(
                            "expected last endpoint to equal current start point, not distance {0}",
                            (null == q ? 0 : p.DistanceTo(q))));

                        q = curve.GetEndPoint(1);



                        if (_debug_output)
                        {
                            Debug.Print("{0} --> {1}",
                              Util.PointString(p),
                              Util.PointString(q));
                        }

                        if (null == p0)
                        {
                            p0 = p; // save loop start point
                        }
                    }
                    Debug.Assert(q.IsAlmostEqualTo(p0, 1e-05),
                      string.Format(
                        "expected last endpoint to equal current start point, not distance {0}",
                        p0.DistanceTo(q)));

                    loops.Add(loop);

                    ++nAdded;
                }
            }
            return nAdded;
        }

        /// <summary>
        /// Retrieve all plan view boundary loops from 
        /// all solids of given element.
        /// </summary>
        JtLoops GetPlanViewBoundaryLoopsMultiple(Element e, ref int nFailures)
        {
            Autodesk.Revit.Creation.Application creapp = e.Document.Application.Create;

            JtLoops loops = new JtLoops(1);

            Options opt = new Options();

            GeometryElement geo = e.get_Geometry(opt);

            if (null != geo)
            {
                Document doc = e.Document;

                if (e is FamilyInstance)
                {
                    geo = geo.GetTransformed(
                      Transform.Identity);
                }

                foreach (GeometryObject obj in geo)
                {
                    AddLoops(creapp, loops, obj, ref nFailures);
                }
            }
            return loops;
        }

        /// <summary>
        /// Retrieve all plan view boundary loops from 
        /// all solids of given element united together.
        /// </summary>
        JtLoops GetPlanViewBoundaryLoops(Element e, ref int nFailures)
        {
            Autodesk.Revit.Creation.Application creapp = e.Document.Application.Create;

            JtLoops loops = new JtLoops(1);

            Options opt = new Options();

            GeometryElement geo = e.get_Geometry(opt);

            if (null != geo)
            {
                Document doc = e.Document;

                if (e is FamilyInstance)
                {
                    geo = geo.GetTransformed(Transform.Identity);
                }

                Solid union = null;

                Plane plane = Plane.CreateByOriginAndBasis(XYZ.Zero, XYZ.BasisX, XYZ.BasisY); // >2017

                foreach (GeometryObject obj in geo)
                {
                    Solid solid = obj as Solid;

                    if (null != solid
                      && 0 < solid.Faces.Size)
                    {
                        // Some solids, e.g. in the standard 
                        // content 'Furniture Chair - Office' 
                        // cause an extrusion analyser failure,
                        // so skip adding those.

                        try
                        {
                            ExtrusionAnalyzer extrusionAnalyzer = ExtrusionAnalyzer.Create(solid, plane, XYZ.BasisZ);
                        }
                        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                        {
                            solid = null;
                            ++nFailures;
                        }

                        if (null != solid)
                        {
                            if (null == union)
                            {
                                union = solid;
                            }
                            else
                            {
                                union = BooleanOperationsUtils.ExecuteBooleanOperation(union, solid, BooleanOperationsType.Union);
                            }
                        }
                    }
                }
                AddLoops(creapp, loops, union, ref nFailures);
            }
            return loops;
        }

        public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

            IList<Reference> collection = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, "Select something");

            XYZ newOrigin = uidoc.Selection.PickPoint(ObjectSnapTypes.Endpoints, "Select the new origin");

            double angle = 8;

            using (Transaction t = new Transaction(doc,"draw Lines"))
            {
                t.Start();

                StringBuilder sb = new StringBuilder();

                foreach (Reference eleRef in collection)
                {
                    Element e = doc.GetElement(eleRef);

                    sb.AppendLine(SvgWall(doc.ActiveView, e, angle, newOrigin));
                    //sb.AppendLine(SvgWall(e, doc));
                }
                
                File.WriteAllText(@"C:\Temp\svgRoom.txt", sb.ToString());

                t.Commit();
            }

            

            //TaskDialog.Show("r", path);

      return Result.Succeeded;
    }

        public XYZ MyTransform(XYZ pt, double alfa, XYZ newOrigine)
        {

            double radiantAngle = alfa * Math.PI / 180;

            //TaskDialog.Show("r", radiantAngle.ToString());

            double a = newOrigine.X - XYZ.Zero.X;
            double b = newOrigine.Y - XYZ.Zero.Y;

            //TaskDialog.Show("r", "a=" + a.ToString() + "b=" + b.ToString());

            //TaskDialog.Show("r", WritePoint(new List<XYZ>{pt}, 1));

            double xtransformed = (pt.X - a) * Math.Cos(radiantAngle) - (pt.Y - b) * Math.Sin(radiantAngle);

            //TaskDialog.Show("r", "x transf: " + xtransformed.ToString());

            double ytransformed = (pt.X - a) * Math.Sin(radiantAngle) + (pt.Y - b) * Math.Cos(radiantAngle);

            //TaskDialog.Show("r", "y transf: " + ytransformed.ToString());

            pt = new XYZ(xtransformed, ytransformed, 0);

            return pt;
        }

        public Face GetElementTopFace(View view, Element elem)
        {
            //List<Solid> solidsFound = new List<Solid>();
            Face topFace = null;

            Options options = new Options()
            {
                View = view,
                ComputeReferences = false,
                IncludeNonVisibleObjects = false
            };

            // get the solid geometry from the element
            GeometryElement baseGeomElem = elem.get_Geometry(options);
            foreach (GeometryObject geomObj in baseGeomElem)
            {
                Solid solid = geomObj as Solid;
                if (solid != null && solid.Faces.Size != 0 && solid.Edges.Size != 0)
                {
                    foreach (Face fa in solid.Faces)
                    {
                        //TaskDialog.Show( "r", WritePoint(new List<XYZ> { fa.ComputeNormal(new UV(0.5, 0.5)) }, 1) );
                        if (fa.ComputeNormal(new UV(0.5, 0.5)).IsAlmostEqualTo(XYZ.BasisZ))
                        {
                            topFace = fa;
                            break;
                        }
                    }

                    //solidsFound.Add(solid);
                }
            }

            return topFace;
        }

        public string SvgWallOld(Element elementWall, Document doc)
        {
            Wall wall = elementWall as Wall;

            LocationCurve lc = elementWall.Location as LocationCurve;

            Line l = lc.Curve as Line;

            double scale = 304.8;

            XYZ startPoint = new XYZ(l.GetEndPoint(0).X*scale, l.GetEndPoint(0).Y * scale, 0);
            XYZ endPoint = new XYZ(l.GetEndPoint(1).X*scale, l.GetEndPoint(1).Y * scale, 0);

            

            XYZ lineDirection = (endPoint-startPoint).Normalize();
            XYZ perpDirection = lineDirection.CrossProduct(XYZ.BasisZ);

            double width = wall.Width*scale;
            

            XYZ corner1 = startPoint + perpDirection * width / 2;
            XYZ corner2 = endPoint + perpDirection * width / 2;
            XYZ corner3 = endPoint - perpDirection * width / 2;
            XYZ corner4 = startPoint - perpDirection * width / 2;

            //TaskDialog.Show("r", WritePoint(new List<XYZ> { corner1, corner2, corner3, corner4}));



            Line l1 = Line.CreateBound(corner1, corner2);
            doc.Create.NewDetailCurve(doc.ActiveView, l1);
            
            Line l2 = Line.CreateBound(corner2, corner3);
            doc.Create.NewDetailCurve(doc.ActiveView, l2);
            
            Line l3 = Line.CreateBound(corner3, corner4);
            doc.Create.NewDetailCurve(doc.ActiveView, l3);

            Line l4 = Line.CreateBound(corner4, corner1);
            doc.Create.NewDetailCurve(doc.ActiveView, l4);


            //return $"<rect x=\"{xPos}\" y=\"{yPos}\" width=\"{width}\" height=\"{height}\" class=\"wall_object\" \n/>";


            return $"\n<polygon points=\"{Math.Round( corner1.X, 3)},{Math.Round( corner1.Y,3) }" +
                $" {Math.Round( corner2.X,3)},{Math.Round(corner2.Y,3)}" +
                $" {Math.Round( corner3.X,3)},{Math.Round(corner3.Y,3)} " +
                $" {Math.Round( corner4.X,3)},{Math.Round(corner4.Y,3)}\" />";
        }

        public string SvgWall(View view, Element elementWall, double rotAngle, XYZ newOrigin )
        {
            Face wallTopFace = GetElementTopFace(view, elementWall);

            IList<XYZ> surfacePoints = wallTopFace.Triangulate().Vertices;

            IList<XYZ> transformedPoints = new List<XYZ>();

            foreach (XYZ item in surfacePoints)
            {
                transformedPoints.Add(MyTransform(item, rotAngle, newOrigin));
            }

            return WriteSVG(transformedPoints, 304.8, "polygon points");
        }

        //public double scale = 304.8;
        public string WriteSVG(IList<XYZ> pts, double scale, string svgType)
        {
            //<polygon points="5140.274,-2404.297 5140.274,-1689.247 5040.274,-1689.247  5040.274,-2404.297" />
            string result = $"<{svgType}=\"";
            foreach (XYZ point in pts)
            {
                result += $"{Math.Round(point.Y*scale,3)},{Math.Round(point.X*scale,3)} ";
            };
            return $"{result}\"/>";
        }
  }
}
