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
        static void Main(string[] args)
        {
            string fileName = args.First();
            VMF vmf = new VMF(File.ReadAllLines(fileName));
            if (TagModifications(vmf))
                File.WriteAllLines(fileName, vmf.ToVMFStrings());
        }

        internal static bool TagModifications(VMF vmf)
        {
            bool hasChanged = false;

            List<VBlock> entities = vmf.Body.Where(item => item.Name == "entity").Select(item => item as VBlock).ToList();
            List<VBlock> instances = entities.Where(entity => entity.Body.Where(item => item.Name == "classname" && (item as VProperty).Value == "func_instance").Count() > 0).ToList();

            hasChanged = Mod_EnablePaintInMap(vmf) | hasChanged;
            hasChanged = Mod_GreenFizzlerFlag(instances) | hasChanged;
            return hasChanged;
        }

        internal static bool Mod_EnablePaintInMap(VMF vmf)
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

        internal static bool Mod_GreenFizzlerFlag(List<VBlock> instances)
        {
            List<VBlock> flags = instances.Where(instance => instance.Body.Where(item => item.Name == "file" && (item as VProperty).Value.EndsWith("CC_GreenFizzlerFlag.vmf")).Count() > 0).ToList();


            return false;
        }
    }
}
