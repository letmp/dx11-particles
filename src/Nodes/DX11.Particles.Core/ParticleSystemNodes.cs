using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Core.Logging;
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

        [Input("BufferSemantics")]
        public IDiffSpread<string> FBufferSemantics;

        [Output("StructureDefinition", DefaultString = "", AutoFlush = false)]
        public ISpread<string> FStructureDefinition;

        [Output("Additional Buffers", DefaultString = "", AutoFlush = false)]
        public ISpread<string> FBuffers;

        [Output("Element Count", AutoFlush = false)]
        public ISpread<int> FEleCount;

        [Output("Stride", AutoFlush = false)]
        public ISpread<int> FStride;

        [Import()]
        public ILogger FLogger;

        [Import]
        protected IPluginHost2 PluginHost;

        private string ParticleSystemNodeId = "";
        private bool firstEval = true;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.Changed += HandleRegistryChange;
            this.ParticleSystemNodeId = PluginHost.GetNodePath(false);
        }

        public void Evaluate(int SpreadMax)
        {
            if (firstEval)
            {
                AddParticleSystem();
                UpdateBufferSemantics();
                firstEval = false;
            }

            if (FParticleSystemName.IsChanged)
            {
                AddParticleSystem();
            }
            if (FBufferSemantics.IsChanged)
            {
                UpdateBufferSemantics();
            }
        }

        public void Dispose()
        {
            ParticleSystemRegistry.Instance.Changed -= HandleRegistryChange;
            RemoveParticleSystem();
        }

        private void HandleRegistryChange(object sender, ParticleSystemRegistryEventArgs e)
        {
            UpdateOutputPins();
        }

        private void AddParticleSystem()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            ParticleSystemData psd = particleSystemRegistry.GetByParticleSystemId(this.ParticleSystemNodeId);
            string particleSystemName = FParticleSystemName[0];

            if (psd != null)
            {
                psd.RemoveNodeId(this.ParticleSystemNodeId);
                if (psd.IsEmpty()) particleSystemRegistry.Remove(psd);
            }

            particleSystemRegistry.Add(particleSystemName, this.ParticleSystemNodeId, FBufferSemantics);
        }

        private void RemoveParticleSystem()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            ParticleSystemData psd = particleSystemRegistry.GetByParticleSystemId(this.ParticleSystemNodeId);
            if (psd != null)
            {
                psd.RemoveNodeId(this.ParticleSystemNodeId);
                if (psd.IsEmpty()) particleSystemRegistry.Remove(psd);
            }
        }

        private void UpdateBufferSemantics()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            string particleSystemName = FParticleSystemName[0];
            particleSystemRegistry.UpdateBufferSemantics(particleSystemName, FBufferSemantics);
        }

        private void UpdateOutputPins()
        {
            var particleSystemData = ParticleSystemRegistry.Instance.GetByParticleSystemId(ParticleSystemNodeId);
            if (particleSystemData != null)
            {
                FStructureDefinition[0] = particleSystemData.StructureDefinition;
                FStructureDefinition.Flush();

                FBuffers.SliceCount = 0;
                FBuffers.AssignFrom(particleSystemData.BufferNames.Values.Distinct().ToArray());
                EnumManager.UpdateEnum(ParticleSystemRegistry.EMITTER_ENUM, "", particleSystemData.BufferNames.Values.Distinct().ToArray());
                FBuffers.Flush();

                FEleCount[0] = particleSystemData.ElementCount;
                FEleCount.Flush();

                FStride[0] = particleSystemData.Stride;
                FStride.Flush();

               
            }
            
        }

        
    }

    #region PluginInfo
    [PluginInfo(Name = "Info", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "ParticleSystem", Help = "Info about the specified particle system", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class InfoParticleSystemNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {
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

        [Import()]
        public ILogger FLogger;

        [Import]
        protected IPluginHost2 PluginHost;
        bool firstEval = true;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.Changed += HandleRegistryChange;
        }

        public void Evaluate(int SpreadMax)
        {
            if (firstEval)
            {
                UpdateOutputPins();
                firstEval = false;
            }

            if (FParticleSystemName.IsChanged)
            {
                UpdateOutputPins();
            }
        }

        public void Dispose()
        {
            ParticleSystemRegistry.Instance.Changed -= HandleRegistryChange;
        }

        private void HandleRegistryChange(object sender, ParticleSystemRegistryEventArgs e)
        {
            UpdateOutputPins();
        }

        private void UpdateOutputPins()
        {
            var particleSystemData = ParticleSystemRegistry.Instance.GetByParticleSystemName(FParticleSystemName[0]);
            if (particleSystemData != null)
            {
                FOutDefines.SliceCount = 0;
                FOutDefines.Add("COMPOSITESTRUCT=" + particleSystemData.StructureDefinition);
                FOutDefines.Add("MAXPARTICLECOUNT=" + particleSystemData.ElementCount);

                foreach (string define in particleSystemData.GetDefines())
                {
                    if (define != "") FOutDefines.Add(define);
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
