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
            hasChanged = Mod_GreenFizzlerFlag(vmf) | hasChanged;
            hasChanged = Mod_EnablePaintInMap(vmf) | hasChanged;
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

        internal static bool Mod_GreenFizzlerFlag(VMF vmf)
        {

            return false;
        }
    }
}
