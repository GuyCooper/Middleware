using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Middleware;
using MiddlewareNetClient;

namespace MIddlewareTestWindowsApp
{
    public partial class Form1 : Form
    {
        private MiddlewareManager _manager = new MiddlewareManager();
        private MiddlewareManager _authManager = new MiddlewareManager();
        private ISession _session;
        private ISession _authSession;
        private System.Threading.SynchronizationContext _sc;
        private MiddlewareAppLogger _logger;
        private string authRequestID;

        public Form1()
        {
            _sc = System.Threading.SynchronizationContext.Current;
            InitializeComponent();
            _logger = new MiddlewareAppLogger(this);
        }

        private void HandlerServerMessages(ISession session, Middleware.Message message)
        {
            _sc.Post(obj =>
            {
                if (message.Command == HandlerNames.SENDREQUEST)
                {
                    txtReceivedData.Text = message.Payload;
                    txtSourceId.Text = message.SourceId;
                }
                else if (message.Command == HandlerNames.SENDMESSAGE)
                {
                    txtReceivedData.Text = message.Payload;
                }
                else if (message.Command == HandlerNames.PUBLISHMESSAGE)
                {
                    txtReceivedPublishMessage.Text = message.Payload;
                }

            }, null);
        }

        private async void promptAndConnect( string url, MiddlewareManager manager, HandleData callback, Action<ISession> onConnected)
        {
            var frmConnect = new FormConnect();
            if (frmConnect.ShowDialog() == DialogResult.OK)
            {
                var session = await manager.CreateSession(url, frmConnect.GetUser(), frmConnect.GetPassword(), _logger); 
                if(session != null)
                {
                    manager.RegisterMessageCallbackFunction(callback);
                    onConnected(session);
                }
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            promptAndConnect(
                txtServerName.Text,
               _manager,
               HandlerServerMessages,
               (session) =>
               {
                   _session = session;
                   btnBroadcast.Enabled = true;
                   btnRegisterListener.Enabled = true;
                   btnSendRequest.Enabled = true;
                   btnSendResponse.Enabled = true;
                   btnSubscribe.Enabled = true;
               });
        }

        private void _LogResponseMessage(string command, SendDataResponse response)
        {
            _sc.Post((obj) =>
           {
               if (response.Success == true)
               {
                   LogMessage(string.Format("{0} succeded", command));
               }
               else
               {
                   LogMessage(string.Format("{0} failed. error: {1}", command, response.Payload));
               }
           }, null);
        }

        private void btnSendRequest_Click(object sender, EventArgs e)
        {
            _manager.SendRequest(_session, txtMsgChannelName.Text, txtSendRequest.Text).ContinueWith(t =>
           {
               _LogResponseMessage("Send Request", t.Result);
           });
        }

        private void btnRegisterListener_Click(object sender, EventArgs e)
        {
            _manager.AddChannelListener(_session, txtMsgChannelName.Text).ContinueWith(t =>
           {
               _LogResponseMessage("Register Listener", t.Result);
           });
        }

        private void btnSendResponse_Click(object sender, EventArgs e)
        {
            _manager.SendMessageToChannel(_session, txtMsgChannelName.Text, txtSendResponse.Text, txtSourceId.Text);
        }

        private void btnSubscribe_Click(object sender, EventArgs e)
        {
            _manager.SubscribeToChannel(_session, txtBcastChannelName.Text).ContinueWith(t =>
           {
               _LogResponseMessage("Subscribe to channel", t.Result);
           });
        }

        private void btnBroadcast_Click(object sender, EventArgs e)
        {
            _manager.PublishMessage(_session, txtBcastChannelName.Text, txtPublishMessage.Text);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(_session != null)
            {
                var dispose = _session as IDisposable;
                if(dispose != null)
                {
                    dispose.Dispose();
                }
                _session = null;
            }
        }

        public void LogMessage(string message)
        {
            _sc.Post((msg) =>
           {
               lstMessages.Items.Add(msg);
           }
            , message);
        }

        private void HandleAuthRequests(ISession session, Middleware.Message message)
        {
            if(message.Command == HandlerNames.LOGIN)
            {
                _sc.Post((obj) =>
               {
                   authRequestID = message.RequestId;
                   lstTextAuthRequests.Items.Add(message.Payload);
                   btnAllow.Enabled = true;
                   btnDeny.Enabled = true;
               }, null);
            }
        }

        private void RegisterAsAuthHandler()
        {
            var response = _authManager.RegisterAuthHandler(_authSession, "Test Auth");
            response.ContinueWith(t =>
            {
                if (t.Result.Success == true)
                {
                    _sc.Post((obj) =>
                    {
                        btnRegisterAuth.Enabled = false;
                        lstTextAuthRequests.Items.Add("auth handler registered ok");

                    }, null);
                }
                else
                {
                    _sc.Post((obj) =>
                    {
                        lstTextAuthRequests.Items.Add("auth handler failed registration");

                    }, null);
                }
            });
        }

        private void btnRegisterAuth_Click(object sender, EventArgs e)
        {
            if (_authSession == null)
            {
                promptAndConnect(
                    txtAuthServerURL.Text,
                   _authManager,
                   HandleAuthRequests,
                   (session) =>
                   {
                       _authSession = session;
                       RegisterAsAuthHandler();
                   });
            }
            else
            {
                RegisterAsAuthHandler();
            }
        }

        private void ProcessAuthResult(bool success, string message)
        {
            btnAllow.Enabled = false;
            btnDeny.Enabled = false;
            var result = new AuthResult
            {
                Message = message,
                Success = success
            };
            _authManager.SendAuthenticationResponse(_authSession, authRequestID, result);
        }

        private void btnAllow_Click(object sender, EventArgs e)
        {
            ProcessAuthResult(true, "authentication succeded");
        }

        private void btnDeny_Click(object sender, EventArgs e)
        {
            ProcessAuthResult(false, "authentication failed");
        }
    }

    internal class MiddlewareAppLogger : ILogger
    {
        private Form1 _form;
        public MiddlewareAppLogger(Form1 form)
        {
            _form = form;
        }
        public void LogError(string error)
        {
            _form.LogMessage(string.Format("ERROR. {0}", error));
        }

        public void LogMessage(string message)
        {
            _form.LogMessage(string.Format("MESSAGE. {0}", message));
        }
    }

}
