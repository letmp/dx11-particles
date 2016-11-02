#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "SelectorData", Category = "DX11.Particles.Selectors", Help = "Creates unique functioncalls, constantbuffer entries and semantics.", Author="tmp")]
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
		
		[Input("Semantic Count", IsSingle=true)]
		public IDiffSpread<int> FInSemanticCount;
		
		[Output("FunctionCall")]
		public ISpread<string> FOutFunctionCall;
		
		[Output("ConstantBuffer Entry")]
		public ISpread<string> FOutBufferEntry;
		
		[Output("Semantic")]
		public ISpread<string> FOutSemantic;

		[Import]
        protected IPluginHost2 PluginHost;
		
		#endregion fields & pins

		public void Evaluate(int SpreadMax)
		{
			FOutFunctionCall.SliceCount = FOutBufferEntry.SliceCount = FOutSemantic.SliceCount = FInSemanticCount[0];
			
			if (! (FInFunctionName.IsChanged || FInFunctionArgument.IsChanged || FInVariableType.IsChanged || FInVariableName.IsChanged || FInSemantic.IsChanged || FInSemanticCount.IsChanged )) return;
			
			
			int nodeId = PluginHost.GetID();
			
			for (int i = 0; i < FInSemanticCount[0]; i++){
				
				string uniqueSuffix = "_" + nodeId + "_" + i;
				string uniqueVariableName = FInVariableName[i] + uniqueSuffix;
				string uniqueSemantic = FInSemantic[i] + uniqueSuffix;
				
				FOutFunctionCall[i] = FInFunctionName[i] + "(" + String.Join(", ", FInFunctionArgument) + "," + uniqueVariableName + ")" ;
				FOutBufferEntry[i] = FInVariableType[i] + " " + uniqueVariableName + " : " + uniqueSemantic + ";";
				FOutSemantic[i] = uniqueSemantic;
			}
		}
	}
}
