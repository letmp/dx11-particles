using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

using SlimDX;

using FeralTic.DX11.Resources;
using FeralTic.DX11;
using VVVV.DX11;
using System.IO;
using VVVV.Core.Logging;

namespace DX11.Extensions
{
    [PluginInfo(Name = "ReadBack", Category = "DX11.Buffer", Version = "RawStream", Author = "tmp")]
    public class ReadBackBufferRawNode : IPluginEvaluate, IDX11ResourceDataRetriever
    {
        [Input("Input", AutoValidate = false)]
        protected Pin<DX11Resource<IDX11StructuredBuffer>> FInput;

        [Input("Enabled", DefaultValue = 1, IsSingle = true)]
        protected ISpread<bool> FInEnabled;

        [Output("Output")]
        protected ISpread<Stream> FOutput;

        [Import()]
        protected IPluginHost FHost;

        [Import()]
        public ILogger FLogger;

        private DX11StagingStructuredBuffer staging;
        private int currentStride = 0;

        public DX11RenderContext AssignedContext
        {
            get;
            set;
        }

        public event DX11RenderRequestDelegate RenderRequest;
        
        #region IPluginEvaluate Members
        public void Evaluate(int SpreadMax)
        {
            if (this.FInput.IsConnected && this.FInEnabled[0])
            {
                this.FInput.Sync();

                if (this.RenderRequest != null) { this.RenderRequest(this, this.FHost); }

                if (this.AssignedContext == null)
                {
                    this.FOutput.SliceCount = 0;
                    return;
                }

                IDX11StructuredBuffer b = this.FInput[0][this.AssignedContext];
                if (b != null)
                {
                    
                    if (this.staging != null && this.staging.ElementCount != b.ElementCount) { this.staging.Dispose(); this.staging = null; }
                    if (this.staging != null && currentStride != b.Stride) {
                        this.staging.Dispose(); this.staging = null;
                        currentStride = b.Stride;
                    }

                    if (this.staging == null)
                    {
                        staging = new DX11StagingStructuredBuffer(this.AssignedContext.Device, b.ElementCount, b.Stride);
                    }

                    this.AssignedContext.CurrentDeviceContext.CopyResource(b.Buffer, staging.Buffer);

                    //this.FOutput.SliceCount = b.ElementCount;
                    this.FOutput.SliceCount = 1;

                    DataStream ds = staging.MapForRead(this.AssignedContext.CurrentDeviceContext);
                    try
                    {
                        MemoryComStream outstream = new MemoryComStream();
                        ds.Position = 0;
                        ds.CopyTo(outstream);
                        FOutput[0] = outstream;

                        this.FOutput.Flush(true);
                    }
                    catch (Exception ex)
                    {
                        FHost.Log(TLogType.Error, "Error inreadback node: " + ex.Message);
                    }
                    finally
                    {
                        staging.UnMap(this.AssignedContext.CurrentDeviceContext);
                    }

                }
                else
                {
                    this.FOutput.SliceCount = 0;
                }
            }
            else
            {
                this.FOutput.SliceCount = 0;
            }
        }
        #endregion
    }
}
