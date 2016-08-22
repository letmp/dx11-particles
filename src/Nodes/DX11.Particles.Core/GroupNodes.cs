#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

using VVVV.DX11;
using VVVV.DX11.Lib.Rendering;
#endregion usings

namespace DX11.Particles.Core
{
    public class Group
    {
        public List<string> Variables = new List<string>();
        public List<string> FunctionCalls = new List<string>();
        public List<string> FunctionDefinitions = new List<string>();
        public List<string> ConstantBufferVariables = new List<string>();
        public List<IDX11RenderSemantic> RenderSemantics = new List<IDX11RenderSemantic>();
        
        public Group() { }

        public void SetAll( IEnumerable<string> variables,
                                IEnumerable<string> functionCalls, IEnumerable<string> functionDefinitions,
                                IEnumerable<string> constantBufferVariables, IEnumerable<IDX11RenderSemantic> renderSemantics)
        {
            Variables = variables.ToList();
            FunctionCalls = functionCalls.ToList();
            FunctionDefinitions = functionDefinitions.ToList();
            ConstantBufferVariables = constantBufferVariables.ToList();
            RenderSemantics = renderSemantics.ToList();
        }

        public void AddVariables(List<string> variables)
        {
            foreach (string var in variables)
            {
                if (!Variables.Contains(var)) Variables.Add(var);
            }
        }

        public void AddFunctionCalls(List<string> functionCalls)
        {
            foreach (string fc in functionCalls)
            {
                if (!FunctionCalls.Contains(fc)) FunctionCalls.Add(fc);
            }
        }

        public void AddFunctionDefinitions(List<string> functionDefinitions)
        {
            foreach (string fd in functionDefinitions)
            {
                if (!FunctionDefinitions.Contains(fd)) FunctionDefinitions.Add(fd);
            }
        }

        public void AddConstantBufferEntries(List<string> constantBufferEntries)
        {
            foreach (string cbe in constantBufferEntries)
            {
                if (!ConstantBufferVariables.Contains(cbe)) ConstantBufferVariables.Add(cbe);
            }
        }

        public void AddRenderSemantics(List<IDX11RenderSemantic> renderSemantics)
        {
            foreach (IDX11RenderSemantic rs in renderSemantics)
            {
                RenderSemantics.Add(rs);
            }
        }

       

    }

    #region PluginInfo
    [PluginInfo(Name = "Join",Category = "DX11.Particles.Groups", Help = "Joins all data needed for the grouping shader", Author="tmp", Tags = "")]
    #endregion PluginInfo
    public class GroupJoin : IPluginEvaluate
    {
        #region fields & pins
        [Input("Variable List")]
        public IDiffSpread<string> FVariables;

        [Input("Function Call")]
        public IDiffSpread<string> FFunctionCall;

        [Input("Function Definition")]
        public IDiffSpread<string> FFunctionDefinition;

        [Input("Constant Buffer Entry")]
        public IDiffSpread<string> FConstantBufferEntry;

        [Input("Custom Semantics", Order = 5000)]
        protected Pin<IDX11RenderSemantic> FInSemantics;

        [Output("Output")]
        public ISpread<Group> FOutGroup;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (! ( FVariables.IsChanged || FFunctionCall.IsChanged || FFunctionDefinition.IsChanged || FConstantBufferEntry.IsChanged || FInSemantics.IsChanged)) return;

            FOutGroup.SliceCount = 1;
            Group gn = new Group();
            gn.SetAll(FVariables, FFunctionCall, FFunctionDefinition, FConstantBufferEntry, FInSemantics);
            FOutGroup[0] = gn;
        }
    }

   
    #region PluginInfo
    [PluginInfo(Name = "Split", Category = "DX11.Particles.Groups", Help = "Splits all data needed for the grouping shader", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class GroupSplit : IPluginEvaluate
    {
        #region fields & pins

        [Input("Input", IsSingle = true)]
        public IDiffSpread<Group> FInGroup;

        [Output("Variable List")]
        public ISpread<string> FVariables;

        [Output("Function Call")]
        public ISpread<string> FFunctionCalls;

        [Output("Function Definition")]
        public ISpread<string> FFunctionDefinition;

        [Output("Constant Buffer Entry")]
        public ISpread<string> FConstantBufferEntry;

        [Output("Custom Semantics")]
        protected Pin<IDX11RenderSemantic> FOutSemantics;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (!FInGroup.IsChanged) return;

            Group gn = FInGroup[0];
            FVariables.SliceCount = FFunctionCalls.SliceCount = FFunctionDefinition.SliceCount = FConstantBufferEntry.SliceCount = FOutSemantics.SliceCount = 0;

            FVariables.AddRange(gn.Variables.ToArray());
            FFunctionCalls.AddRange(gn.FunctionCalls.ToArray());
            FFunctionDefinition.AddRange(gn.FunctionDefinitions.ToArray());
            FConstantBufferEntry.AddRange(gn.ConstantBufferVariables.ToArray());
            FOutSemantics.AddRange(gn.RenderSemantics.ToArray());
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Zip", Category = "DX11.Particles.Groups", Help = "Logical connection of Selection Bundles", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class GroupZip : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        // A spread which contains our inputs
        public Spread<IIOContainer<ISpread<Group>>> FInputs = new Spread<IIOContainer<ISpread<Group>>>();

        [Output("Output")]
        public ISpread<Group> FOutGroup;

        [Config("Input Count", DefaultValue = 2, MinValue = 2)]
        public IDiffSpread<int> FInputCountIn;

        [Import]
        public IIOFactory FIOFactory;
        #endregion fields & pins

        #region pin management
        public void OnImportsSatisfied()
        {
            FInputCountIn.Changed += HandleInputCountChanged;
        }

        private void HandlePinCountChanged<T>(ISpread<int> countSpread, Spread<IIOContainer<T>> pinSpread, Func<int, IOAttribute> ioAttributeFactory) where T : class
        {
            pinSpread.ResizeAndDispose(
                countSpread[0],
                (i) =>
                {
                    var ioAttribute = ioAttributeFactory(i + 1);
                    return FIOFactory.CreateIOContainer<T>(ioAttribute);
                }
            );
        }

        private void HandleInputCountChanged(IDiffSpread<int> sender)
        {
            HandlePinCountChanged(sender, FInputs, (i) => new InputAttribute(string.Format("Input {0}", i)));
        }
        #endregion

        public void Evaluate(int SpreadMax)
        {
            if (!FInputs.IsChanged) return;

            Group gn = new Group();
            
            for (int i = 0; i < FInputs.Count(); i++)
            {
                Group gnIn = FInputs[i].IOObject[0];
                if (gnIn != null)
                {
                    gn.AddConstantBufferEntries(gnIn.ConstantBufferVariables);
                    gn.AddFunctionCalls(gnIn.FunctionCalls);
                    gn.AddFunctionDefinitions(gnIn.FunctionDefinitions);
                    gn.AddRenderSemantics(gnIn.RenderSemantics);
                    gn.AddVariables(gnIn.Variables);
                }
            }

            FOutGroup[0] = gn;
        }
    }

}
