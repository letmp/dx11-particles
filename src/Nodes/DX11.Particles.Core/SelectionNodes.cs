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
        public List<string> Functions = new List<string>();
        
        public List<string> CustomSemanticEntries = new List<string>();
        public List<IDX11RenderSemantic> CustomSemantics = new List<IDX11RenderSemantic>();

        public List<string> ResourceSemanticEntries = new List<string>();
        public List<DX11Resource<IDX11RenderSemantic>> ResourceSemantics = new List<DX11Resource<IDX11RenderSemantic>>();
        
        public Selector() { }

        public void SetAll( string functionCall, IEnumerable<string> variables,
                            IEnumerable<string> functionDefinitions,
                            IEnumerable<string> customSemanticEntries, IEnumerable<IDX11RenderSemantic> customSemantics,
                            IEnumerable<string> resourceSemanticEntries, IEnumerable<DX11Resource<IDX11RenderSemantic>> resourceSemantics
                    )
        {
            FunctionCall = functionCall;
            Variables = variables.ToList();
            Functions = functionDefinitions.ToList();

            //CustomSemanticEntries = customSemanticEntries.Where(x => x != "").ToList();
            //CustomSemantics.AddRange(customSemantics);
            var cseList = customSemanticEntries.ToList();
            var csList = customSemantics.ToList();
            for (int i = 0; i < cseList.Count(); i++)
            {
                string cse = cseList[i];
                if (cse != "")
                {
                    CustomSemanticEntries.Add(cse);
                    CustomSemantics.Add(csList[i]);
                }

            }

            //ResourceSemanticEntries = resourceSemanticEntries.Where(x => x != " ").ToList();
            //ResourceSemantics.AddRange(resourceSemantics);
            var rseList = resourceSemanticEntries.ToList();
            var rsList = resourceSemantics.ToList();
            for (int i = 0; i < rseList.Count(); i++)
            {
                string rse = rseList[i];
                if (rse != "")
                {
                    ResourceSemanticEntries.Add(rse);
                    ResourceSemantics.Add(rsList[i]);
                }

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
        
        public void AddFunction(List<string> function)
        {
            foreach (string f in function)
            {
                if (!Functions.Contains(f)) Functions.Add(f);
            }
        }
        
        public void AddCustomSemanticEntries(List<string> customSemanticEntries)
        {
            foreach (string cse in customSemanticEntries)
            {
                if (!CustomSemanticEntries.Contains(cse) && cse != "") CustomSemanticEntries.Add(cse);
            }
        }

        public void AddCustomSemantics(List<IDX11RenderSemantic> customSemantics)
        {
            foreach (IDX11RenderSemantic cs in customSemantics)
            {
                if (!CustomSemantics.Contains(cs)) CustomSemantics.Add(cs);
            }
        }

        public List<IDX11RenderSemantic> GetCustomSemantics()
        {
            return CustomSemantics;
        }

        public void AddResourceSemanticEntries(List<string> resourceSemanticEntries)
        {
            foreach (string rse in resourceSemanticEntries)
            {
                if (!ResourceSemanticEntries.Contains(rse) && rse != "") ResourceSemanticEntries.Add(rse);
            }
        }

        public void AddResourceSemantics(List<DX11Resource<IDX11RenderSemantic>> resourceSemantics)
        {
            foreach (DX11Resource<IDX11RenderSemantic> rs in resourceSemantics)
            {
                if (!ResourceSemantics.Contains(rs)) ResourceSemantics.Add(rs);
            }
        }

        public List<DX11Resource<IDX11RenderSemantic>> GetResourceSemantics()
        {
            return ResourceSemantics;
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

        [Input("Custom Semantic Definition")]
        public IDiffSpread<string> FInCustomSemanticDefinition;

        [Input("Custom Semantic Count", MinValue = 0)]
        public IDiffSpread<int> FInCustomSemanticCount;

        [Input("Resource Semantic Definition")]
        public IDiffSpread<string> FInResourceSemanticDefinition;

        [Input("Resource Semantic Count", MinValue = 0)]
        public IDiffSpread<int> FInResourceSemanticCount;
        
        [Output("FunctionCall")]
        public ISpread<string> FOutFunctionCall;

        [Output("Custom Semantic Entry")]
        public ISpread<ISpread<string>> FOutCustomSemanticEntry;

        [Output("Custom Semantic")]
        public ISpread<ISpread<string>> FOutCustomSemantic;

        [Output("Resource Semantic Entry")]
        public ISpread<ISpread<string>> FOutResourceSemanticEntry;

        [Output("Resource Semantic")]
        public ISpread<ISpread<string>> FOutResourceSemantic;


        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            
            if (!(FInFunctionName.IsChanged || FInFunctionArgument.IsChanged ||
                FInCustomSemanticDefinition.IsChanged || FInCustomSemanticCount.IsChanged || 
                FInResourceSemanticDefinition.IsChanged || FInResourceSemanticCount.IsChanged)) return;

            string uniqueSuffix = "_" + this.GetHashCode();
            int functionCallCount = 0;

            FOutCustomSemanticEntry.SliceCount = FOutCustomSemantic.SliceCount = FInCustomSemanticDefinition.SliceCount;
            for (int i = 0; i < FInCustomSemanticDefinition.SliceCount; i++)
            {
                FOutCustomSemanticEntry[i].SliceCount = FOutCustomSemantic[i].SliceCount = 0;

                for (int j = 0; j < FInCustomSemanticCount[i]; j++)
                {
                    string[] csd = FInCustomSemanticDefinition[i].Replace(";","").Split(':');
                    string variableType = csd[0].Split(' ')[0].Trim();
                    string variableName = csd[0].Split(' ')[1].Trim() + uniqueSuffix + "_" + j;
                    string semanticName = csd[1].Trim() + uniqueSuffix + "_" + j;

                    FOutCustomSemanticEntry[i].Add(variableType + " " + variableName + " : " + semanticName + ";");
                    FOutCustomSemantic[i].Add(semanticName);
                    if (j > functionCallCount) functionCallCount = j;
                }
            }

            FOutResourceSemanticEntry.SliceCount = FOutResourceSemantic.SliceCount = FInResourceSemanticDefinition.SliceCount;
            for (int i = 0; i < FInResourceSemanticDefinition.SliceCount; i++)
            {
                FOutResourceSemanticEntry[i].SliceCount = FOutResourceSemantic[i].SliceCount = 0;

                for (int j = 0; j < FInResourceSemanticCount[i]; j++)
                {
                    string[] csd = FInResourceSemanticDefinition[i].Replace(";", "").Split(':');
                    string variableType = csd[0].Split(' ')[0].Trim();
                    string variableName = csd[0].Split(' ')[1].Trim() + uniqueSuffix + "_" + j;
                    string semanticName = csd[1].Trim() + uniqueSuffix + "_" + j;

                    FOutResourceSemanticEntry[i].Add(variableType + " " + variableName + " : " + semanticName + ";");
                    FOutResourceSemantic[i].Add(semanticName);
                    if (j > functionCallCount) functionCallCount = j;
                }
            }


            FOutFunctionCall.SliceCount = 0;
            int variableCount = Math.Max(FInCustomSemanticDefinition.SliceCount, FInResourceSemanticDefinition.SliceCount);

            for (int i = 0; i <= functionCallCount; i++)
            {

                List<string> uniqueCSVariableNames = new List<string>();
                List<string> uniqueRSVariableNames = new List<string>();

                for (int j = 0; j < variableCount; j++)
                {
                    if (FOutCustomSemanticEntry.SliceCount > 0 && FOutCustomSemanticEntry[j].SliceCount != 0)
                        uniqueCSVariableNames.Add(FOutCustomSemanticEntry[j][i % FOutCustomSemanticEntry[j].SliceCount].Split(' ')[1]);

                    if (FOutResourceSemanticEntry.SliceCount > 0 && FOutResourceSemanticEntry[j].SliceCount != 0)
                        uniqueRSVariableNames.Add(FOutResourceSemanticEntry[j][i % FOutResourceSemanticEntry[j].SliceCount].Split(' ')[1]);                    
                }

                FOutFunctionCall.Add(FInFunctionName[i] + "(" + String.Join(", ", FInFunctionArgument) + "," + String.Join(", ", uniqueCSVariableNames.Concat(uniqueRSVariableNames)) + ")");
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "Join",Category = "DX11.Particles.Selection", Help = "Joins data for a selector", Author="tmp", Tags = "")]
    #endregion PluginInfo
    public class SelectorJoin : IPluginEvaluate
    {
        #region fields & pins
        [Input("FunctionCall")]
        public IDiffSpread<string> FFunctionCall;

        [Input("Variables")]
        public IDiffSpread<string> FVariables;
        
        [Input("Function")]
        public IDiffSpread<string> FFunction;

        [Input("Custom Semantic Entry")]
        public IDiffSpread<string> FCSEntry;

        [Input("Custom Semantic")]
        protected Pin<IDX11RenderSemantic> FInCustomSemantic;

        [Input("Resource Semantic Entry")]
        public IDiffSpread<string> FRSEntry;

        [Input("Resource Semantic")]
        protected Pin<DX11Resource<IDX11RenderSemantic>> FInResourceSemantic;

        [Output("Output")]
        public ISpread<Selector> FOutSelector;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (! ( FVariables.IsChanged || FFunctionCall.IsChanged || FFunction.IsChanged ||
                FCSEntry.IsChanged || FInCustomSemantic.IsChanged ||
                FRSEntry.IsChanged || FInResourceSemantic.IsChanged )) return;

            FOutSelector.SliceCount = FFunctionCall.SliceCount;
            for (int i = 0; i < FFunctionCall.SliceCount; i++)
            {
                Selector selector = new Selector();
                selector.SetAll(FFunctionCall[i], FVariables, FFunction, FCSEntry, FInCustomSemantic, FRSEntry, FInResourceSemantic);
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

        [Output("FunctionCall")]
        public ISpread<ISpread<string>> FFunctionCall;

        [Output("Variables")]
        public ISpread<ISpread<string>> FVariables;
        
        [Output("Functions")]
        public ISpread<ISpread<string>> FFunctions;

        [Output("Custom Semantic Entry")]
        public ISpread<ISpread<string>> FOutCSEntry;

        [Output("Custom Semantics")]
        public ISpread<ISpread<IDX11RenderSemantic>> FOutCustomSemantic;

        [Output("Resource Semantic Entry")]
        public ISpread<ISpread<string>> FOutRSEntry;

        [Output("Resource Semantics")]
        public ISpread<ISpread<DX11Resource<IDX11RenderSemantic>>> FOutResourceSemantic;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (!FInSelector.IsChanged) return;

            if (FPreventDoubles[0]) // output only unique data
            {
                FVariables.SliceCount = FFunctionCall.SliceCount = FFunctions.SliceCount =
                FOutCSEntry.SliceCount = FOutCustomSemantic.SliceCount =
                FOutRSEntry.SliceCount = FOutResourceSemantic.SliceCount = 1;

                FFunctionCall[0].SliceCount = FVariables[0].SliceCount  = FFunctions[0].SliceCount =
                FOutCSEntry[0].SliceCount = FOutCustomSemantic[0].SliceCount =
                FOutRSEntry[0].SliceCount = FOutResourceSemantic[0].SliceCount = 0;

                for (int i = 0; i < SpreadMax; i++)
                {
                    Selector selector = FInSelector[i];
                    if (selector != null)
                    {
                        // functioncalls
                        if (!FFunctionCall[0].Contains(selector.FunctionCall)) FFunctionCall[0].Add(selector.FunctionCall);
                        // variables
                        foreach (string variable in selector.Variables) if (!FVariables[0].Contains(variable)) FVariables[0].Add(variable);
                        // functions
                        foreach (string funcdef in selector.Functions) if (!FFunctions[0].Contains(funcdef)) FFunctions[0].Add(funcdef);
                        // custom semantic entries
                        foreach (string cs in selector.CustomSemanticEntries) if (!FOutCSEntry[0].Contains(cs)) FOutCSEntry[0].Add(cs);
                        // custom semantics
                        foreach (IDX11RenderSemantic csIn in selector.GetCustomSemantics())
                        {
                            bool canAdd = true;
                            foreach (IDX11RenderSemantic cs in FOutCustomSemantic[0])
                            {
                                if (cs.Semantic == csIn.Semantic) canAdd = false;
                            }
                            if (canAdd) FOutCustomSemantic[0].Add(csIn);
                        }

                        // resource semantic entries
                        foreach (string rs in selector.ResourceSemanticEntries) if (!FOutRSEntry[0].Contains(rs)) FOutRSEntry[0].Add(rs);
                        // resource semantics
                        foreach (DX11Resource<IDX11RenderSemantic> rsIn in selector.GetResourceSemantics())
                        {
                            bool canAdd = true;
                            foreach (DX11Resource<IDX11RenderSemantic> rs in FOutResourceSemantic[0])
                            {
                                if (rs.Equals(rsIn)) canAdd = false;
                            }
                            if (canAdd) FOutResourceSemantic[0].Add(rsIn);
                        }
                    }
                }
            }
            else // ouput all selector data with correct bin sizes
            {
                FVariables.SliceCount = FFunctionCall.SliceCount = FFunctions.SliceCount =
                FOutCSEntry.SliceCount = FOutCustomSemantic.SliceCount =
                FOutRSEntry.SliceCount = FOutResourceSemantic.SliceCount = SpreadMax;

                for (int i = 0; i < SpreadMax; i++)
                {

                    FFunctionCall[i].SliceCount = FVariables[i].SliceCount = FFunctions[i].SliceCount = 
                    FOutCSEntry[i].SliceCount = FOutCustomSemantic[i].SliceCount =
                    FOutRSEntry[i].SliceCount = FOutResourceSemantic[i].SliceCount = 0;

                    Selector selector = FInSelector[i];
                    if (selector != null)
                    {
                        FFunctionCall[i].Add(selector.FunctionCall);
                        FVariables[i].AssignFrom(selector.Variables);
                        FFunctions[i].AssignFrom(selector.Functions);
                        FOutCSEntry[i].AssignFrom(selector.CustomSemanticEntries);
                        FOutCustomSemantic[i].AssignFrom(selector.GetCustomSemantics());
                        FOutRSEntry[i].AssignFrom(selector.ResourceSemanticEntries);
                        FOutResourceSemantic[i].AssignFrom(selector.GetResourceSemantics());
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
                        selector.AddFunction(selectorIn.Functions);
                        selector.AddCustomSemanticEntries(selectorIn.CustomSemanticEntries);
                        selector.AddCustomSemantics(selectorIn.GetCustomSemantics());
                        selector.AddResourceSemanticEntries(selectorIn.ResourceSemanticEntries);
                        selector.AddResourceSemantics(selectorIn.GetResourceSemantics());
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
                        selector.AddFunction(selectorIn.Functions);
                        selector.AddCustomSemanticEntries(selectorIn.CustomSemanticEntries);
                        selector.AddCustomSemantics(selectorIn.GetCustomSemantics());
                        selector.AddResourceSemanticEntries(selectorIn.ResourceSemanticEntries);
                        selector.AddResourceSemantics(selectorIn.GetResourceSemantics());
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
                        selector.AddFunction(selectorIn.Functions);
                        selector.AddCustomSemanticEntries(selectorIn.CustomSemanticEntries);
                        selector.AddCustomSemantics(selectorIn.GetCustomSemantics());
                        selector.AddResourceSemanticEntries(selectorIn.ResourceSemanticEntries);
                        selector.AddResourceSemantics(selectorIn.GetResourceSemantics());
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
                        selector.AddFunction(selectorIn.Functions);
                        selector.AddCustomSemanticEntries(selectorIn.CustomSemanticEntries);
                        selector.AddCustomSemantics(selectorIn.GetCustomSemantics());
                        selector.AddResourceSemanticEntries(selectorIn.ResourceSemanticEntries);
                        selector.AddResourceSemantics(selectorIn.GetResourceSemantics());
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
                selector.Functions = selectorIn.Functions;
                selector.CustomSemanticEntries = selectorIn.CustomSemanticEntries;
                selector.CustomSemantics = selectorIn.CustomSemantics;
                selector.ResourceSemanticEntries = selectorIn.ResourceSemanticEntries;
                selector.ResourceSemantics = selectorIn.ResourceSemantics;

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
