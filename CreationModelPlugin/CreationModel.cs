﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            //var res1 = new FilteredElementCollector(doc)
            //    .OfClass(typeof(Wall))
            //    //.Cast<Wall>() может дать исключение, так как типы лишние приходят
            //    .OfType<Wall>()
            //    .ToList();            
            //var res2 = new FilteredElementCollector(doc)
            //    .OfClass(typeof(WallType))                
            //    .OfType<WallType>()
            //    .ToList();
            //var res3 = new FilteredElementCollector(doc)
            //    .OfClass(typeof(FamilyInstance))
            //    .OfCategory(BuiltInCategory.OST_Doors)
            //    .OfType<FamilyInstance>()
            //    .Where(x => x.Name.Equals("1010х2100(h)"))
            //    .ToList();
            //var res4 = new FilteredElementCollector(doc)
            //   .WhereElementIsNotElementType()
            //   .ToList();

            Level level_1;
            Level level_2;
            TakeLevels(doc, out level_1, out level_2);
            CreateWalls(doc, level_1, level_2);
            return Result.Succeeded;
        }


        private static void CreateWalls(Document doc, Level level_1, Level level_2)
        {
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level_1.Id, true);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level_2.Id);
            }
            AddDoor(doc, level_1, walls[0]);
            AddWindow(doc, level_1, walls[1]);
            AddWindow(doc, level_1, walls[2]);
            AddWindow(doc, level_1, walls[3]);
            transaction.Commit();
        }

        private static void AddDoor(Document doc, Level level_1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point_1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point_2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point_1 + point_2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();
            doc.Create.NewFamilyInstance(point, doorType, wall, level_1, StructuralType.NonStructural);
        }

        private static void AddWindow(Document doc, Level level_1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1830 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point_1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point_2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point_1 + point_2) / 2;

            if (!windowType.IsActive)
                windowType.Activate();
            var window = doc.Create.NewFamilyInstance(point, windowType, wall, level_1, StructuralType.NonStructural);
            Parameter sillHeight = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
            double sh = UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters);
            sillHeight.Set(sh);
        }

        private static void TakeLevels(Document doc, out Level level_1, out Level level_2)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            level_1 = listLevel
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();

            level_2 = listLevel
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();
        }




    }
}
