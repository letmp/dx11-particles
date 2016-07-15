using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils;
using System.Linq;

namespace DX11.Particles.Core
{
    #region PluginInfo
    [PluginInfo(Name = "GetEmitterOffset", AutoEvaluate = true, Category = "DX11.Particles.Core", Version = "", Help = "Returns the region of an emitter in a specified particle system.", Author = "tmp", Tags = "")]
    #endregion PluginInfo

    public class GetEmitterOffsetNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Input("ParticleSystem", EnumName = ParticleSystemRegistry.PARTICLESYSTEM_ENUM, Order = 1, IsSingle = true, DefaultEnumEntry = ParticleSystemRegistry.DEFAULT_ENUM)]
        public IDiffSpread<EnumEntry> FParticleSystemName;

        [Input("Emitter Name", EnumName = ParticleSystemRegistry.EMITTER_ENUM, Order = 2)]
        public IDiffSpread<EnumEntry> FEmitterName;

        [Output("Region")]
        public ISpread<int> FEmitterRegion;

        private bool _ParticleSystemChanged = false;

        public void OnImportsSatisfied()
        {
            var particleSystemRegistry = ParticleSystemRegistry.Instance;
            particleSystemRegistry.Changed += HandleRegistryChange;
        }

        public void Evaluate(int SpreadMax)
        {
            if (_ParticleSystemChanged || FParticleSystemName.IsChanged || FEmitterName.IsChanged)
            {
                UpdateEnums();
                UpdateOutputPins();
            }
        }

        private void HandleRegistryChange(object sender, ParticleSystemRegistryEventArgs e)
        {
            _ParticleSystemChanged = true;
        }

        private void UpdateEnums()
        {
            ParticleSystemData psd = ParticleSystemRegistry.Instance[FParticleSystemName[0]];
            if (psd != null)
            {
                EnumManager.UpdateEnum(ParticleSystemRegistry.EMITTER_ENUM, "", psd.EmitterNames.Values.Distinct().ToArray());
            }
        }

        private void UpdateOutputPins()
        {
            FEmitterRegion.SliceCount = 0;

            var particleSystemData = ParticleSystemRegistry.Instance.GetByParticleSystemName(FParticleSystemName[0]);
            if (particleSystemData != null)
            {
                foreach (string en in FEmitterName)
                {
                    string shaderRegisterNodeId = particleSystemData.GetShaderRegisterNodeId(en);
                    if (shaderRegisterNodeId != null)
                    {
                        List<int> fromTo = particleSystemData.GetEmitterRegion(shaderRegisterNodeId);
                        FEmitterRegion.Add(fromTo[0]);
                        FEmitterRegion.Add(fromTo[1]);
                    }
                }

                
           }
        }

    }
}
