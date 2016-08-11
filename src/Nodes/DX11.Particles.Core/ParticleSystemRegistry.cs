using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VVVV.PluginInterfaces.V2;

namespace DX11.Particles.Core
{
    public delegate void RegistryChangedHandler(ParticleSystemRegistry sender, ParticleSystemRegistryEventArgs e);

    public class ParticleSystemRegistry : Dictionary<string, ParticleSystemData>
    {
        public const string PARTICLESYSTEM_ENUM = "DX11.Particles.Core.ParticleSystemNames";
        public const string DEFAULT_ENUM = "Default System";

        public const string EMITTER_ENUM = "DX11.Particles.Core.EmitterNames";
        public const string BUFFER_ENUM = "DX11.Particles.Core.BufferNames";
        
        private static ParticleSystemRegistry _instance;
        public event RegistryChangedHandler Changed;

        public static ParticleSystemRegistry Instance
        {
            get { return _instance ?? (_instance = new ParticleSystemRegistry()); }
        }

        public ParticleSystemRegistry() : base()
        {
            Add(DEFAULT_ENUM, new ParticleSystemData("DefaultId"));
            UpdateParticleSystemEnum();
        }

        public void Add(string particleSystemName, string particleSystemRegisterNodeId)
        {

            if (!Instance.ContainsKey(particleSystemName)){
                Instance[particleSystemName] = new ParticleSystemData(particleSystemRegisterNodeId);
            }
            else
            {
                Instance[particleSystemName].AddNodeId(particleSystemRegisterNodeId);
            }

            UpdateParticleSystemEnum();

            if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(false,false));
        }

        public void Remove(ParticleSystemData particleSystemData)
        {
            var item = Instance.First(kvp => kvp.Value == particleSystemData);
            Instance.Remove(item.Key);

            UpdateParticleSystemEnum();

            if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(false, false));
        }

        public void SetShaderVariables (string particleSystemName, string shaderRegisterNodeId, IEnumerable<string> variables)
        {
            if (Instance.ContainsKey(particleSystemName))
            {
                Instance[particleSystemName].SetShaderVariables(shaderRegisterNodeId, variables);
                if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true, false));
            }
        }

        public void RemoveShaderVariables (string shaderRegisterNodeId)
        {
            try
            {
                ParticleSystemData psd = Instance.First(kvp => kvp.Value.HasShaderNodeId(shaderRegisterNodeId)).Value;
                if (psd != null)
                {
                    psd.RemoveShaderVariables(shaderRegisterNodeId);
                    if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true, false));
                }
            }
            catch (Exception) { }
        }

        public void SetDefines(string particleSystemName, string shaderRegisterNodeId, IEnumerable<string> additionalDefines)
        {
            if (Instance.ContainsKey(particleSystemName))
            {
                Instance[particleSystemName].SetDefines(shaderRegisterNodeId, additionalDefines);
                if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true, false));
            }
        }

        public void RemoveDefines(string shaderRegisterNodeId)
        {
            try
            {
                ParticleSystemData psd = Instance.First(kvp => kvp.Value.HasShaderNodeId(shaderRegisterNodeId)).Value;
                if (psd != null)
                {
                    psd.RemoveDefines(shaderRegisterNodeId);
                    if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true, false));
                }
            }
            catch (Exception){}
        }

        public void SetEmitterSize(string particleSystemName, string shaderRegisterNodeId, int emitterSize)
        {
            if (Instance.ContainsKey(particleSystemName))
            {
                Instance[particleSystemName].SetEmitterSize(shaderRegisterNodeId, emitterSize);
                if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true, false));
            }
        }

        public void RemoveEmitterSize(string shaderRegisterNodeId)
        {
            try
            {
                ParticleSystemData psd = Instance.First(kvp => kvp.Value.HasShaderNodeId(shaderRegisterNodeId)).Value;
                if (psd != null)
                {
                    psd.RemoveEmitterSize(shaderRegisterNodeId);
                    if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true, false));
                }
            }
            catch (Exception) { }
        }

        public void SetEmitterName(string particleSystemName, string shaderRegisterNodeId, string emitterName)
        {
            if (Instance.ContainsKey(particleSystemName))
            {
                Instance[particleSystemName].SetEmitterName(shaderRegisterNodeId, emitterName);
                if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true, false));
            }
        }

        public void RemoveEmitterName(string shaderRegisterNodeId)
        {
            try
            {
                ParticleSystemData psd = Instance.First(kvp => kvp.Value.HasShaderNodeId(shaderRegisterNodeId)).Value;
                if (psd != null)
                {
                    psd.RemoveEmitterName(shaderRegisterNodeId);
                    if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(true, false));
                }
            }
            catch (Exception) { }
        }

        public void SetBufferSettings(string particleSystemName, string bufferRegisterNodeId, List<ParticleSystemBufferSettings> bufferSettings)
        {
            if (Instance.ContainsKey(particleSystemName))
            {
                Instance[particleSystemName].SetBufferSettings(bufferRegisterNodeId, bufferSettings);
                if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(false, true));
            }
        }

        public void RemoveBufferSettings(string bufferRegisterNodeId)
        {
            try
            {
                ParticleSystemData psd = Instance.First(kvp => kvp.Value.HasBufferRegisterNodeId(bufferRegisterNodeId)).Value;
                if (psd != null)
                {
                    psd.RemoveBufferSettings(bufferRegisterNodeId);
                    if (Changed != null) Changed(this, new ParticleSystemRegistryEventArgs(false, true));
                }
            }
            catch (Exception) { }
        }

        public ParticleSystemData GetByParticleSystemId (string particleSystemRegisterNodeId)
        {
            foreach (ParticleSystemData psd in Instance.Values)
            {
                if (psd.HasNodeId(particleSystemRegisterNodeId)) return psd;
            }
            return null;
        }

        public ParticleSystemData GetByParticleSystemName(string particleSystemName)
        {
            foreach (var kvp in Instance)
            {
                if (kvp.Key == particleSystemName) return kvp.Value;
            }
            return null;
        }

        public ParticleSystemData GetByShaderId (string shaderRegisterNodeId)
        {
            foreach (ParticleSystemData psd in Instance.Values)
            {
                if (psd.HasShaderNodeId(shaderRegisterNodeId)) return psd;
            }
            return null;
        }

        public string GetParticleSystemName (ParticleSystemData particleSystemData)
        {
            var item = Instance.First(kvp => kvp.Value == particleSystemData);
            return item.Key;
        }

        private void UpdateParticleSystemEnum()
        {
            EnumManager.UpdateEnum(ParticleSystemRegistry.PARTICLESYSTEM_ENUM, "", this.Keys.ToArray());
        }
        
    }

    public class ParticleSystemData
    {
        public List<string> ParticleSystemRegisterNodeIds = new List<string>();
        
        // Settings set by shader registry nodes
        public Dictionary<string, List<string>> ShaderVariables = new Dictionary<string,List<string>>();
        public Dictionary<string, List<string>> Defines = new Dictionary<string, List<string>>();
        public Dictionary<string, int> EmitterSizes = new Dictionary<string, int>();
        public Dictionary<string, string> EmitterNames = new Dictionary<string, string>();
        
        // Settings set by buffer registry nodes
        public Dictionary<string, List<ParticleSystemBufferSettings>> BufferSettings = new Dictionary<string, List<ParticleSystemBufferSettings>>();
        
        // calculated settings
        public string StructureDefinition { get; protected set; }
        public int Stride { get; protected set; }
        public int ElementCount { get; protected set; }

        public ParticleSystemData(string particleSystemRegisterNodeId)
        {
            this.ParticleSystemRegisterNodeIds.Add(particleSystemRegisterNodeId);
            this.StructureDefinition = "";
            this.Stride = 0;
            this.ElementCount = 0;
        }

        public void AddNodeId (string particleSystemRegisterNodeId ){
            this.ParticleSystemRegisterNodeIds.Add(particleSystemRegisterNodeId);
        }

        public void RemoveNodeId(string particleSystemRegisterNodeId){
            this.ParticleSystemRegisterNodeIds.Remove(particleSystemRegisterNodeId);
        }

        public bool HasNodeId(string particleSystemRegisterNodeId)
        {
            return this.ParticleSystemRegisterNodeIds.Contains(particleSystemRegisterNodeId);
        }

        public bool IsEmpty()
        {
            return this.ParticleSystemRegisterNodeIds.Count == 0;
        }

        public void SetShaderVariables (string shaderRegisterNodeId, IEnumerable<string> variables){
            this.ShaderVariables[shaderRegisterNodeId] = variables.ToList();
            this.UpdateStructure();
        }

        public void RemoveShaderVariables (string shaderRegisterNodeId){
            this.ShaderVariables.Remove(shaderRegisterNodeId);
            this.UpdateStructure();
        }

        public void SetDefines(string shaderRegisterNodeId, IEnumerable<string> additionalDefines)
        {
            this.Defines[shaderRegisterNodeId] = additionalDefines.ToList();
        }

        public void RemoveDefines(string shaderRegisterNodeId)
        {
            this.Defines.Remove(shaderRegisterNodeId);
        }

        public List<string> GetDefines()
        {
            List<string> defines = (
                               from definelist in Defines.Values
                               from define in definelist
                               select define
                           ).Distinct().ToList();
            return defines;
        }

        public List<string> GetDefines(string shaderRegisterNodeId)
        {
            if (this.Defines.ContainsKey(shaderRegisterNodeId))
                return this.Defines[shaderRegisterNodeId];
            else return new List<string>();
        }

        public void SetEmitterSize(string shaderRegisterNodeId, int size)
        {
            this.EmitterSizes[shaderRegisterNodeId] = size;
            UpdateElementCount();
        }

        public void RemoveEmitterSize(string shaderRegisterNodeId)
        {
            if (this.EmitterSizes.ContainsKey(shaderRegisterNodeId))
            {
                this.EmitterSizes.Remove(shaderRegisterNodeId);
                UpdateElementCount();
            }
        }

        public void SetEmitterName(string shaderRegisterNodeId, string name)
        {
            this.EmitterNames[shaderRegisterNodeId] = name;
            UpdateEmitterEnum();
        }

        public void RemoveEmitterName(string shaderRegisterNodeId)
        {
            if (this.EmitterNames.ContainsKey(shaderRegisterNodeId))
            {
                this.EmitterNames.Remove(shaderRegisterNodeId);
                UpdateEmitterEnum();
            }
        }

        public void SetBufferSettings(string bufferRegisterNodeId, List<ParticleSystemBufferSettings> bufferSettings)
        {
            this.BufferSettings[bufferRegisterNodeId] = bufferSettings;
        }

        public void RemoveBufferSettings(string bufferRegisterNodeId)
        {
            this.BufferSettings.Remove(bufferRegisterNodeId);
        }

        public List<ParticleSystemBufferSettings> GetBufferSettings()
        {
            List<ParticleSystemBufferSettings> buffsettings = (
                               from buffsettingslist in BufferSettings.Values
                               from buffersetting in buffsettingslist
                               select buffersetting
                           ).ToList();
            return buffsettings;
        }

        public List<ParticleSystemBufferSettings> GetBufferSettings(string bufferRegisterNodeId)
        {
            if (this.BufferSettings.ContainsKey(bufferRegisterNodeId))
                return this.BufferSettings[bufferRegisterNodeId];
            else return new List<ParticleSystemBufferSettings>();
        }

        public bool HasBufferRegisterNodeId(string bufferRegisterNodeId)
        {
            return this.BufferSettings.ContainsKey(bufferRegisterNodeId);
        }


        public string GetShaderRegisterNodeId(string emitterName)
        {
            foreach (var kvp in EmitterNames)
            {
                if (kvp.Value == emitterName) return kvp.Key;
            }
            return null;
        }

        public bool HasShaderNodeId(string shaderRegisterNodeId)
        {
            return this.ShaderVariables.ContainsKey(shaderRegisterNodeId);
        }

        private void UpdateStructure(){
            var variables = (
                                from shaderVariables in ShaderVariables.Values
                                    from shaderVariable in shaderVariables
                                    select shaderVariable
                            ).Distinct().ToList();

            StructHelper sh = new StructHelper(variables);

            this.StructureDefinition = string.Join(" ", sh.StructureDefinition);
            this.Stride = sh.Stride;
        }

        public List<int> GetEmitterRegion(string shaderRegisterNodeId)
        {
            List<int> fromTo = new List<int>();

            fromTo.Add(0);
            fromTo.Add(0);

            foreach (var kvp in EmitterSizes)
            {
                if (kvp.Key == shaderRegisterNodeId)
                {
                    fromTo[1] += kvp.Value;
                    return fromTo;
                }
                else
                {
                    fromTo[0] += kvp.Value;
                    fromTo[1] += kvp.Value;
                }
            }

            return fromTo;
        }

        public int GetEmitterOffset(string shaderRegisterNodeId)
        {
            int offset = 0;

            foreach (var kvp in EmitterSizes)
            {
                if (kvp.Key == shaderRegisterNodeId) return offset;
                else offset += kvp.Value;
            }

            return offset;
        }

        private void UpdateElementCount()
        {
            int count = 0;
            foreach (int size in EmitterSizes.Values)
            {
                count += size;
            }
            this.ElementCount = count;
        }

        private void UpdateEmitterEnum()
        {
            EnumManager.UpdateEnum(ParticleSystemRegistry.EMITTER_ENUM, "", this.EmitterNames.Values.Distinct().ToArray());
        }
                
    }

    public class ParticleSystemRegistryEventArgs : EventArgs
    {
        public ParticleSystemRegistryEventArgs(bool isShaderRegistryEvent,bool isBufferRegistryEvent)
        {
            this.isShaderRegistryEvent = isShaderRegistryEvent;
            this.isBufferRegistryEvent = isBufferRegistryEvent;
        }
        
        private bool isShaderRegistryEvent;
        public bool IsShaderRegistryEvent
        {
            get { return isShaderRegistryEvent; }
        }

        private bool isBufferRegistryEvent;
        public bool IsBufferRegistryEvent
        {
            get { return isBufferRegistryEvent; }
        }
    }

    public class ParticleSystemBufferSettings
    {
        public string bufferSemantic;
        public int elementCount;
        public int stride;
        public int bufferMode;
        public bool resetCounter;

        public ParticleSystemBufferSettings(string bufferSemantic, int elementCount, int stride, int bufferMode, bool resetCounter){
            this.bufferSemantic = bufferSemantic;
            this.elementCount = elementCount;
            this.stride = stride;
            this.bufferMode = bufferMode;
            this.resetCounter = resetCounter;
        }
    }

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

            Stride = stride * 4;

        }

    }

}
