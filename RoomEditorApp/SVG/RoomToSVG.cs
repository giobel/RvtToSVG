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
using Autodesk.Revit.DB.Architecture;
using System.Linq;
#endregion

namespace RoomEditorApp
{
    [Transaction( TransactionMode.Manual )]

  public class RoomToSVG : IExternalCommand
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

            ISelectionFilter roomFilter = new CategorySelectionFilter("Rooms"); 

            IList<Reference> collection = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, roomFilter, "Select any rooms");

            XYZ newOrigin = uidoc.Selection.PickPoint(ObjectSnapTypes.Endpoints, "Select the new origin");

            double angle = Helpers.angleToNorth;

            StringBuilder sb = new StringBuilder();

            List<Element> roomList = new List<Element>();

            foreach (Reference eleRef in collection)
            {
                roomList.Add(doc.GetElement(eleRef));

            }
            //group rooms by their name so we can place them in a <g class=roomName> </g> paragraph (easier to select them all at once in js).
            var groupedRooms = roomList.GroupBy(x => x.Name.Split(null).First().ToLower());

            foreach (var groupItem in groupedRooms)
            {
                string name = groupItem.First().Name.Split(null).First().ToLower();

                sb.AppendLine($"<g class=\"rooms-{name}\" id=\"{name}\">");

                foreach (Element room in groupItem)
                {
                    sb.AppendLine(Helpers.SVGCreateRoom(doc.ActiveView, room, angle, newOrigin, SVGTypes.Rooms));
                }

                sb.AppendLine("</g>");
            }
            
                //sb.AppendLine(SvgWall(e, doc));
            
                 
            File.WriteAllText(@"C:\Temp\svgRoom.txt", sb.ToString());

            //TaskDialog.Show("r", path);

      return Result.Succeeded;
    }

  }
}
