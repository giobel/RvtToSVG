# Based on RoomEditorApp

Revit add-in part of cloud-based, real-time, round-trip, 2D Revit model editor.

RoomEditorApp implements export of Revit BIM data to a web based cloud database, reimport, and subscription to automatic changes to update the BIM in real time as the user edits a simplified graphical 2D view in any browser on any device.

The database part is implemented by
the [roomedit](https://github.com/jeremytammik/roomedit)
[CouchDB](https://couchdb.apache.org) app.

Please refer to [The Building Coder](http://thebuildingcoder.typepad.com) for
more information, especially in
the [cloud](http://thebuildingcoder.typepad.com/blog/cloud)
and [desktop](http://thebuildingcoder.typepad.com/blog/desktop) categories.

Here is a
recent [summary and overview description](http://thebuildingcoder.typepad.com/blog/2015/11/connecting-desktop-and-cloud-room-editor-update.html#3) of
this project.


# Added

Export from Revit to SVG file. See BTest.cs