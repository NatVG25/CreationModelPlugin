using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    public class WallsUtils
    {
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
            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]); //создаем линию методом CreateBound на классе Line
                Wall wall = Wall.Create(doc, line, level1.Id, false); //создаем стену с помощью метода Create
                walls.Add(wall); //добавляем созданную стену в список
                //далее обращаемся к параметру высоты стены WALL_HEIGHT_TYPE и устанавливаем для него значение level2
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }

            transaction.Commit();


            return walls;
        }
    }
}
