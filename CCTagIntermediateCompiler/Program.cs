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
        static private VMF vmf;
        static private List<VBlock> entities;
        static private List<VBlock> instances;
        static private List<VBlock> flags;

        static void Main(string[] args)
        {
#if DEBUG
            args = new string[] { "preview.vmf" }; //REMOVE TEST DATA WHEN DEPLOYING :P
#endif
            try
            {
                string fileName = args.FirstOrDefault();
                vmf = new VMF(File.ReadAllLines(fileName));
                if (TagModifications())
                    File.WriteAllLines(fileName, vmf.ToVMFStrings());
            }
            catch (Exception ex)
            {
                File.WriteAllText("errors.txt", ex.ToString());
            }
        }

        internal static bool TagModifications()
        {
            bool hasChanged = false;

            entities = vmf.Body.Where(item => item.Name == "entity").Select(item => item as VBlock).ToList();
            instances = entities.Where(entity => entity.Body.Where(item => item.Name == "classname" && (item as VProperty).Value == "func_instance").Count() > 0).ToList();
            flags = instances.Where(instance => instance.Body.Where(item => item.Name == "targetname" && (item as VProperty).Value.StartsWith("CC_")).Count() > 0).ToList();

            hasChanged = Mod_EnablePaintInMap()   || hasChanged;
            hasChanged = Mod_COOPChanges()        || hasChanged;
            hasChanged = Mod_BlueFizzlerChanges() || hasChanged;
            //hasChanged = Mod_GreenFizzlerFlag() || hasChanged;

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

        internal static bool Mod_COOPChanges()
        {
            bool hasChanged = false;

            //If is coop, add point entity where the elevator instance is.
            var coop_exit = instances.Where(instance =>
                instance.Body.Where(property =>
                    property.Name == "file" &&
                    property.GetType() == typeof(VProperty) &&
                    ((VProperty)property).Value.EndsWith("coop_exit.vmf"))
                    .Count() == 1).FirstOrDefault();
            if (coop_exit != null)
            {
                //Then this must be coop
                var coop_exit_origin = coop_exit.Body.Where(property => property.Name == "origin" && property.GetType() == typeof(VProperty)).FirstOrDefault() as VProperty;
                if (coop_exit_origin == null)
                {
                    Console.WriteLine("We have a coop exit, with no origin?");
                    return false;
                }

                #region Add our special entity!!!

                var editorVMF = new string[]{
                    "editor",
                    "{",
                    "\"color\" \"220 30 220\"",
                    "\"visgroupshown\" \"1\"",
                    "\"visgroupautoshown\" \"1\"",
                    "\"logicalpos\" \"[0 0]\"",
                    "}"
                };

                var entity = new VBlock("entity", new List<IVNode>()
                {
                    new VProperty("id", vmf.GetUniqueID().ToString()),
                    new VProperty("classname", "info_target"),
                    new VProperty("angles", "0 0 0"),
                    new VProperty("targetname", "supress_blue_portalgun_spawn"),
                    new VProperty("origin", coop_exit_origin.Value),
                    new VBlock(new string[]{
                        "editor",
                        "{",
                        "\"color\" \"220 30 220\"",
                        "\"visgroupshown\" \"1\"",
                        "\"visgroupautoshown\" \"1\"",
                        "\"logicalpos\" \"[0 0]\"",
                        "}"
                    })
                });
                vmf.Body.Add(entity);
                entities.Add(entity);

                #endregion

                #region Swap all singleplayer instances for coop instances.
                foreach (var instance in instances)
                {
                    var file = instance.Body.FirstOrDefault(property => property.GetType() == typeof(VProperty) && property.Name == "file") as VProperty;
                    if (file.Value.EndsWith("_sp.vmf"))
                        file.Value = file.Value.Replace("_sp.vmf", "_coop.vmf");
                }
                #endregion

                hasChanged = true;
            }

            return hasChanged;
        }

        internal static bool Mod_BlueFizzlerChanges()
        {
            bool hasChanged = false;

            List<VBlock> fizzlers = entities.Where(entity => entity.GetType() == typeof(VBlock) &&
                                                             //entity.Body.FirstOrDefault(property => property.GetType() == typeof(VProperty) &&
                                                             //                                       property.Name == "targetname" &&
                                                             //                                       (property as VProperty).Value.StartsWith("barrierhazard") &&
                                                             //                                       (property as VProperty).Value.EndsWith("_brush")
                                                             //                                       ) != null &&
                                                             entity.Body.FirstOrDefault(property => property.GetType() == typeof(VProperty) &&
                                                                                                    property.Name == "classname" &&
                                                                                                    (property as VProperty).Value == "trigger_portal_cleanser"
                                                                                                    ) != null
                                                             ).Select(entity => entity as VBlock).ToList();
            foreach(var fizzler in fizzlers)
            {
                VBlock copy = fizzler.DeepClone();
                (copy.Body.First(property => property.Name == "id") as VProperty).Value = vmf.GetUniqueID().ToString(); //TODO: Add method for generating new ids automatically to the VMF class.
                (copy.Body.First(property => property.Name == "classname") as VProperty).Value = "trigger_paint_cleanser";
                (copy.Body.First(property => property.Name == "spawnflags") as VProperty).Value = "64";

                vmf.Body.Add(copy);

                //I wonder if it will be a problem that the brushes and sides don't have unique IDs... lets change them to be sure.
                foreach (VBlock solid in copy.Body.Where(property => property.GetType() == typeof(VBlock) && property.Name == "solid").Select(property => property as VBlock))
                {
                    (solid.Body.First(property => property.Name == "id") as VProperty).Value = vmf.GetUniqueID().ToString();
                    foreach (VBlock side in solid.Body.Where(property => property.GetType() == typeof(VBlock) && property.Name == "side").Select(property => property as VBlock))
                    {
                        (side.Body.First(property => property.Name == "id") as VProperty).Value = vmf.GetUniqueID().ToString();
                        (side.Body.First(property => property.Name == "material") as VProperty).Value = "tools/toolstrigger";
                    }
                }
            }
            return hasChanged;
        }

        internal static bool Mod_GreenFizzlerFlag()
        {
            List<VBlock> greenFizzlerFlags = instances.Where(instance => instance.Body.Where(item => item.Name == "targetname" && (item as VProperty).Value.EndsWith("CC_GreenFizzlerFlag.vmf")).Count() > 0).ToList();
            foreach (var flag in greenFizzlerFlags)
            {
                string origin = "0 0 0";
                double[] originParts = origin.Split(' ').Select(s => double.Parse(s)).ToArray();
                List<string> origins = new List<string> { origin };
                for (int i = 0; i < 3; i++)
                    for (int j = -48; j <= 48; j += 96)
                        origins.Add(string.Format("{0} {1} {2}",
                            originParts[0] + (i == 0 ? j : 0),
                            originParts[1] + (i == 1 ? j : 0),
                            originParts[2] + (i == 2 ? j : 0)));

                List<VBlock> fizzlersEmittersToChange = entities.Where(entity =>
                    entity.Body.FirstOrDefault(property =>
                        property.Name == "origin" &&
                        property.GetType() == typeof(VProperty) &&
                        origins.Contains((property as VProperty).Value)
                        ) != null &&
                        entity.Body.FirstOrDefault(property =>
                            property.Name.StartsWith("replace") &&
                            property.GetType() == typeof(VProperty) &&
                            (property as VProperty).Value.StartsWith("$connectioncount")) == null)
                .ToList();

                foreach (var fizzlerEmitter in fizzlersEmittersToChange)
                {
                    //Determine if fizzler or laser
                    bool isLaser = fizzlerEmitter.Body.Where(property => property.Name.StartsWith("replace") && property.GetType() == typeof(VProperty) && (property as VProperty).Value == "$skin 2").Count() == 0;

                    if (isLaser)
                    {
                        //Convert to fizzler
                        //or throw up an error, forget trying to deal with this
                    }

                    //Determine direction flag is facing, and which way that correlates to the fizzler
                    //Either throw error when direction is wrong, or perhaps instead just break out, because it could be meant for another fizzler.... just don't process this

                    //find all parts of this item
                    string targetname = ((VProperty)fizzlerEmitter.Body.FirstOrDefault(property => property.Name == "targetname")).Value;
                    string name = targetname.Substring(targetname.Length - 4);

                    VBlock fizzlerBrush = entities.Where(entity => entity.GetType() == typeof(VBlock) &&
                        ((VBlock)entity).Body.Where(property => property.Name == "targetname" &&
                            property.GetType() == typeof(VProperty) &&
                            ((VProperty)property).Value.EndsWith(name + "_brush")).Count() == 1
                        ).FirstOrDefault();

                    //Change emitter instance (if desired)

                    //change fizzler texures
                    //effects/fizzler_gelgun


                    //copy and make trigger


                    //copy and move, and make trigger / twice, needs to know direction for this

                    //find center and place particle emitter based on direction

                }
            }

            return false;
        }
    }
}
