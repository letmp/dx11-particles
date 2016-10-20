using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils;

namespace DX11.Particles.Core
{

    #region PluginInfo
    [PluginInfo(Name = "Register", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "ParticleSystem", Help = "Registers a particle system and outputs the variables of shaders that are registered to this particle system. It also calculates the stride. ", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class ParticleSystemNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {
        [Input("ParticleSystem Name", DefaultString = ParticleSystemRegistry.DEFAULT_ENUM, IsSingle = true)]
        public IDiffSpread<string> FParticleSystemName;

        [Import]
        protected IPluginHost2 PluginHost;

        private string ParticleSystemRegisterNodeId = "";
        //private bool firstEval = true;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            this.ParticleSystemRegisterNodeId = PluginHost.GetNodePath(false);
        }

        public void Evaluate(int SpreadMax)
        {
            /*if (firstEval)
            {
                AddParticleSystem();
                firstEval = false;
            }*/

            if (FParticleSystemName.IsChanged)
            {
                AddParticleSystem();
            }

        }

        public void Dispose()
        {
            RemoveParticleSystem();
        }

        private void AddParticleSystem()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            ParticleSystemData psd = particleSystemRegistry.GetByParticleSystemId(this.ParticleSystemRegisterNodeId);
            string particleSystemName = FParticleSystemName[0];

            if (psd != null)
            {
                psd.RemoveNodeId(this.ParticleSystemRegisterNodeId);
                if (psd.IsEmpty()) particleSystemRegistry.Remove(psd);
            }

            particleSystemRegistry.Add(particleSystemName, this.ParticleSystemRegisterNodeId);
        }

        private void RemoveParticleSystem()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            ParticleSystemData psd = particleSystemRegistry.GetByParticleSystemId(this.ParticleSystemRegisterNodeId);
            if (psd != null)
            {
                psd.RemoveNodeId(this.ParticleSystemRegisterNodeId);
                if (psd.IsEmpty()) particleSystemRegistry.Remove(psd);
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Register", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "Emitter", Help = "Registers an emitter at the particle system.", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class ParticleSystemEmitterNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {

        [Input("Structure", DefaultString = "")]
        public IDiffSpread<string> FVariables;

        [Input("Defines")]
        public IDiffSpread<string> FDefines;

        [Input("EmitterName", DefaultString = "", IsSingle=true)]
        public IDiffSpread<string> FEmitterName;

        [Input("EmitCount", DefaultValue = 0, IsSingle=true)]
        public IDiffSpread<int> FEmitCount;

        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;

        [Import]
        protected IPluginHost2 PluginHost;
        
        private string ShaderRegisterNodeId = "";
        //private bool firstEval = true;
        private bool _ParticleSystemChanged = false;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            this.ShaderRegisterNodeId = this.PluginHost.GetNodePath(false);
            particleSystemRegistry.Changed += HandleRegistryChange;
        }

        public void Evaluate(int SpreadMax)
        {
            if (_ParticleSystemChanged)
            {
                UpdateDefines();
                UpdateEmitterSize();
                UpdateEmitterName();
                UpdateShaderVariables();
                _ParticleSystemChanged = false;
            }

            /*if (firstEval)
            {
                SetDefines();
                SetEmitterSize();
                SetEmitterName();
                SetShaderVariables();
                firstEval = false;
            }*/

            if (FParticleSystemName.IsChanged)
            {
                UpdateDefines();
                UpdateEmitterSize();
                UpdateEmitterName();
                UpdateShaderVariables();
            }

            if (FVariables.IsChanged)
            {
                SetShaderVariables();
            }

            if (FDefines.IsChanged)
            {
                SetDefines();
            }

            if (FEmitCount.IsChanged)
            {
                SetEmitterSize();
            }
            if (FEmitterName.IsChanged)
            {
                SetEmitterName();
            }

        }

        public void Dispose()
        {
            RemoveDefines();
            RemoveEmitterSize();
            RemoveEmitterName();
            RemoveShaderVariables();
        }

        private void HandleRegistryChange(object sender, ParticleSystemRegistryEventArgs e)
        {
            if (! ( e.IsShaderRegistryEvent || e.IsBufferRegistryEvent)) _ParticleSystemChanged = true;
        }

        private void SetShaderVariables ()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.SetShaderVariables(FParticleSystemName[0], this.ShaderRegisterNodeId, FVariables);
        }

        private void RemoveShaderVariables()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveShaderVariables(this.ShaderRegisterNodeId);
        }

        private void UpdateShaderVariables()
        {
            RemoveShaderVariables();
            SetShaderVariables();
        }

        private void SetDefines()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.SetDefines(FParticleSystemName[0], this.ShaderRegisterNodeId, FDefines);
        }

        private void RemoveDefines()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveDefines(this.ShaderRegisterNodeId);
        }

        private void UpdateDefines()
        {
            RemoveDefines();
            SetDefines();
        }

        private void SetEmitterSize()
        {
            if (FEmitCount.SliceCount > 0)
            {
                var particleSystemRegistry = ParticleSystemRegistry.Instance;
                particleSystemRegistry.SetEmitterSize(FParticleSystemName[0], this.ShaderRegisterNodeId, FEmitCount[0]);
            }
        }

        private void RemoveEmitterSize()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveEmitterSize(this.ShaderRegisterNodeId);
        }

        private void UpdateEmitterSize()
        {
            RemoveEmitterSize();
            SetEmitterSize();
        }

        private void SetEmitterName()
        {
            if (FEmitCount.SliceCount > 0 && FEmitterName[0]!="")
            {
                var particleSystemRegistry = ParticleSystemRegistry.Instance;
                particleSystemRegistry.SetEmitterName(FParticleSystemName[0], this.ShaderRegisterNodeId, FEmitterName[0]);

                UpdateEmitterEnum();
            }
            
        }

        private void RemoveEmitterName()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveEmitterName(this.ShaderRegisterNodeId);

            UpdateEmitterEnum();
        }

        private void UpdateEmitterEnum()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            var particleSystemData = particleSystemRegistry.GetByParticleSystemName(FParticleSystemName[0]);
            var allNames = (
                           from systemName in particleSystemRegistry.Keys
                           from emitterName in particleSystemRegistry[systemName].EmitterNames.Values
                           select emitterName
                            ).Distinct().ToArray();

            EnumManager.UpdateEnum(ParticleSystemRegistry.EMITTER_ENUM, "", allNames);
        }

        private void UpdateEmitterName()
        {
            RemoveEmitterName();
            SetEmitterName();
        }

    }

    #region PluginInfo
    [PluginInfo(Name = "Register", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "Shader", Help = "Registers a shader at the particle system.", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class ParticleSystemShaderNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {

        [Input("Structure", DefaultString = "")]
        public IDiffSpread<string> FVariables;

        [Input("Defines")]
        public IDiffSpread<string> FDefines;

        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;

        [Import]
        protected IPluginHost2 PluginHost;

        private string ShaderRegisterNodeId = "";
        //private bool firstEval = true;
        private bool _ParticleSystemChanged = false;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            this.ShaderRegisterNodeId = this.PluginHost.GetNodePath(false);
        }

        public void Evaluate(int SpreadMax)
        {

            if (_ParticleSystemChanged)
            {
                UpdateDefines();
                UpdateShaderVariables();
                _ParticleSystemChanged = false;
            }

            /*if (firstEval)
            {
                SetDefines();
                SetShaderVariables();
                firstEval = false;
            }*/

            if (FParticleSystemName.IsChanged)
            {
                UpdateDefines();
                UpdateShaderVariables();
            }

            if (FVariables.IsChanged)
            {
                SetShaderVariables();
            }

            if (FDefines.IsChanged)
            {
                SetDefines();
            }
        }

        public void Dispose()
        {
            RemoveDefines();
            RemoveShaderVariables();
        }

        private void HandleRegistryChange(object sender, ParticleSystemRegistryEventArgs e)
        {
            if (!(e.IsShaderRegistryEvent || e.IsBufferRegistryEvent)) _ParticleSystemChanged = true;
        }

        private void SetShaderVariables()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.SetShaderVariables(FParticleSystemName[0], this.ShaderRegisterNodeId, FVariables);
        }

        private void RemoveShaderVariables()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveShaderVariables(this.ShaderRegisterNodeId);
        }

        private void UpdateShaderVariables()
        {
            RemoveShaderVariables();
            SetShaderVariables();
        }

        private void SetDefines()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.SetDefines(FParticleSystemName[0], this.ShaderRegisterNodeId, FDefines);
        }

        private void RemoveDefines()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveDefines(this.ShaderRegisterNodeId);
        }

        private void UpdateDefines()
        {
            RemoveDefines();
            SetDefines();
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Register", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "Buffer", Help = "Adds buffersemantics and settings to the particle system registry.", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class ParticleSystemBufferNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {

        [Input("BufferSemantic", DefaultString = "")]
        public IDiffSpread<string> FBufferSemantic;

        [Input("Element Count")]
        public IDiffSpread<int> FEleCount;

        [Input("Stride")]
        public IDiffSpread<int> FStride;

        [Input("Buffer Mode")]
        public IDiffSpread<int> FBufMode;

        [Input("Reset Counter")]
        public IDiffSpread<bool> FResetCounter;

        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;

        [Import]
        protected IPluginHost2 PluginHost;

        private string BufferNodeId = "";
        //private bool firstEval = true;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            this.BufferNodeId = this.PluginHost.GetNodePath(false);
        }

        public void Evaluate(int SpreadMax)
        {
            if (FBufferSemantic.IsChanged || FEleCount.IsChanged || FStride.IsChanged || FBufMode.IsChanged || FResetCounter.IsChanged || FParticleSystemName.IsChanged)
            {
                UpdateBufferSettings(SpreadMax);
            }

            /*if (firstEval)
            {
                SetBufferSettings(SpreadMax);
                firstEval = false;
            }*/

        }

        public void Dispose()
        {
            RemoveBufferSettings();
        }


        private void SetBufferSettings(int SpreadMax)
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            List<ParticleSystemBufferSettings> psbsl = new List<ParticleSystemBufferSettings>();
            for (int i = 0; i < SpreadMax; i++)
            {
                ParticleSystemBufferSettings psbs = new ParticleSystemBufferSettings(FBufferSemantic[i], FEleCount[i], FStride[i], FBufMode[i], Convert.ToBoolean(FResetCounter[i]));
                psbsl.Add(psbs);
            }

            particleSystemRegistry.SetBufferSettings(FParticleSystemName[0], this.BufferNodeId, psbsl);
        }

        private void RemoveBufferSettings()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveBufferSettings(this.BufferNodeId);
        }

        private void UpdateBufferSettings(int SpreadMax)
        {
            RemoveBufferSettings();
            SetBufferSettings(SpreadMax);
        }


    }

}
