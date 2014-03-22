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
            string fileName = args[0];
            VMF vmf = new VMF(File.ReadAllLines(fileName));
            if (TagModifications(vmf))
                File.WriteAllLines(fileName, vmf.ToVMFStrings());
        }

        internal static bool TagModifications(VMF vmf)
        {
            bool hasChanged = false;

            return hasChanged;
        }

        internal static bool Mod_GreenFizzlers(VMF vmf)
        {

            return false;
        }
    }
}
