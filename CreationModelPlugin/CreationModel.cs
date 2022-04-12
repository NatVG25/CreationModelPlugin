using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]

    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            List<Level> listLevel = GetLevels(doc);

            Transaction transaction1 = new Transaction(doc, "Построение стен");

            transaction1.Start();

            List<Wall> walls = WallsCreate(doc, listLevel);

            transaction1.Commit();
           
            Transaction transaction2 = new Transaction(doc, "Добавление окон и двери");

            transaction2.Start();

            AddDoor(doc, walls[0]); 
            
            AddWindows(doc, walls);

            AddRoof(doc, walls);

            transaction2.Commit();
           
            return Result.Succeeded;
        }

        private void AddRoof(Document doc, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc) //находим нужный тип крыши
                    .OfClass(typeof(RoofType))
                    .OfType<RoofType>()
                    .Where(x => x.Name.Equals("Типовой - 400мм")) //фильтруя по имени (названию типа)
                    .Where(x => x.FamilyName.Equals("Базовая крыша")) //и по названию семейства
                    .FirstOrDefault();

            Level level2 = GetLevels(doc) //из созданного списка находим Уровень 1
                              .Where(x => x.Name.Equals("Уровень 2"))
                              .FirstOrDefault();

            double wallWidth = walls[0].Width;
            double dt = wallWidth / 2;

            double dx = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters)/ 2;
            double dy = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters) / 2;
            double dz = UnitUtils.ConvertToInternalUnits(4000, UnitTypeId.Millimeters);
            double heigthRoof = UnitUtils.ConvertToInternalUnits(2000, UnitTypeId.Millimeters);
            
            double dX = dx + dt;
            double dY = dy + dt;
            double dZ = dz + heigthRoof;

            CurveArray curveArray = new CurveArray();
          
            curveArray.Append(Line.CreateBound(new XYZ(0, -dY, dz), new XYZ(0, 0, dZ)));
            curveArray.Append(Line.CreateBound(new XYZ(0, 0, dZ), new XYZ(0, dY, dz)));

            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, dZ), new XYZ(0, dY, 0), doc.ActiveView);
            doc.Create.NewExtrusionRoof(curveArray, plane, level2, roofType, -dX, dX);
            
            //double wallWidth = walls[0].Width;
            //double dt = wallWidth/2;

            //List<XYZ> points = new List<XYZ>();
            //points.Add(new XYZ(-dt, -dt, 0));
            //points.Add(new XYZ(dt, -dt, 0));
            //points.Add(new XYZ(dt, dt, 0));
            //points.Add(new XYZ(-dt, dt, 0));
            //points.Add(new XYZ(-dt, -dt, 0));

            //Application application = doc.Application;
            //CurveArray footprint = application.Create.NewCurveArray(); //находим отпечаток границы дома, по которому будет построена крыша, пока он пустой
            //for (int i = 0; i < 4; i++) //переберем все стены
            //{
            //    LocationCurve curve = walls[i].Location as LocationCurve; //у текущей стены возьмем свойство Location, получим кривую
            //    /*  footprint.Append(curve.Curve);*/ //добавляем кривую в отпечаток при помощи метода Append (из LocationCurve можно получить саму Curve через свойство Curve)
            //    XYZ p1 = curve.Curve.GetEndPoint(0);
            //    XYZ p2 = curve.Curve.GetEndPoint(1);
            //    Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
            //    footprint.Append(line);
            //}
            //ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
            //FootPrintRoof footPrintRoof = doc.Create.NewFootPrintRoof(footprint, level2, roofType, out footPrintToModelCurveMapping);

            ////ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
            ////iterator.Reset();
            ////while (iterator.MoveNext())
            ////{
            ////    ModelCurve modelCurve = iterator.Current as ModelCurve;
            ////    footPrintRoof.set_DefinesSlope(modelCurve, true);
            ////    footPrintRoof.set_SlopeAngle(modelCurve, 0.5);
            ////}
            //foreach(ModelCurve m in footPrintToModelCurveMapping)
            //{
            //    footPrintRoof.set_DefinesSlope(m, true);
            //    footPrintRoof.set_SlopeAngle(m, 0.5);
            //}

        }

        private void AddWindows(Document doc, List<Wall> walls)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                       .OfClass(typeof(FamilySymbol))
                       .OfCategory(BuiltInCategory.OST_Windows)
                       .OfType<FamilySymbol>()
                       .Where(x => x.Name.Equals("0915 x 1830 мм"))
                       .Where(x => x.FamilyName.Equals("Фиксированные"))
                       .FirstOrDefault();

            for (int i = 1; i < 4; i++)
            {
                Wall wall = walls[i];
               
                LocationCurve hostCurve = wall.Location as LocationCurve;
                XYZ point1 = hostCurve.Curve.GetEndPoint(0);
                XYZ point2 = hostCurve.Curve.GetEndPoint(1);
                XYZ pointMid = (point1 + point2) / 2;
                
                XYZ wallCenter = GetElementCenter(wall);
                XYZ point = (pointMid + wallCenter)/2;


                Level level1 = GetLevels(doc) //из созданного списка находим Уровень 1
                              .Where(x => x.Name.Equals("Уровень 1"))
                              .FirstOrDefault();

                if (!windowType.IsActive)
                    windowType.Activate();

                doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);

            }

        }
        public XYZ GetElementCenter(Element element) //метод для нахождения центра элемента с использованием метода get_BoundingBox
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }


        public static List<Level> GetLevels(Document doc)
        {
            List<Level> listLevel = new FilteredElementCollector(doc) //создаем список уровней в проекте
                                        .OfClass(typeof(Level))
                                        .OfType<Level>()
                                        .ToList();
            return listLevel;
        }

        public static List<Wall> WallsCreate(Document doc, List<Level> listLevel)
        {
            Level level1 = listLevel //из созданного списка находим Уровень 1
             .Where(x => x.Name.Equals("Уровень 1"))
             .FirstOrDefault();

            Level level2 = listLevel //из созданного списка находим Уровень 2
              .Where(x => x.Name.Equals("Уровень 2"))
              .FirstOrDefault();

            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters); //ширина, преобразованная в миллиметры
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters); //глубина, преобразованная в миллиметры
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            //создадим транзакцию для создания стен
            //Transaction transaction = new Transaction(doc, "Построение стен");
            //transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]); //создаем линию методом CreateBound на классе Line
                Wall wall = Wall.Create(doc, line, level1.Id, false); //создаем стену с помощью метода Create
                walls.Add(wall); //добавляем созданную стену в список
                //далее обращаемся к параметру высоты стены WALL_HEIGHT_TYPE и устанавливаем для него значение level2
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }

            //transaction.Commit();


            return walls;
        }


        private void AddDoor(Document doc,/* Level level1,*/ Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilySymbol))
                        .OfCategory(BuiltInCategory.OST_Doors)
                        .OfType<FamilySymbol>()
                        .Where(x => x.Name.Equals("0915 x 2134 мм"))
                        .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                        .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            Level level1 = GetLevels(doc) //из созданного списка находим Уровень 1
                          .Where(x => x.Name.Equals("Уровень 1"))
                          .FirstOrDefault();

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);

        }
    }
}
