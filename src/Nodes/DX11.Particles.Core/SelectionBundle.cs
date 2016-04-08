#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.DX11;
using VVVV.DX11.Lib.Rendering;

using VVVV.Core.Logging;
#endregion usings

namespace DX11.Particles.Core
{
    public class SelectionBundle
    {
        public List<string> Variables = new List<string>();
        public List<string> FunctionDefinitions = new List<string>();
        public List<string> ConstantBufferVariables = new List<string>();
        public List<IDX11RenderSemantic> RenderSemantics = new List<IDX11RenderSemantic>();
        public string FunctionCall = "";

        public SelectionBundle() { }

        public void SetAll( IEnumerable<string> variables,
                                string functionCall, IEnumerable<string> functionDefinitions,
                                IEnumerable<string> constantBufferVariables, IEnumerable<IDX11RenderSemantic> renderSemantics)
        {
            Variables = variables.ToList();
            FunctionCall = functionCall;
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

        public void LogicAndOr(Spread<IIOContainer<ISpread<SelectionBundle>>> FInputs, bool isLogicAnd = false)
        {
            List<string> functioncalls = new List<string>();
            for (int i = 0; i < FInputs.Count(); i++)
            {
                SelectionBundle sbIn = FInputs[i].IOObject[0];
                if (sbIn != null)
                {
                    this.AddVariables(sbIn.Variables);
                    this.AddFunctionDefinitions(sbIn.FunctionDefinitions);
                    this.AddConstantBufferEntries(sbIn.ConstantBufferVariables);
                    this.AddRenderSemantics(sbIn.RenderSemantics);
                    functioncalls.Add(sbIn.FunctionCall);
                }
            }

            this.SetFunctionCallAndOr(functioncalls, isLogicAnd);
        }

        public void SetFunctionCallAndOr(List<string> functionCalls, bool isLogicAnd = false)
        {
            int size = functionCalls.Count();
            for (int i = 0; i < size; i++)
            {
                if (i == 0) FunctionCall += "( ";

                if (i != 0 && isLogicAnd) FunctionCall += " && ";
                if (i != 0 && !isLogicAnd) FunctionCall += " || ";

                FunctionCall += functionCalls[i];

                if (i == size - 1) FunctionCall += " )";
            }
        }

        public void SetFunctionCallNot()
        {
            FunctionCall = "!( " + FunctionCall + ")";
        }

        public string BuildShaderCode(string blueprint){
            blueprint = blueprint.Replace("{structvariables}", string.Join(System.Environment.NewLine, Variables));
            blueprint = blueprint.Replace("{cbuffervariables}", string.Join(System.Environment.NewLine, ConstantBufferVariables));
            blueprint = blueprint.Replace("{functiondefinitions}", string.Join(System.Environment.NewLine, FunctionDefinitions));
            if (FunctionCall!="")
                blueprint = blueprint.Replace("{functioncalls}", FunctionCall);
            else
                blueprint = blueprint.Replace("{functioncalls}", "true");
            return blueprint;
        }
        
    }

    #region PluginInfo
    [PluginInfo(Name = "Join",Category = "DX11.Particles.Selection", Help = "Joins all data needed for the selection shader", Author="tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectionJoin : IPluginEvaluate
    {
        #region fields & pins
        [Input("Variable List")]
        public IDiffSpread<string> FVariables;

        [Input("Function Call", IsSingle = true)]
        public IDiffSpread<string> FFunctionCall;

        [Input("Function Definition", IsSingle=true)]
        public IDiffSpread<string> FFunctionDefinition;

        [Input("Constant Buffer Entry")]
        public IDiffSpread<string> FConstantBufferEntry;

        [Input("Custom Semantics", Order = 5000)]
        protected Pin<IDX11RenderSemantic> FInSemantics;

        [Output("Output")]
        public ISpread<SelectionBundle> FOutSelectionBundle;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (! ( FVariables.IsChanged || FFunctionCall.IsChanged || FFunctionDefinition.IsChanged || FConstantBufferEntry.IsChanged || FInSemantics.IsChanged)) return;

            FOutSelectionBundle.SliceCount = 1;
            SelectionBundle sb = new SelectionBundle();
            sb.SetAll(FVariables, FFunctionCall[0], FFunctionDefinition, FConstantBufferEntry, FInSemantics);
            FOutSelectionBundle[0] = sb;
        }
    }

   
    #region PluginInfo
    [PluginInfo(Name = "Split", Category = "DX11.Particles.Selection", Help = "Splits all data needed for the selection shader", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectionSplit : IPluginEvaluate
    {
        #region fields & pins

        [Input("Input", IsSingle = true)]
        public IDiffSpread<SelectionBundle> FInSelectionBundle;

        [Output("Variable List")]
        public ISpread<string> FVariables;

        [Output("Function Call")]
        public ISpread<string> FFunctionCall;

        [Output("Function Definition")]
        public ISpread<string> FFunctionDefinition;

        [Output("Constant Buffer Entry")]
        public ISpread<string> FConstantBufferEntry;

        [Output("Custom Semantics")]
        protected Pin<IDX11RenderSemantic> FOutSemantics;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (!FInSelectionBundle.IsChanged) return;

            SelectionBundle sb = FInSelectionBundle[0];
            FVariables.SliceCount = FFunctionCall.SliceCount = FFunctionDefinition.SliceCount = FConstantBufferEntry.SliceCount = FOutSemantics.SliceCount = 0;

            FVariables.AddRange(sb.Variables.ToArray());
            FFunctionCall.Add(sb.FunctionCall);
            FFunctionDefinition.AddRange(sb.FunctionDefinitions.ToArray());
            FConstantBufferEntry.AddRange(sb.ConstantBufferVariables.ToArray());
            FOutSemantics.AddRange(sb.RenderSemantics.ToArray());
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "AND", Category = "DX11.Particles.Selection", Help = "Logical connection of Selection Bundles", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectionLogicAND : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        // A spread which contains our inputs
        public Spread<IIOContainer<ISpread<SelectionBundle>>> FInputs = new Spread<IIOContainer<ISpread<SelectionBundle>>>();

        [Output("Output")]
        public ISpread<SelectionBundle> FOutSelectionBundle;

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

            SelectionBundle sb = new SelectionBundle();
            sb.LogicAndOr(FInputs,true);
            FOutSelectionBundle[0] = sb;
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "OR", Category = "DX11.Particles.Selection", Help = "Logical connection of Selection Bundles", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectionLogicOR : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        // A spread which contains our inputs
        public Spread<IIOContainer<ISpread<SelectionBundle>>> FInputs = new Spread<IIOContainer<ISpread<SelectionBundle>>>();

        [Output("Output")]
        public ISpread<SelectionBundle> FOutSelectionBundle;

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
            SelectionBundle sb = new SelectionBundle();
            sb.LogicAndOr(FInputs);
            FOutSelectionBundle[0] = sb;
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "NOT", Category = "DX11.Particles.Selection", Help = "Logical negation of Selection Bundle", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectionLogicNOT : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input", IsSingle = true)]
        public IDiffSpread<SelectionBundle> FInSelectionBundle;

        [Output("Output")]
        public ISpread<SelectionBundle> FOutSelectionBundle;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (!FInSelectionBundle.IsChanged) return;

            SelectionBundle sb = FInSelectionBundle[0];
            SelectionBundle sbNew = new SelectionBundle();
            if (sb != null)
            {
                sbNew.Variables = sb.Variables;
                sbNew.ConstantBufferVariables = sb.ConstantBufferVariables;
                sbNew.FunctionCall = sb.FunctionCall;
                sbNew.FunctionDefinitions = sb.FunctionDefinitions;
                sbNew.RenderSemantics = sb.RenderSemantics;
                sbNew.SetFunctionCallNot();
            }
            
            FOutSelectionBundle[0] = sbNew;
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Switch", Category = "DX11.Particles.Selection", Help = "Switches between multiple SelectionBundle inputs", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectionSwitch : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins

        [Input("Switch", IsSingle = true, DefaultValue=0)]
        public ISpread<int> FSwitch;

        public Spread<IIOContainer<ISpread<SelectionBundle>>> FInputs = new Spread<IIOContainer<ISpread<SelectionBundle>>>();

        [Output("Output")]
        public ISpread<SelectionBundle> FOutSelectionBundle;

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

            int index = FSwitch[0];
            SelectionBundle sb = FInputs[index].IOObject[0];

            SelectionBundle sbNew = new SelectionBundle();
            if (sb != null)
            {
                sbNew.Variables = sb.Variables;
                sbNew.ConstantBufferVariables = sb.ConstantBufferVariables;
                sbNew.FunctionCall = sb.FunctionCall;
                sbNew.FunctionDefinitions = sb.FunctionDefinitions;
                sbNew.RenderSemantics = sb.RenderSemantics;
            }

            FOutSelectionBundle[0] = sbNew;

        }
    }

    #region PluginInfo
    [PluginInfo(Name = "BuildShaderCode", Category = "DX11.Particles.Selection", Help = "Takes a string as input and replaces keywords with data in the selectionbundle", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectionBuildShaderCode : IPluginEvaluate
    {
        #region fields & pins

        [Input("Input", IsSingle = true)]
        public IDiffSpread<SelectionBundle> FInSelectionBundle;
        
        [Input("Shader Blueprint", IsSingle = true)]
        public IDiffSpread<string> FBlueprint;
        
        [Output("ShaderCode")]
        public ISpread<string> FOutShaderCode;

        [Output("Variable List")]
        public ISpread<string> FVariables;

        [Output("Custom Semantics")]
        protected Pin<IDX11RenderSemantic> FOutSemantics;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (!(FInSelectionBundle.IsChanged || FBlueprint.IsChanged)) return;

            FOutShaderCode.SliceCount = FOutSemantics.SliceCount = FVariables.SliceCount = 0;

            SelectionBundle sb = FInSelectionBundle[0];
            if (sb == null)
            {
                sb = new SelectionBundle();
                sb.Variables.Add("int oneVariableNeeded;");
            }
            
            FOutShaderCode.Add(sb.BuildShaderCode(FBlueprint[0]));
            FVariables.AddRange(sb.Variables.ToArray());

            // if rendersemantic is null we have to add a fakesemantic. otherwise shaders would fail
            foreach (var rs in sb.RenderSemantics)
            {
                if (rs == null) FOutSemantics.Add(new StructuredBufferRenderSemantic("fakesemantic", false));
                else FOutSemantics.Add(rs);
            }
           
        }
    }
}
