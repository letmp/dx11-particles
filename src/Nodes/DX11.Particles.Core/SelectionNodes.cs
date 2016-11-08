#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

using VVVV.DX11;
using VVVV.Utils.Streams;
using VVVV.Nodes.Generic;
#endregion usings

namespace DX11.Particles.Core
{
    public class Selector
    {
        public string FunctionCall = "";
        public List<string> Variables = new List<string>();
        public List<string> FunctionDefinitions = new List<string>();
        public List<string> ConstantBufferVariables = new List<string>();
        public Dictionary<string, IDX11RenderSemantic> RenderSemantics = new Dictionary<string, IDX11RenderSemantic>();

        public Selector() { }

        public void SetAll(string functionCall, IEnumerable<string> variables,
                                 IEnumerable<string> functionDefinitions,
                                IEnumerable<string> constantBufferVariables, IEnumerable<IDX11RenderSemantic> renderSemantics)
        {
            FunctionCall = functionCall;
            Variables = variables.ToList();
            FunctionDefinitions = functionDefinitions.ToList();
            ConstantBufferVariables = constantBufferVariables.ToList();
            
            foreach (IDX11RenderSemantic rs in renderSemantics.ToList())
            {
                if (rs != null) RenderSemantics[rs.Semantic] = rs;
            }
        }

        public void SetFunctionCall(string functionCall)
        {
            FunctionCall = functionCall;
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
                RenderSemantics[rs.Semantic] = rs;
            }
        }

        public List<IDX11RenderSemantic> GetRenderSemantics()
        {
            List<IDX11RenderSemantic> rsList = new List<IDX11RenderSemantic>();
            rsList.AddRange(RenderSemantics.Values);
            return rsList;
        }
        
    }

    #region PluginInfo
    [PluginInfo(Name = "SelectorData", Category = "DX11.Particles.Selection", Help = "Creates unique functioncalls, constantbuffer entries and semantics.", Author = "tmp")]
    #endregion PluginInfo
    public class StringSelectorDataNode : IPluginEvaluate
    {
        #region fields & pins

        [Input("FunctionName")]
        public IDiffSpread<string> FInFunctionName;

        [Input("FunctionArgument")]
        public IDiffSpread<string> FInFunctionArgument;

        [Input("VariableType")]
        public IDiffSpread<string> FInVariableType;

        [Input("VariableName")]
        public IDiffSpread<string> FInVariableName;

        [Input("Semantic")]
        public IDiffSpread<string> FInSemantic;

        [Input("Semantic Count", IsSingle = true)]
        public IDiffSpread<int> FInSemanticCount;

        [Output("FunctionCall")]
        public ISpread<string> FOutFunctionCall;

        [Output("ConstantBuffer Entry")]
        public ISpread<string> FOutBufferEntry;

        [Output("Semantic")]
        public ISpread<string> FOutSemantic;
        

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            
            if (!(FInFunctionName.IsChanged || FInFunctionArgument.IsChanged || FInVariableType.IsChanged || FInVariableName.IsChanged || FInSemantic.IsChanged || FInSemanticCount.IsChanged)) return;

            FOutFunctionCall.SliceCount = FOutBufferEntry.SliceCount = FOutSemantic.SliceCount = 0;
            
            for (int i = 0; i < FInSemanticCount[0]; i++)
            {

                string uniqueSuffix = "_" + this.GetHashCode() + "_" + i;
                
                var uniqueVariableNames = FInVariableName
                            .Select(x => x + uniqueSuffix)
                            .ToList();
                
                FOutFunctionCall.Add (FInFunctionName[i] + "(" + String.Join(", ", FInFunctionArgument) + "," + String.Join(", ", uniqueVariableNames) + ")");

                for ( int j = 0; j < FInVariableName.SliceCount; j++)
                {
                    FOutBufferEntry.Add( FInVariableType[j] + " " + FInVariableName[j] + uniqueSuffix + " : " + FInSemantic[j] + uniqueSuffix + ";");
                    FOutSemantic.Add(FInSemantic[j] + uniqueSuffix);
                }
                
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Join",Category = "DX11.Particles.Selection", Help = "Joins data for a selector", Author="tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectorJoin : IPluginEvaluate
    {
        #region fields & pins
        [Input("Function Call")]
        public IDiffSpread<string> FFunctionCall;

        [Input("Variable List")]
        public IDiffSpread<string> FVariables;
        
        [Input("Function Definition")]
        public IDiffSpread<string> FFunctionDefinition;

        [Input("Constant Buffer Entry")]
        public IDiffSpread<string> FConstantBufferEntry;

        [Input("Custom Semantics", Order = 5000)]
        protected Pin<IDX11RenderSemantic> FInSemantics;

        [Output("Output")]
        public ISpread<Selector> FOutSelector;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (! ( FVariables.IsChanged || FFunctionCall.IsChanged || FFunctionDefinition.IsChanged || FConstantBufferEntry.IsChanged || FInSemantics.IsChanged)) return;

            FOutSelector.SliceCount = FFunctionCall.SliceCount;
            for (int i = 0; i < FFunctionCall.SliceCount; i++)
            {
                Selector selector = new Selector();
                selector.SetAll(FFunctionCall[i], FVariables, FFunctionDefinition, FConstantBufferEntry, FInSemantics);
                FOutSelector[i] = selector;
            }
        }
    }

   
    #region PluginInfo
    [PluginInfo(Name = "Split", Category = "DX11.Particles.Selection", Help = "Outputs the data of selectors", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectorSplit : IPluginEvaluate
    {
        #region fields & pins

        [Input("Input")]
        public IDiffSpread<Selector> FInSelector;

        [Input("Prevent from doubles", DefaultBoolean = false, IsSingle = true)]
        public IDiffSpread<bool> FPreventDoubles;

        [Output("Function Call")]
        public ISpread<ISpread<string>> FFunctionCall;

        [Output("Variable List")]
        public ISpread<ISpread<string>> FVariables;
        
        [Output("Function Definition")]
        public ISpread<ISpread<string>> FFunctionDefinition;

        [Output("Constant Buffer Entry")]
        public ISpread<ISpread<string>> FConstantBufferEntry;

        [Output("Custom Semantics")]
        public ISpread<ISpread<IDX11RenderSemantic>> FOutSemantics;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (!FInSelector.IsChanged) return;

            if (FPreventDoubles[0]) // output only unique data
            {
                FVariables.SliceCount = FFunctionCall.SliceCount = FFunctionDefinition.SliceCount = FConstantBufferEntry.SliceCount = FOutSemantics.SliceCount = 1;
                FFunctionCall[0].SliceCount = FVariables[0].SliceCount  = FFunctionDefinition[0].SliceCount = FConstantBufferEntry[0].SliceCount = FOutSemantics[0].SliceCount = 0;

                for (int i = 0; i < SpreadMax; i++)
                {
                    Selector selector = FInSelector[i];
                    if (selector != null)
                    {
                        if (!FFunctionCall[0].Contains(selector.FunctionCall)) FFunctionCall[0].Add(selector.FunctionCall);
                        foreach (string variable in selector.Variables) if (!FVariables[0].Contains(variable)) FVariables[0].Add(variable);

                        foreach (string funcdef in selector.FunctionDefinitions) if (!FFunctionDefinition[0].Contains(funcdef)) FFunctionDefinition[0].Add(funcdef);
                        foreach (string cBuffVar in selector.ConstantBufferVariables) if (!FConstantBufferEntry[0].Contains(cBuffVar)) FConstantBufferEntry[0].Add(cBuffVar);

                        foreach (IDX11RenderSemantic rsIn in selector.GetRenderSemantics())
                        {
                            bool canAdd = true;
                            foreach (IDX11RenderSemantic rs in FOutSemantics[0])
                            {
                                if (rs.Semantic == rsIn.Semantic) canAdd = false;
                            }
                            if (canAdd) FOutSemantics[0].Add(rsIn);
                        }
                    }
                }
            }
            else // ouput all selector data with correct bin sizes
            {
                FVariables.SliceCount = FFunctionCall.SliceCount = FFunctionDefinition.SliceCount = FConstantBufferEntry.SliceCount = FOutSemantics.SliceCount = SpreadMax;

                for (int i = 0; i < SpreadMax; i++)
                {

                    FFunctionCall[i].SliceCount = 0;
                    FVariables[i].SliceCount = 0;
                    FFunctionDefinition[i].SliceCount = 0;
                    FConstantBufferEntry[i].SliceCount = 0;
                    FOutSemantics[i].SliceCount = 0;

                    Selector selector = FInSelector[i];
                    if (selector != null)
                    {
                        FFunctionCall[i].Add(selector.FunctionCall);
                        FVariables[i].AssignFrom(selector.Variables);
                        FFunctionDefinition[i].AssignFrom(selector.FunctionDefinitions);
                        FConstantBufferEntry[i].AssignFrom(selector.ConstantBufferVariables);
                        FOutSemantics[i].AssignFrom(selector.GetRenderSemantics());
                    }

                }
            }
            
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Cons", Category = "DX11.Particles.Selection", Help = "Concatenates all input spreads to one output spread", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectorCons : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        // A spread which contains our inputs
        public Spread<IIOContainer<ISpread<Selector>>> FInputs = new Spread<IIOContainer<ISpread<Selector>>>();

        [Output("Output")]
        public ISpread<Selector> FOutSelector;

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
            FOutSelector.SliceCount = 0;
            for (int i = 0; i < FInputs.Count(); i++)
            {
                for (int j = 0; j < FInputs[i].IOObject.SliceCount; j++)
                {
                    Selector selectorIn = FInputs[i].IOObject[j];
                    if (selectorIn != null)
                    {
                        FOutSelector.Add(selectorIn);
                    }
                }
                
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "AND", Category = "DX11.Particles.Selection", Help = "Joins data of several selectors and generates a new AND-connected function call.", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectorAND : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        // A spread which contains our inputs
        public Spread<IIOContainer<ISpread<Selector>>> FInputs = new Spread<IIOContainer<ISpread<Selector>>>();

        [Output("Output")]
        public ISpread<Selector> FOutSelector;

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
            FOutSelector.SliceCount = SpreadMax;

            for (int i = 0; i < SpreadMax; i++)
            {
                Selector selector = new Selector();
                string functionCall = "(";
                for (int j = 0; j < FInputs.Count(); j++)
                {
                    Selector selectorIn = FInputs[j].IOObject[i];
                    if (selectorIn != null)
                    {
                        if (j != 0) functionCall += " && ";
                        functionCall += selectorIn.FunctionCall;
                        selector.AddVariables(selectorIn.Variables);
                        selector.AddFunctionDefinitions(selectorIn.FunctionDefinitions);
                        selector.AddConstantBufferEntries(selectorIn.ConstantBufferVariables);
                        selector.AddRenderSemantics(selectorIn.GetRenderSemantics());
                    }
                }

                functionCall += " )";

                List<string> functionCalls = new List<string>();
                functionCalls.Add(functionCall);

                selector.SetFunctionCall(functionCall);

                FOutSelector[i] = selector;
            }
            
        }
    }
    
    #region PluginInfo
    [PluginInfo(Name = "AND", Category = "DX11.Particles.Selection", Version="Spectral", Help = "Joins data of a selector spread and generates a new AND-connected function call.", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectorSpectralAND : IPluginEvaluate
    {
        #region fields & pins

        [Input("Input")]
        public ISpread<ISpread<Selector>> FInSelector;
        
        [Output("Output")]
        public ISpread<Selector> FOutSelector;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (!FInSelector.IsChanged  ) return;

            FOutSelector.SliceCount = FInSelector.SliceCount;
            
            for (int i = 0; i < FInSelector.SliceCount; i++)
            {
                Selector selector = new Selector();
                string functionCall = "(";

                for (int j = 0; j < FInSelector[i].SliceCount; j++)
                {
                    
                    Selector selectorIn = FInSelector[i][j];
                    if (selectorIn != null)
                    {
                        if (j != 0) functionCall += " && ";

                        functionCall += selectorIn.FunctionCall;
                        selector.AddVariables(selectorIn.Variables);
                        selector.AddFunctionDefinitions(selectorIn.FunctionDefinitions);
                        selector.AddConstantBufferEntries(selectorIn.ConstantBufferVariables);
                        selector.AddRenderSemantics(selectorIn.GetRenderSemantics());
                    }
                    
                }

                functionCall += " )";
                List<string> functionCalls = new List<string>();
                functionCalls.Add(functionCall);
                selector.SetFunctionCall(functionCall);

                FOutSelector[i] = selector;
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "OR", Category = "DX11.Particles.Selection", Help = "Joins data of several selectors and generates a new OR-connected function call.", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectorOR : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        // A spread which contains our inputs
        public Spread<IIOContainer<ISpread<Selector>>> FInputs = new Spread<IIOContainer<ISpread<Selector>>>();

        [Output("Output")]
        public ISpread<Selector> FOutSelector;

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
            FOutSelector.SliceCount = SpreadMax;

            for (int i = 0; i < SpreadMax; i++)
            {
                Selector selector = new Selector();
                string functionCall = "(";
                for (int j = 0; j < FInputs.Count(); j++)
                {

                    Selector selectorIn = FInputs[j].IOObject[i];
                    
                    

                    if (selectorIn != null)
                    {
                        if (j != 0) functionCall += " || ";
                        functionCall += selectorIn.FunctionCall;
                        selector.AddVariables(selectorIn.Variables);
                        selector.AddFunctionDefinitions(selectorIn.FunctionDefinitions);
                        selector.AddConstantBufferEntries(selectorIn.ConstantBufferVariables);
                        selector.AddRenderSemantics(selectorIn.GetRenderSemantics());
                    }

                }

                functionCall += " )";
                List<string> functionCalls = new List<string>();
                functionCalls.Add(functionCall);
                selector.SetFunctionCall(functionCall);

                FOutSelector[i] = selector;
            }

        }
    }

    #region PluginInfo
    [PluginInfo(Name = "OR", Category = "DX11.Particles.Selection", Version = "Spectral", Help = "Joins data of a selector spread and generates a new OR-connected function call.", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectorSpectralOR : IPluginEvaluate
    {
        #region fields & pins

        [Input("Input")]
        public ISpread<ISpread<Selector>> FInSelector;

        [Output("Output")]
        public ISpread<Selector> FOutSelector;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (!FInSelector.IsChanged) return;

            FOutSelector.SliceCount = FInSelector.SliceCount;

            for (int i = 0; i < FInSelector.SliceCount; i++)
            {
                Selector selector = new Selector();
                string functionCall = "(";

                for (int j = 0; j < FInSelector[i].SliceCount; j++)
                {

                    Selector selectorIn = FInSelector[i][j];
                    if (selectorIn != null)
                    {
                        if (j != 0) functionCall += " || ";

                        functionCall += selectorIn.FunctionCall;
                        selector.AddVariables(selectorIn.Variables);
                        selector.AddFunctionDefinitions(selectorIn.FunctionDefinitions);
                        selector.AddConstantBufferEntries(selectorIn.ConstantBufferVariables);
                        selector.AddRenderSemantics(selectorIn.GetRenderSemantics());
                    }

                }

                functionCall += " )";
                List<string> functionCalls = new List<string>();
                functionCalls.Add(functionCall);
                selector.SetFunctionCall(functionCall);

                FOutSelector[i] = selector;
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "NOT", Category = "DX11.Particles.Selection", Help = "Outputs the selectors with negated functioncalls.", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectionLogicNOT : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input")]
        public IDiffSpread<Selector> FInSelector;

        [Output("Output")]
        public ISpread<Selector> FOutSelector;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (!FInSelector.IsChanged) return;

            FOutSelector.SliceCount = SpreadMax;

            for (int i = 0; i < SpreadMax; i++)
            {
                Selector selectorIn = FInSelector[i];
                Selector selector = new Selector();

                selector.FunctionCall = "!( " + selectorIn.FunctionCall + ")";
                selector.Variables = selectorIn.Variables;
                selector.FunctionDefinitions = selectorIn.FunctionDefinitions;
                selector.ConstantBufferVariables = selectorIn.ConstantBufferVariables;
                selector.RenderSemantics = selectorIn.RenderSemantics;

                FOutSelector[i] = selector;
            }
            
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Switch", Category = "DX11.Particles.Selection", Help = "Switches between multiple Selector inputs", Author = "tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectionSwitch : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins

        [Input("Switch", IsSingle = true, DefaultValue = 0)]
        public ISpread<int> FSwitch;

        public Spread<IIOContainer<ISpread<Selector>>> FInputs = new Spread<IIOContainer<ISpread<Selector>>>();

        [Output("Output")]
        public ISpread<Selector> FOutSelector;

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
            FOutSelector.SliceCount = 0;

            for (int i = 0; i < FInputs[index].IOObject.SliceCount; i++)
            {
                Selector selectorIn = FInputs[index].IOObject[i];
                if (selectorIn != null)
                {
                    FOutSelector.Add(selectorIn);
                }

            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Zip", Category = "DX11.Particles.Selection", Help = "Zip Selectors", Tags = "", Author = "tmp")]
    #endregion PluginInfo
    public class SelectorZip : Zip<IInStream<Selector>>
    {
    }

    #region PluginInfo
    [PluginInfo(Name = "Unzip", Category = "DX11.Particles.Selection", Help = "Unzip Selectors", Tags = "", Author = "tmp")]
    #endregion PluginInfo
    public class SelectorUnzip : Unzip<IInStream<Selector>>
    {
    }

    #region PluginInfo
    [PluginInfo(Name = "Select", Category = "DX11.Particles.Selection", Help = "select the slices which form the new spread.", Tags = "", Author = "tmp")]
    #endregion PluginInfo
    public class SelectorSelect : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input", CheckIfChanged = true)]
        ISpread<Selector> FInput;

        [Input("Select", DefaultValue = 1, MinValue = 0)]
        ISpread<int> FSelect;

        [Output("Output", AutoFlush = false)]
        ISpread<Selector> FOutput;

        [Output("Former Slice", AutoFlush = false)]
        ISpread<int> FFormer;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutput.SliceCount = 0;
            FFormer.SliceCount = 0;
            if (FInput.IsChanged)
            {
                for (int i = 0; i < SpreadMax; i++)
                {
                    Selector output = FInput[i];

                    for (int j = 0; j < FSelect[i]; j++)
                    {
                        FOutput.Add(output);
                        FFormer.Add(i % FInput.SliceCount);
                    }
                }

                FOutput.Flush();
                FFormer.Flush();
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Select", Category = "DX11.Particles.Selection", Version = "Bin", Help = "Select the slices which form the new spread.", Tags = "", Author = "tmp")]
    #endregion PluginInfo
    public class SelectorSelectBin : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input", CheckIfChanged = true)]
        ISpread<ISpread<Selector>> FInput;

        [Input("Select", DefaultValue = 1, MinValue = 0)]
        ISpread<int> FSelect;

        [Output("Output", AutoFlush = false)]
        ISpread<ISpread<Selector>> FOutput;

        [Output("Former Slice", AutoFlush = false)]
        ISpread<int> FFormer;
        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            FOutput.SliceCount = FInput.SliceCount;
            FFormer.SliceCount = 0;
            if (FInput.IsChanged)
            {
                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    FOutput[i].SliceCount = 0;
                    for (int j = 0; j < FSelect[i]; j++)
                    {
                        FOutput[i].AddRange(FInput[i]);
                        FFormer.Add(i);
                    }
                }
                FOutput.Flush();
                FFormer.Flush();
            }
        }
    }


    #region PluginInfo
    [PluginInfo(Name = "GetSlice", Category = "DX11.Particles.Selection", Help = "gets all slices specified in the index input from the input spread", Author = "tmp")]
    #endregion PluginInfo
    public class SelectorGetSlice : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input", BinSize = 1)]
        IDiffSpread<ISpread<Selector>> FInput;

        [Input("Index", DefaultValue = 0)]
        ISpread<int> FIndex;

        [Output("Output", AutoFlush = false, BinVisibility = PinVisibility.OnlyInspector)]
        ISpread<ISpread<Selector>> FOutput;
        #endregion fields & pins

        public void Evaluate(int spreadMax)
        {
            spreadMax = FIndex.SliceCount;
            FOutput.SliceCount = spreadMax;

            for (int i = 0; i < spreadMax; i++)
            {
                FOutput[i] = FInput[FIndex[i]];
            }
            FOutput.Flush();
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "DeleteSlice", Category = "DX11.Particles.Selection", Help = "Deletes a slice from a Spread at the given index.", Tags = "", Author = "tmp")]
    #endregion PluginInfo
    public class SelectorDeleteSlice : DeleteSlice<IInStream<Selector>> { }

    #region PluginInfo
    [PluginInfo(Name = "SetSlice", Category = "DX11.Particles.Selection", Help = "Replace individual slices of the spread with the given input", Tags = "", Author = "tmp")]
    #endregion PluginInfo
    public class SelectorSetSlice : SetSlice<IInStream<Selector>> { }

}
