using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    public class LevelUtils
    {
        public static List<Level> GetLevels(Document doc)
        {
            List<Level> listLevel = new FilteredElementCollector(doc) //создаем список уровней в проекте
                                        .OfClass(typeof(Level))
                                        .OfType<Level>()
                                        .ToList();
            return listLevel;
        }
               
    }
}
