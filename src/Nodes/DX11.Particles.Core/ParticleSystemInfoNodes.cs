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
            if (FParticleSystemName.SliceCount != 0)
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


    }

    #region PluginInfo
    [PluginInfo(Name = "Info", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "Emitter", Help = "Info about the specified particle system.", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class InfoEmitterNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {
        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;
        
        public IDiffSpread<EnumEntry> FEmitterName;

        [Output("Emitter Region")]
        public ISpread<int> FEmitterRegion = null;

        [Output("ParticleSystem Defines", DefaultString = "", AutoFlush = false, AllowFeedback = true)]
        public ISpread<string> FOutDefines;

        [Output("ParticleSystem ElementCount", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FElementCount;

        [Output("ParticleSystem Stride", AutoFlush = false)]
        public ISpread<int> FStride;

        [Import]
        protected IPluginHost2 PluginHost;

        [Import]
        protected IIOFactory FIOFactory;

        private string EnumName;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.Changed += HandleRegistryChange;

            FParticleSystemName.Changed += (spread) => FillEnum();

            CreateEnumPin();
        }

        private void FillEnum()
        {
            if (FParticleSystemName == null || FParticleSystemName.SliceCount < 1 || FParticleSystemName[0] == null) return;

            var particleSystemData = ParticleSystemRegistry.Instance.GetByParticleSystemName(FParticleSystemName[0]);
            var enums = particleSystemData.EmitterNames.Values.ToArray();
            EnumManager.UpdateEnum(EnumName, enums.FirstOrDefault(), enums);
        }

        public void CreateEnumPin()
        {
            EnumName = ParticleSystemRegistry.EMITTER_ENUM + "_" + this.GetHashCode();

            var attr = new InputAttribute("Emitter Name");
            attr.Order = 2;
            attr.AutoValidate = true;
            attr.EnumName = EnumName;

            Type pinType = typeof(IDiffSpread<EnumEntry>);

            var pin = FIOFactory.CreateIOContainer(pinType, attr);
            FEmitterName = (IDiffSpread<EnumEntry>)(pin.RawIOObject);
        }

        public void Evaluate(int SpreadMax)
        {
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
            FillEnum();
            UpdateOutputPins();
        }

        private void UpdateOutputPins()
        {

            if (FParticleSystemName.SliceCount != 0)
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
            if (FParticleSystemName.SliceCount != 0)
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

    #region PluginInfo
    [PluginInfo(Name = "Info", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "Buffer", Help = "Info about the specified buffer.", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class InfoBufferNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {
        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;

        [Input("Filter", DefaultString = "", IsSingle = true)]
        public IDiffSpread<string> FFilter;

        public IDiffSpread<EnumEntry> FBufferName;

        [Output("Order", DefaultString = "", AutoFlush = false)]
        public ISpread<int> FOrder;

        [Output("Buffer Semantic", DefaultString = "", AutoFlush = false)]
        public ISpread<string> FBufferSemantic;

        [Output("Buffer ElementCount", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FElementCountBuf;

        [Output("Buffer Value Range", Visibility = PinVisibility.OnlyInspector, DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FValueRange;

        [Output("Buffer Stride", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FStrideBuf;

        [Output("Buffer Mode", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<int> FModeBuf;

        [Output("Buffer ResetCounter", DefaultValue = 0, AutoFlush = false, AllowFeedback = true)]
        public ISpread<bool> FResetCounterBuf;

        [Import]
        protected IPluginHost2 PluginHost;

        [Import]
        protected IIOFactory FIOFactory;

        private string EnumName;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.Changed += HandleRegistryChange;

            FParticleSystemName.Changed += (spread) => FillEnum();

            CreateEnumPin();
        }

        private void FillEnum()
        {
            if (FParticleSystemName == null || FParticleSystemName.SliceCount < 1 || FParticleSystemName[0] == null) return;

            var particleSystemData = ParticleSystemRegistry.Instance.GetByParticleSystemName(FParticleSystemName[0]);
            
            var enums = particleSystemData.BufferSettings
                .SelectMany(kvp => kvp.Value.Select(value => value.bufferSemantic)).ToArray();

            if (FFilter.SliceCount > 0 && FFilter[0] != "")
            {
                enums = enums.Where(v => v.Contains(FFilter[0])).ToArray();
            }

            EnumManager.UpdateEnum(EnumName, enums.FirstOrDefault(), enums);
        }

        public void CreateEnumPin()
        {
            EnumName = ParticleSystemRegistry.BUFFER_ENUM + "_" + this.GetHashCode();

            var attr = new InputAttribute("Buffer Name");
            attr.Order = 2;
            attr.AutoValidate = true;
            attr.EnumName = EnumName;

            Type pinType = typeof(IDiffSpread<EnumEntry>);

            var pin = FIOFactory.CreateIOContainer(pinType, attr);
            FBufferName = (IDiffSpread<EnumEntry>)(pin.RawIOObject);
        }

        public void Evaluate(int SpreadMax)
        {
            if (FFilter.IsChanged)
            {
                FillEnum();
            }
            if (FParticleSystemName.IsChanged || FBufferName.IsChanged || FFilter.IsChanged)
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
            FillEnum();
            UpdateOutputPins();
        }

        private void UpdateOutputPins()
        {
            FOrder.SliceCount = FBufferSemantic.SliceCount = FElementCountBuf.SliceCount = FValueRange.SliceCount =
            FStrideBuf.SliceCount = FModeBuf.SliceCount = FResetCounterBuf.SliceCount = 0;

            if (FParticleSystemName.SliceCount != 0)
            {
                var particleSystemData = ParticleSystemRegistry.Instance.GetByParticleSystemName(FParticleSystemName[0]);
                if (particleSystemData != null)
                {
                    // gett all buffer settings
                    var pbsl = particleSystemData.BufferSettings
                        .SelectMany(kvp => kvp.Value).ToArray();

                    // now find the buffer settings specified by enum
                    for (int i = 0; i < pbsl.Count(); i++)
                    {
                        ParticleSystemBufferSettings pbs = pbsl[i];
                        for (int j = 0; j < FBufferName.SliceCount; j++)
                        {
                           if(pbs.bufferSemantic == FBufferName[j])
                            {
                                // we found the specified buffer settings
                                FOrder.Add(i);
                                FBufferSemantic.Add(pbs.bufferSemantic);
                                FElementCountBuf.Add(pbs.elementCount);
                                FValueRange.Add(pbs.valueRange);
                                FStrideBuf.Add(pbs.stride);
                                FModeBuf.Add(pbs.bufferMode);
                                FResetCounterBuf.Add(pbs.resetCounter);
                                
                                break;
                            }
                        }
                    }
                }

                FOrder.Flush();
                FBufferSemantic.Flush();
                FElementCountBuf.Flush();
                FValueRange.Flush();
                FStrideBuf.Flush();
                FModeBuf.Flush();
                FResetCounterBuf.Flush();
            }

        }

    }

    #region PluginInfo
    [PluginInfo(Name = "Info", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "Attribute", Help = "Info about the specified attribute.", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class InfoAttributeNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IDisposable
    {
        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 2, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;
        
        public IDiffSpread<EnumEntry> FAttributeName;

        [Output("VariableType", DefaultString = "", AutoFlush = false)]
        public ISpread<string> FVariableType;

        [Output("VariableName", DefaultString = "", AutoFlush = false)]
        public ISpread<string> FVariableName;
        
        [Output("Stride", DefaultString = "", AutoFlush = false)]
        public ISpread<int> FStride;

        [Import]
        protected IPluginHost2 PluginHost;

        [Import]
        protected IIOFactory FIOFactory;

        private string EnumName;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.Changed += HandleRegistryChange;

            FParticleSystemName.Changed += (spread) => FillEnum();

            CreateEnumPin();
        }

        private void FillEnum()
        {
            if (FParticleSystemName == null || FParticleSystemName.SliceCount < 1 || FParticleSystemName[0] == null) return;

            var particleSystemData = ParticleSystemRegistry.Instance.GetByParticleSystemName(FParticleSystemName[0]);

            var enums = particleSystemData.ShaderVariables
                .SelectMany(kvp => kvp.Value).Distinct().ToArray();
            
            EnumManager.UpdateEnum(EnumName, enums.FirstOrDefault(), enums);
        }

        public void CreateEnumPin()
        {
            EnumName = ParticleSystemRegistry.ATTRIBUTE_ENUM + "_" + this.GetHashCode();

            var attr = new InputAttribute("Attribute Name");
            attr.Order = 2;
            attr.AutoValidate = true;
            attr.EnumName = EnumName;

            Type pinType = typeof(IDiffSpread<EnumEntry>);

            var pin = FIOFactory.CreateIOContainer(pinType, attr);
            FAttributeName = (IDiffSpread<EnumEntry>)(pin.RawIOObject);
        }

        public void Evaluate(int SpreadMax)
        {
            if (FParticleSystemName.IsChanged)
            {
                FillEnum();
            }
            if (FParticleSystemName.IsChanged || FAttributeName.IsChanged )
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
            FillEnum();
            UpdateOutputPins();
        }

        private void UpdateOutputPins()
        {
            FVariableName.SliceCount = FVariableType.SliceCount = FStride.SliceCount = 0;

            for (int i = 0; i < FAttributeName.SliceCount; i++)
            {
                string varDefinition = FAttributeName[i];
                Definition def = new Definition(varDefinition);
                FVariableName.Add(def.Name);
                FVariableType.Add(def.Type);
                FStride.Add(def.Size * 4);
            }

            FVariableName.Flush();
            FVariableType.Flush();
            FStride.Flush();

        }

    }

}

