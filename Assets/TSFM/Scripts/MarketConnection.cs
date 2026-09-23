using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using UnityEngine;
#if !UNITY_WEBGL || UNITY_EDITOR
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
#endif
namespace TSFM {
    public class MarketConnection : MonoBehaviour {
        public event Action<string> Message;
        public event Action Opened;
        public event Action<string> Closed;
        public bool IsOpen {get;private set;}
        readonly ConcurrentQueue<string> inbox=new ConcurrentQueue<string>();
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void TSFM_Connect(string target);
        [DllImport("__Internal")] static extern void TSFM_Send(string json);
        [DllImport("__Internal")] static extern void TSFM_Close();
#else
        ClientWebSocket socket;
        CancellationTokenSource cancel;
        readonly SemaphoreSlim sendLock=new SemaphoreSlim(1,1);
        int generation;
#endif
        public void Connect() {
            IsOpen=false;
            while(inbox.TryDequeue(out _)){}
#if UNITY_WEBGL && !UNITY_EDITOR
            TSFM_Connect(gameObject.name);
#else
            cancel?.Cancel();socket?.Dispose();cancel=new CancellationTokenSource();
            socket=new ClientWebSocket();
            _=ConnectNative(socket,cancel.Token,++generation);
#endif
        }
#if !UNITY_WEBGL || UNITY_EDITOR
        async Task ConnectNative(ClientWebSocket current,CancellationToken token,int epoch) {
            try {
                await current.ConnectAsync(new Uri("ws://127.0.0.1:8080/ws"),token);
                if(epoch!=generation)return;
                inbox.Enqueue("@open");
                var buffer=new byte[8192];
                while(current.State==WebSocketState.Open&&!token.IsCancellationRequested) {
                    using(var stream=new MemoryStream()) {
                        WebSocketReceiveResult result;
                        do {
                            result=await current.ReceiveAsync(new ArraySegment<byte>(buffer),token);
                            if(result.MessageType==WebSocketMessageType.Close) {if(epoch==generation)inbox.Enqueue("@closed");return;}
                            stream.Write(buffer,0,result.Count);
                            if(stream.Length>131072)throw new InvalidOperationException("Server message too large.");
                        } while(!result.EndOfMessage);
                        if(epoch==generation)inbox.Enqueue(Encoding.UTF8.GetString(stream.ToArray()));
                    }
                }
            }catch(OperationCanceledException){}catch(Exception){if(epoch==generation)inbox.Enqueue("@closed");}
        }
        async Task SendNative(string json,ClientWebSocket current,CancellationToken token) {
            try {
                await sendLock.WaitAsync(token);
                try {if(current.State==WebSocketState.Open)await current.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)),WebSocketMessageType.Text,true,token);}
                finally {sendLock.Release();}
            }catch(OperationCanceledException){}catch(Exception){inbox.Enqueue("@closed");}
        }
#endif
        public void Send(Command command) {
            if(!IsOpen)return;
            var json=JsonUtility.ToJson(command);
#if UNITY_WEBGL && !UNITY_EDITOR
            TSFM_Send(json);
#else
            _=SendNative(json,socket,cancel.Token);
#endif
        }
        public void OnSocketOpen(string unused){inbox.Enqueue("@open");}
        public void OnSocketMessage(string json){inbox.Enqueue(json);}
        public void OnSocketClosed(string reason){inbox.Enqueue("@closed");}
        void Update() {
            for(int i=0;i<200&&inbox.TryDequeue(out var json);i++) {
                if(json=="@open"){IsOpen=true;Opened?.Invoke();}
                else if(json=="@closed"){IsOpen=false;Closed?.Invoke("Connection lost. Your saved progress will be restored when you reconnect.");}
                else Message?.Invoke(json);
            }
        }
        void OnDestroy() {
#if UNITY_WEBGL && !UNITY_EDITOR
            TSFM_Close();
#else
            generation++;cancel?.Cancel();socket?.Dispose();cancel?.Dispose();
#endif
        }
    }
}
