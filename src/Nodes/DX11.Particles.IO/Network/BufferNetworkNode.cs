using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VVVV.PluginInterfaces.V2;

namespace DX11.Particles.IO.Network
{
    class BufferContainer
    {
        private string _Identifier;
        private AggregatedStream _MemoryStream;
        private bool _WasUpdated = false;

        System.Timers.Timer timer;
        private bool _IsTimingOut = false;
        private bool _IsReleased = false;
        
        public BufferContainer(string id, AggregatedStream memoryStream) {
            SetIdentifier(id);
            SetData(memoryStream);
        }
        
        public void SetIdentifier(string id)
        {
            _Identifier = id;
        }

        public string GetIdentifier()
        {
            return _Identifier;
        }

        public void SetData(AggregatedStream memoryStream)
        {
            if (_IsTimingOut) StopTimeOut();
            _WasUpdated = true;
            _MemoryStream = memoryStream;
        }

        public AggregatedStream GetData()
        {
            return _MemoryStream;
        }

        public bool GetUpdateStatus()
        {
            return _WasUpdated;
        }

        public void ResetUpdateStatus() { _WasUpdated = false; }

        public bool IsTimingOut()
        {
            return _IsTimingOut;
        }

        public void StartTimeOut(int time)
        {
            if (time > 0)
            {
                timer = new System.Timers.Timer();
                timer.Interval = time;
                timer.Elapsed += Release;
                timer.Enabled = true;

                _IsTimingOut = true;
            }
        }

        public void StopTimeOut()
        {
            timer.Stop();
            timer.Elapsed -= Release;
            _IsTimingOut = false;
        }

        private void Release(object sender, ElapsedEventArgs e)
        {
            _IsReleased = true;
        }

        public bool IsReleased()
        {
            return _IsReleased;
        }
    }


    #region PluginInfo
    [PluginInfo(Name = "Store", Category = "DX11.Particles.Network", Version="Raw", Help = "Stores bytedata. Expects an Identifier as first slice followed by the data to store.", Tags = "")]
    #endregion PluginInfo
    public class StoreRawNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input")]
        public ISpread<Stream> FInput;

        [Input("Bin Size",DefaultValue = 1)]
        public IDiffSpread<int> FBinSize;
        
        [Input("Timeout", MinValue = 0, DefaultValue = 0, IsSingle = true)]
        public IDiffSpread<int> FTimeOut;

        [Output("Output")]
        public ISpread<Stream> FOutput;

        [Output("ID")]
        public ISpread<string> FOutString;

        //when dealing with byte streams (what we call Raw in the GUI) it's always
        //good to have a byte buffer around. we'll use it when copying the data.
        protected byte[] FBuffer;

        Dictionary<string, BufferContainer> Store = new Dictionary<string, BufferContainer>();
        
        #endregion fields & pins
        
        //called when data for any output pin is requested
        public void Evaluate(int spreadMax)
        {

            // reset update status - needed later to identify buffers that got no update and time out
            foreach(BufferContainer bc in Store.Values) bc.ResetUpdateStatus();
            
            if(FInput.SliceCount > 0)
            {
                int IdentifierSlice = 0;
                for (int i = 0; i < FBinSize.SliceCount; i++) // go through all bins (1 bin = 1 client)
                {

                    // Get the first slice of the frame. It holds the identifier string
                    string IdentifierString = "";
                    using (var reader = new StreamReader(FInput[IdentifierSlice], Encoding.Default))
                    {
                        IdentifierString = reader.ReadToEnd();
                    }

                    // The rest of the frame is the raw data we want to store in the buffer
                    List<Stream> streams = new List<Stream>();
                    for (int streamId = 1; streamId < FBinSize[i]; streamId++) // iterate from identifier slice to the end of this frame
                    {
                        streams.Add(FInput[IdentifierSlice + streamId]);
                    }
                    AggregatedStream data = new AggregatedStream(streams); // aggregate the data

                    // Store the data - this also updates the timeout flag
                    if (Store.ContainsKey(IdentifierString)) Store[IdentifierString].SetData(data);
                    else Store.Add(IdentifierString, new BufferContainer(IdentifierString, data));

                    IdentifierSlice += FBinSize[i];
                }
            }
            
            // start timeout for all buffer entries that had no update
            foreach (BufferContainer bc in Store.Values)
            {
                if (bc.GetUpdateStatus() == false && !bc.IsTimingOut()) bc.StartTimeOut(FTimeOut[0]);
            }

            // remove all buffers where timeout elapsed
            var buffersToRemove = Store.Where(kvp => kvp.Value.IsReleased() == true).Select(kvp => kvp.Key).ToArray();
            foreach (string bufferKeys in buffersToRemove)
            {
                Store.Remove(bufferKeys);
            }

            // output stored buffers
            FOutput.SliceCount = Store.Count;
            int slice = 0;
            foreach (BufferContainer bc in Store.Values)
            {
                FOutput[slice] = bc.GetData();
                FOutString[slice] = bc.GetIdentifier();
                slice++;
            }
        }
    }
}
