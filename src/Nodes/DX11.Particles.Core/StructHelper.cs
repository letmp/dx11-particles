#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace DX11.Particles.Core
{
    public class SlotBundle
    {
        public List<string> Definitions = new List<string>();
        protected int MaxSize = 4;
        public int Size = 0;

        public bool CanFit(int size)
        {
            return MaxSize - Size >= size;
        }

        public void DoFit(string definition, int size)
        {
            Size += size;
            Definitions.Add(definition);
        }

        public SlotBundle(string definition, int size)
        {
            this.DoFit(definition, size);
        }
    }

    public class Definition : IEquatable<Definition>
    {

        public int Size = 0;
        public string DefinitionString = "";
        public bool Valid;
        public string Type = "";
        public string Name = "";

        public Definition(string definition)
        {

            try
            {

                this.DefinitionString = definition;
                string[] typeAndName = definition.Split(null);
                Type = typeAndName[0];
                Name = typeAndName[1].Remove(typeAndName[1].LastIndexOf(';'), 1);

                this.Valid = isValid(Type);
                if (Valid) this.Size = getSize(Type);
            }
            catch (Exception)
            {
                Valid = false;
            }
        }

        public bool Equals(Definition other)
        {
            if (other == null) return false;
            return (other.Name == this.Name);
        }

        private Regex Matcher = new Regex("^(float|double|uint|int|bool)(\\d{0,1})(x(\\d{1})){0,1}$");

        private bool isValid(string type)
        {
            return Matcher.IsMatch(type);
        }

        private int getSize(string type)
        {
            string pattern = "\\d";
            MatchCollection mc = Regex.Matches(type, pattern);
            int size = 1;
            foreach (Match m in mc)
            {
                int value = Convert.ToInt32(m.ToString());
                if (size == 0) size = value;
                else size *= value;
            }
            return size;
        }
    }

    public class StructHelper
    {
        protected List<SlotBundle> Slots = new List<SlotBundle>();
        protected List<Definition> Definitions = new List<Definition>();
        
        public List<string> StructureDefinition = new List<string>();
        public int Stride = 0;

        public StructHelper(List<string> FDefinition)
        {
           
            Slots.Clear();
            Definitions.Clear();

            foreach (string str in FDefinition)
            {
                Definition nd = new Definition(str);

                if (!Definitions.Contains(nd))
                    Definitions.Add(nd);
            }
            
            Definitions.OrderByDescending(d => d.Size);

            int stride = 0;
            foreach (Definition d in Definitions.Where(d => d.Valid))
            {

                var goodFit = (
                                    from slot in Slots.OrderByDescending(s => s.Size)
                                    where slot.CanFit(d.Size)
                                    select slot
                                );

                var ns = goodFit.FirstOrDefault();
                if (ns == null)
                {
                    ns = new SlotBundle(d.DefinitionString, d.Size);
                    Slots.Add(ns);
                }
                else ns.DoFit(d.DefinitionString, d.Size);

                stride += d.Size;
            }

            foreach (SlotBundle sb in Slots)
            {
                foreach (string str in sb.Definitions)
                {
                    StructureDefinition.Add(str);
                }
                
            }

            Stride= stride * 4;

        }

    }
}
