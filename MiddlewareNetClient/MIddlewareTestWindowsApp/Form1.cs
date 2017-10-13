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

        public Form1()
        {
            _sc = System.Threading.SynchronizationContext.Current;
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            _session = _manager.CreateSession(txtServerName.Text);
            if(_session != null)
            {
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

                _session.StartDispatcher();

                btnBroadcast.Enabled = true;
                btnRegisterListener.Enabled = true;
                btnSendRequest.Enabled = true;
                btnSendResponse.Enabled = true;
                btnSubscribe.Enabled = true;
            }
        }

        private void btnSendRequest_Click(object sender, EventArgs e)
        {
            var prms = new MiddlewareRequestParams(txtMsgChannelName.Text, (s,x) => MessageBox.Show("send success succeded"),
                                                                      (s,x) => MessageBox.Show("send request failed!"));              
            _manager.SendRequest(_session, prms, txtSendRequest.Text);
        }

        private void btnRegisterListener_Click(object sender, EventArgs e)
        {
            var prms = new MiddlewareRequestParams(txtMsgChannelName.Text, (s, x) => MessageBox.Show("register listener succeded"),
                                                                      (s, x) => MessageBox.Show("register listener failed!"));
            _manager.AddChannelListener(_session, prms);
        }

        private void btnSendResponse_Click(object sender, EventArgs e)
        {
            var prms = new MiddlewareRequestParams(txtMsgChannelName.Text, null, null);
            _manager.SendMessageToChannel(_session, prms, txtSendResponse.Text, txtSourceId.Text);
        }

        private void btnSubscribe_Click(object sender, EventArgs e)
        {
            var prms = new MiddlewareRequestParams(txtBcastChannelName.Text, (s, x) => MessageBox.Show("subscribe channel succeded"),
                                                                      (s, x) => MessageBox.Show("subscribe channel failed!"));
            _manager.SubscribeToChannel(_session, prms);
        }

        private void btnBroadcast_Click(object sender, EventArgs e)
        {
            var prms = new MiddlewareRequestParams(txtBcastChannelName.Text, null, null);
            _manager.PublishMessage(_session, prms, txtPublishMessage.Text);
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
    }
}
