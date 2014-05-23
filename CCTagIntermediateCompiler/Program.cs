using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMFParser;

namespace CCTagIntermediateCompiler
{
    class Program
    {
        VMF vmf;
        List<VBlock> entities;
        List<VBlock> instances;
        List<VBlock> flags;
        
        static void Main(string[] args)
        {
            string fileName = args.First();
            vmf = new VMF(File.ReadAllLines(fileName));
            if (TagModifications())
                File.WriteAllLines(fileName, vmf.ToVMFStrings());
        }

        internal static bool TagModifications()
        {
            bool hasChanged = false;

            entities = vmf.Body.Where(item => item.Name == "entity").Select(item => item as VBlock).ToList();
            instances = entities.Where(entity => entity.Body.Where(item => item.Name == "classname" && (item as VProperty).Value == "func_instance").Count() > 0).ToList();
            flags = instances.Where(instance => instance.Body.Where(item => item.Name == "targetname" && (item as VProperty).Value.StartsWith("CC_")).Count() > 0).ToList();

            hasChanged = Mod_EnablePaintInMap() | hasChanged;
            hasChanged = Mod_GreenFizzlerFlag() | hasChanged;
            return hasChanged;
        }

        internal static bool Mod_EnablePaintInMap()
        {
            bool hasChanged = false;

            VBlock world = vmf.Body.Where(item => item.Name == "world").First() as VBlock;
            VProperty paintInMap = world.Body.Where(item => item.Name == "paintinmap").First() as VProperty;
            if (paintInMap == null)
            {
                world.Body.Add(new VProperty("paintinmap", "1"));
                hasChanged = true;
            }
            else if (paintInMap.Value != "1")
            {
                paintInMap.Value = "1";
                hasChanged = true;
            }

            return hasChanged;
        }

        internal static bool Mod_GreenFizzlerFlag()
        {
            List<VBlock> greenFizzlerFlags = instances.Where(instance => instance.Body.Where(item => item.Name == "targetname" && (item as VProperty).Value.EndsWith("CC_GreenFizzlerFlag.vmf")).Count() > 0).ToList();
            foreach(var flag in greenFizzlerFlags)
            {
                //origin
                //-48 0 0
            //TODO: come back to this    
            string origin = "0 0 0";
            double[] originParts = origin.Split(' ').Select(s => double.Parse(s)).ToArray();
            List<string> origins = new List<string> { origin };
            for (int i = 0; i < 3; i++)
                for (int j = -48; j <= 48; j += 96)
                    origins.Add(string.Format("{0} {1} {2}", 
                        originParts[0] + (i == 0 ? j : 0),
                        originParts[1] + (i == 1 ? j : 0), 
                        originParts[2] + (i == 2 ? j : 0)));
                
            }
            
            return false;
        }
    }
}
