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
        private ISession _session;
        private System.Threading.SynchronizationContext _sc;
        private MiddlewareAppLogger _logger;
        public Form1()
        {
            _sc = System.Threading.SynchronizationContext.Current;
            InitializeComponent();
            _logger = new MiddlewareAppLogger(this);
        }

        private async void _ConnectToServer(string username, string password)
        {
            var session = await _manager.CreateSession(txtServerName.Text, username, password, _logger);
            if (session != null)
            {
                //connected and authenticated ok, now register callback handler
                _session = session;
                _manager.RegisterMessageCallbackFunction((s, m) =>
                {
                    _sc.Post(obj =>
                    {
                        var message = (Middleware.Message)obj;
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

                    }, m);

                });

                btnBroadcast.Enabled = true;
                btnRegisterListener.Enabled = true;
                btnSendRequest.Enabled = true;
                btnSendResponse.Enabled = true;
                btnSubscribe.Enabled = true;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            var frmConnect = new FormConnect();
            if (frmConnect.ShowDialog() == DialogResult.OK)
            {
                _ConnectToServer(frmConnect.GetUser(), frmConnect.GetPassword());
            }
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
