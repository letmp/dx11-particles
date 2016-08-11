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
    [PluginInfo(Name = "Info", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "ParticleSystem", Help = "Info about the specified particle system", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class InfoParticleSystemNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {
        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;

        [Output("Buffer Semantic", DefaultString = "", AutoFlush = false)]
        public ISpread<string> FBufferSemantic;

        [Output("Buffer ElementCount", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FElementCountBuf;

        [Output("Buffer Stride", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FStrideBuf;

        [Output("Buffer Mode", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FModeBuf;

        [Output("Buffer ResetCounter", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<bool> FResetCounterBuf;

        [Output("ParticleSystem ElementCount", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FElementCountPS;

        [Output("ParticleSystem Stride", AutoFlush = false)]
        public ISpread<int> FStridePS;
        
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
                FBufferSemantic.SliceCount = FElementCountBuf.SliceCount = FStrideBuf.SliceCount = FModeBuf.SliceCount = FResetCounterBuf.SliceCount = 0;

                foreach (ParticleSystemBufferSettings psbs in particleSystemData.GetBufferSettings())
                {
                    FBufferSemantic.Add(psbs.bufferSemantic);
                    FElementCountBuf.Add(psbs.elementCount);
                    FStrideBuf.Add(psbs.stride);
                    FModeBuf.Add(psbs.bufferMode);
                    FResetCounterBuf.Add(psbs.resetCounter);
                }
                FBufferSemantic.Flush();
                FElementCountBuf.Flush();
                FStrideBuf.Flush();
                FModeBuf.Flush();
                FResetCounterBuf.Flush();

                FElementCountPS[0] = particleSystemData.ElementCount;
                FElementCountPS.Flush();

                FStridePS[0] = particleSystemData.Stride;
                FStridePS.Flush();
            }

        }


    }

    #region PluginInfo
    [PluginInfo(Name = "Info", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "Emitter", Help = "Info about the specified particle system.", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class InfoEmitterNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {
        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;

        [Input("Emitter Name", EnumName = ParticleSystemRegistry.EMITTER_ENUM, Order = 2)]
        public IDiffSpread<EnumEntry> FEmitterName;

        [Output("Emitter Region")]
        public ISpread<int> FEmitterRegion;

        [Output("ParticleSystem Defines", DefaultString = "", AutoFlush = false, AllowFeedback = true)]
        public ISpread<string> FOutDefines;

        [Output("ParticleSystem ElementCount", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FElementCount;

        [Output("ParticleSystem Stride", AutoFlush = false)]
        public ISpread<int> FStride;

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

            if (FParticleSystemName.IsChanged || FEmitterName.IsChanged)
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
                FEmitterRegion.SliceCount = 0;

                FOutDefines.Add("COMPOSITESTRUCT=" + particleSystemData.StructureDefinition);
                FOutDefines.Add("MAXPARTICLECOUNT=" + particleSystemData.ElementCount);
                foreach (string define in particleSystemData.GetDefines())
                {
                    if (define != "") FOutDefines.Add(define);
                }

                foreach (string en in FEmitterName)
                {
                    string shaderRegisterNodeId = particleSystemData.GetShaderRegisterNodeId(en);
                    if (shaderRegisterNodeId != null)
                    {
                        FOutDefines.Add("EMITTEROFFSET=" + particleSystemData.GetEmitterOffset(shaderRegisterNodeId));
                        List<int> fromTo = particleSystemData.GetEmitterRegion(shaderRegisterNodeId);
                        FEmitterRegion.Add(fromTo[0]);
                        FEmitterRegion.Add(fromTo[1]);
                    }
                    
                }
                FOutDefines.Flush();

                FElementCount[0] = particleSystemData.ElementCount;
                FElementCount.Flush();

                FStride[0] = particleSystemData.Stride;
                FStride.Flush();
            }

        }

    }

    #region PluginInfo
    [PluginInfo(Name = "Info", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "Shader", Help = "Info about the specified particle system.", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class InfoShaderNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {
        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;

        [Output("ParticleSystem Defines", DefaultString = "", AutoFlush = false, AllowFeedback = true)]
        public ISpread<string> FOutDefines;

        [Output("ParticleSystem ElementCount", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FElementCount;

        [Output("ParticleSystem Stride", AutoFlush = false)]
        public ISpread<int> FStride;

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

                FElementCount[0] = particleSystemData.ElementCount;
                FElementCount.Flush();

                FStride[0] = particleSystemData.Stride;
                FStride.Flush();
            }

        }

    }


}

