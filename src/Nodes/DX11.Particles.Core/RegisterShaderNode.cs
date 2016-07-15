using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils;

namespace DX11.Particles.Core
{
    #region PluginInfo
    [PluginInfo(Name = "Register", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "Shader", Help = "Adds mandatory variables of a shader to the particle system registry and outputs all composited variables for the particle system.", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class RegisterShaderNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {

        [Input("Structure", DefaultString = "")]
        public IDiffSpread<string> FVariables;

        [Input("Defines")]
        public IDiffSpread<string> FDefines;

        [Input("EmitterName", DefaultString = "", IsSingle=true)]
        public IDiffSpread<string> FEmitterName;

        [Input("GroupName", DefaultString = "", IsSingle = true)]
        public IDiffSpread<string> FGroupName;

        [Input("EmitCount", DefaultValue = 0, IsSingle=true)]
        public IDiffSpread<int> FEmitCount;

        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;

        [Output("Defines", DefaultString = "", AutoFlush = false, AllowFeedback = true)]
        public ISpread<string> FOutDefines;

        [Output("BufferSemantics", DefaultString = "", AutoFlush = false, AllowFeedback = true)]
        public ISpread<string> FOutBufferSemantics;

        [Output("Element Count", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FElementCount;

        [Output("Stride", AutoFlush = false)]
        public ISpread<int> FStride;

        [Import]
        protected IPluginHost2 PluginHost;
        
        private string ShaderNodeId = "";
        private bool _ParticleSystemChanged = false;
        private bool firstEval = true;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.Changed += HandleRegistryChange;
            this.ShaderNodeId = this.PluginHost.GetNodePath(false);
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

            if (FParticleSystemName.IsChanged)
            {
                UpdateDefines();
                UpdateEmitterSize();
                UpdateEmitterName();
                UpdateShaderVariables();
            }

            if (firstEval)
            {
                SetShaderVariables();
                SetDefines();
                SetEmitterSize();
                SetEmitterName();
                firstEval = false;
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
            if (FGroupName.IsChanged)
            {
                SetGroupName();
            }

        }

        public void Dispose()
        {
            RemoveDefines();
            RemoveEmitterSize();
            RemoveEmitterName();
            RemoveGroupName();
            RemoveShaderVariables();
            ParticleSystemRegistry.Instance.Changed -= HandleRegistryChange;
        }

        private void HandleRegistryChange(object sender, ParticleSystemRegistryEventArgs e)
        {
            if (!e.IsShaderEvent) _ParticleSystemChanged = true;
            UpdateOutputPins();
        }

        private void SetShaderVariables ()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.SetShaderVariables(FParticleSystemName[0], this.ShaderNodeId, FVariables);
        }

        private void RemoveShaderVariables()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveShaderVariables(this.ShaderNodeId);
        }

        private void UpdateShaderVariables()
        {
            RemoveShaderVariables();
            SetShaderVariables();
        }

        private void SetDefines()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.SetDefines(FParticleSystemName[0], this.ShaderNodeId, FDefines);
        }

        private void RemoveDefines()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveDefines(this.ShaderNodeId);
        }

        private void UpdateDefines()
        {
            RemoveDefines();
            SetDefines();
        }

        private void SetEmitterSize()
        {
            if (FEmitCount[0] > 0)
            {
                var particleSystemRegistry = ParticleSystemRegistry.Instance;
                particleSystemRegistry.SetEmitterSize(FParticleSystemName[0], this.ShaderNodeId, FEmitCount[0]);
            }
        }

        private void RemoveEmitterSize()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveEmitterSize(this.ShaderNodeId);
        }

        private void UpdateEmitterSize()
        {
            RemoveEmitterSize();
            SetEmitterSize();
        }

        private void SetEmitterName()
        {
            if (FEmitCount[0] > 0 && FEmitterName[0]!="")
            {
                var particleSystemRegistry = ParticleSystemRegistry.Instance;
                particleSystemRegistry.SetEmitterName(FParticleSystemName[0], this.ShaderNodeId, FEmitterName[0]);
            }
        }

        private void RemoveEmitterName()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveEmitterName(this.ShaderNodeId);
        }

        private void UpdateEmitterName()
        {
            RemoveEmitterName();
            SetEmitterName();
        }

        private void SetGroupName()
        {
            if (FGroupName[0] != "")
            {
                var particleSystemRegistry = ParticleSystemRegistry.Instance;
                particleSystemRegistry.SetGroupName(FParticleSystemName[0], this.ShaderNodeId, FGroupName[0]);
            }
        }

        private void RemoveGroupName()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.RemoveGroupName(this.ShaderNodeId);
        }

        private void UpdateGroupName()
        {
            RemoveGroupName();
            SetGroupName();
        }

        private void UpdateOutputPins()
        {
            var particleSystemData = ParticleSystemRegistry.Instance.GetByShaderId(this.ShaderNodeId);
            if (particleSystemData != null)
            {
                FOutDefines.SliceCount = 0;
                FOutDefines.Add("COMPOSITESTRUCT=" + particleSystemData.StructureDefinition);
                FOutDefines.Add("MAXPARTICLECOUNT=" + particleSystemData.ElementCount);

                foreach (string define in particleSystemData.GetDefines())
                {
                    if (define != "") FOutDefines.Add(define);
                }
                
                if (FEmitCount[0] > 0) // this is an emitter, so we output an offset too
                {
                    FOutDefines.Add("EMITTEROFFSET=" + particleSystemData.GetEmitterOffset(this.ShaderNodeId));
                }

                FOutDefines.Flush();

                FOutBufferSemantics.SliceCount = 0;
                FOutBufferSemantics.AssignFrom(particleSystemData.BufferSemantics); 
                FOutBufferSemantics.Flush();

                FElementCount[0] = particleSystemData.ElementCount;
                FElementCount.Flush();

                FStride[0] = particleSystemData.Stride;
                FStride.Flush();
            }
            
        }

    }
}
