using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RoomEditorApp
{

    class SVGTypes
    {
        public static string Walls = "polygon";
        public static string Rooms = "polygon";
        public static string Doors = "polygon";
    }
    class Helpers
    {

        public static double angleToNorth = 8;

        public static string SVGCreateGeneric(View view, Element elementWall, double rotAngle, XYZ newOrigin, string svgTypeName)
        {
            Face wallTopFace = GetWallTopFace(view, elementWall);

            IList<XYZ> surfacePoints = wallTopFace.Triangulate().Vertices;

            IList<XYZ> transformedPoints = new List<XYZ>();

            foreach (XYZ item in surfacePoints)
            {
                transformedPoints.Add(MyTransform(item, rotAngle, newOrigin));
            }

            return WriteSVG(transformedPoints, 304.8, svgTypeName);
        }

        public static string SVGCreateDoor(View view, Element e, double rotAngle, XYZ newOrigin, string svgTypeName)
        {

            Options opt = new Options();
            opt.View = view;

            List<Line> eleList = new List<Line>();

            
            //<polygon points="50,0 150,0 150,10 50,10" stroke="red" stroke-width="1"/>


                    GeometryElement obj = e.get_Geometry(opt);


                    foreach (var o in obj)
                    {
                        GeometryInstance gi = o as GeometryInstance;

                        foreach (GeometryObject instanceObj in gi.GetInstanceGeometry())
                        {
                            if (instanceObj.GetType().ToString().Contains("Line"))
                            {
                                //TaskDialog.Show("r", instanceObj.GetType().ToString());	
                                eleList.Add(instanceObj as Line);
                            }

                        }
                    }


            LocationPoint loc = e.Location as LocationPoint;
            
            FamilyInstance fi = e as FamilyInstance;
            double length = fi.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble();
            Wall hostWall = fi.Host as Wall;

            LocationCurve lc = hostWall.Location as LocationCurve;

            XYZ wallDir = (lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0)).Normalize();
            XYZ wallPerp = wallDir.CrossProduct(XYZ.BasisZ);
            double width = hostWall.Width;

            XYZ corner1 = loc.Point + wallDir * length / 2 + wallPerp * width / 2;
            XYZ corner2 = loc.Point - wallDir * length / 2 + wallPerp * width / 2;
            XYZ corner3 = loc.Point - wallDir * length / 2 - wallPerp * width / 2;
            XYZ corner4 = loc.Point + wallDir * length / 2 - wallPerp * width / 2;


            List<XYZ> doorOpening = new List<XYZ> { corner1, corner2, corner3, corner4 };


            IList<XYZ> transformedPoints = new List<XYZ>();

            foreach (XYZ item in doorOpening)
            {
                transformedPoints.Add(MyTransform(item, rotAngle, newOrigin));
            }

            string svg = $"<polygon points=";

            return WriteSVG(transformedPoints, 304.8, svgTypeName);
        }

        public static string SVGCreateRoom(View view, Element elementRoom, double rotAngle, XYZ newOrigin, string svgTypeName)
        {          

            Face wallTopFace = GetRoomFace(view, elementRoom);

            IList<XYZ> surfacePoints = wallTopFace.Triangulate().Vertices;

            IList<XYZ> transformedPoints = new List<XYZ>();

            foreach (XYZ item in surfacePoints)
            {
                transformedPoints.Add(MyTransform(item, rotAngle, newOrigin));
            }

            SpatialElement se = elementRoom as SpatialElement;

            string roomArea = Math.Round(se.Area, 2).ToString();

            return WriteSVG(transformedPoints, 304.8, svgTypeName, roomArea);
        }

        public static string WriteSVG(IList<XYZ> pts, double scale, string svgType)
        {
            //<polygon points="5140.274,-2404.297 5140.274,-1689.247 5040.274,-1689.247  5040.274,-2404.297" />
            string result = $"<{svgType} points=\"";
            foreach (XYZ point in pts)
            {
                result += $"{Math.Round(point.Y * scale, 3)},{Math.Round(point.X * scale, 3)} ";
            };
            return $"{result}\"/>";
        }

        public static string WriteSVG(IList<XYZ> pts, double scale, string svgType, string area)
        {
            string result = $"<{svgType} area= \"{area}\" points=\"";
            foreach (XYZ point in pts)
            {
                result += $"{Math.Round(point.Y * scale, 3)},{Math.Round(point.X * scale, 3)} ";
            };
            return $"{result}\"/>";
        }

        #region Geometry
        public static XYZ MyTransform(XYZ pt, double alfa, XYZ newOrigine)
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

        public static Face GetWallTopFace(View view, Element elem)
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

        public static Face GetRoomFace(View view, Element elem)
        {
            Face topFace = null;

            Options options = new Options()
            {
                View = view,
                ComputeReferences = false,
                IncludeNonVisibleObjects = false
            };

            GeometryElement baseGeomElem = elem.get_Geometry(options);

            foreach (GeometryObject geomObj in baseGeomElem)
            {
                Solid solid = geomObj as Solid;
                if (solid != null)
                {
                    foreach (Face fa in solid.Faces)
                    {
                        if (fa != null)
                            topFace = fa;
                    }
                }
            }

            return topFace;
        }


        #endregion


        #region Not in use
        public string SvgWallOld(Element elementWall, Document doc)
        {
            Wall wall = elementWall as Wall;

            LocationCurve lc = elementWall.Location as LocationCurve;

            Line l = lc.Curve as Line;

            double scale = 304.8;

            XYZ startPoint = new XYZ(l.GetEndPoint(0).X * scale, l.GetEndPoint(0).Y * scale, 0);
            XYZ endPoint = new XYZ(l.GetEndPoint(1).X * scale, l.GetEndPoint(1).Y * scale, 0);



            XYZ lineDirection = (endPoint - startPoint).Normalize();
            XYZ perpDirection = lineDirection.CrossProduct(XYZ.BasisZ);

            double width = wall.Width * scale;


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


            return $"\n<polygon points=\"{Math.Round(corner1.X, 3)},{Math.Round(corner1.Y, 3) }" +
                $" {Math.Round(corner2.X, 3)},{Math.Round(corner2.Y, 3)}" +
                $" {Math.Round(corner3.X, 3)},{Math.Round(corner3.Y, 3)} " +
                $" {Math.Round(corner4.X, 3)},{Math.Round(corner4.Y, 3)}\" />";
        }
        #endregion
    }

    public class CategorySelectionFilter : ISelectionFilter
    {

        public string catNameChosen { get; set; }

        public CategorySelectionFilter(string catName)
        {
            this.catNameChosen = catName;
        }

        public bool AllowElement(Element e)
        {

            //if (e.Category.Name == "Structural Framing")
            if (e.Category != null & e.Category.Name == catNameChosen)
            {
                return true;
            }
            return false;
        }


        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }

    }//close class
}
