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
    [Transaction( TransactionMode.Manual )]

  public class WallToSVG : IExternalCommand
  {
      public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

            ISelectionFilter WallFilter = new CategorySelectionFilter("Walls");

            IList<Reference> collection = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, WallFilter, "Select some walls");

            XYZ newOrigin = uidoc.Selection.PickPoint(ObjectSnapTypes.Endpoints, "Select the new origin");

            double angle = Helpers.angleToNorth;

            StringBuilder sb = new StringBuilder();

            foreach (Reference eleRef in collection)
            {
                Element e = doc.GetElement(eleRef);

                sb.AppendLine(Helpers.CreateGenericSVG(doc.ActiveView, e, angle, newOrigin, SVGTypes.Walls));
                //sb.AppendLine(SvgWall(e, doc));
            }
                
            File.WriteAllText(@"C:\Temp\svgRoom.txt", sb.ToString());

            //TaskDialog.Show("r", path);

      return Result.Succeeded;
    }




  }
}
