using System;
using Agw.Generic;
using Agw.Generic.ExtensionMethods;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;


namespace AgwRodBreak
{

    public class RodBreak
    {

        [CommandMethod("AGWRB")]
        public void AgwRb()
        {
            //first select lines
            string keyFlip = "Flip";
            var pEntopt = new PromptEntityOptions(Environment.NewLine + "Select First line or ");
            pEntopt.SetRejectMessage(Environment.NewLine + "Only Lines allowed");
            pEntopt.AddAllowedClass(typeof(Line), false);
            pEntopt.Keywords.Add(keyFlip);

            var result = Active.Editor.GetEntity(pEntopt);

            switch (result.Status)
            {
                case PromptStatus.OK:

                    DrawRodEnd(result);
                    AgwRb();
                    break;

                case PromptStatus.Keyword:

                    if (result.StringResult != keyFlip) return;
                    flipLastEntity();
                    AgwRb();
                    break;

                default:
                    break;
            }

        }

        private void flipLastEntity()
        {
            var Id = Autodesk.AutoCAD.Internal.Utils.EntLast();

            using (Active.Document.LockDocument())
            using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(Id, OpenMode.ForRead) as Spline;

                if (ent != null && ent.NumFitPoints == 7)
                {

                    Line3d MirrorLine3d = new Line3d(ent.StartPoint, ent.GetFitPointAt(4));

                    ent.UpgradeOpen();
                    ent.TransformBy(Matrix3d.Mirroring(MirrorLine3d));

                    tr.Commit();

                }
            }
        }

        private void DrawRodEnd(PromptEntityResult result)
        {
            Point3d pnt1;
            Point3d pnt2;
            using (Active.Document.LockDocument())
            using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
            {
                var pEntopt = new PromptEntityOptions(Environment.NewLine + "Select Second line:");
                pEntopt.SetRejectMessage(Environment.NewLine + "Only Lines allowed");
                pEntopt.AddAllowedClass(typeof(Line), false);

                var line1 = tr.GetObject(result.ObjectId, OpenMode.ForRead) as Line;
                pnt1 = line1.ClosestPoint(result.PickedPoint);

                pEntopt.Message = Environment.NewLine + "Select Second line:";
                var result2 = Active.Editor.GetEntity(pEntopt);

                if (result2.Status != PromptStatus.OK) return;
                var line2 = tr.GetObject(result2.ObjectId, OpenMode.ForRead) as Line;
                pnt2 = line2.ClosestPoint(result2.PickedPoint);

            }

            var midp = PointTools.MidBetween2Points(pnt1, pnt2);
            var RefVec = new Vector3d(0, 0, 1);
            var VectorXaxis = new Vector3d(1, 0, 0);
            var DirectionVec = pnt1.GetVectorTo(pnt2);

            var Angle = VectorXaxis.GetAngleTo(DirectionVec, RefVec) + Math.PI / 2;
            var dist = pnt1.DistanceTo(pnt2) / 8;

            var mid1a = PointTools.MidBetween2Points(pnt1, midp).PolarPoint(Angle, dist);

            var mid2 = PointTools.MidBetween2Points(pnt2, midp);
            var mid2a = mid2.PolarPoint(Angle + Math.PI, dist);
            var mid2b = mid2.PolarPoint(Angle, dist);

            var pntcol = new Point3dCollection();
            pntcol.Add(pnt1);
            pntcol.Add(mid1a);
            pntcol.Add(midp);
            pntcol.Add(mid2a);
            pntcol.Add(pnt2);
            pntcol.Add(mid2b);
            pntcol.Add(midp);

            var vect = new Point3d(0, 0, 0).GetAsVector();
            var spline = new Spline(pntcol, DirectionVec.GetPerpendicularVector(), vect, 4, 0);
            Active.Database.AddEntities(spline);
        }
    }
}
