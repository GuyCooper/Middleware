namespace MIddlewareTestWindowsApp
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.txtServerName = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnSendResponse = new System.Windows.Forms.Button();
            this.txtSendResponse = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtSourceId = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtReceivedData = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnRegisterListener = new System.Windows.Forms.Button();
            this.btnSendRequest = new System.Windows.Forms.Button();
            this.txtSendRequest = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtMsgChannelName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnBroadcast = new System.Windows.Forms.Button();
            this.txtReceivedPublishMessage = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtPublishMessage = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.btnSubscribe = new System.Windows.Forms.Button();
            this.txtBcastChannelName = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 23);
            this.label1.TabIndex = 5;
            // 
            // txtServerName
            // 
            this.txtServerName.Location = new System.Drawing.Point(212, 35);
            this.txtServerName.Name = "txtServerName";
            this.txtServerName.Size = new System.Drawing.Size(251, 26);
            this.txtServerName.TabIndex = 1;
            this.txtServerName.Text = "ws://localhost:8080";
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(499, 31);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(125, 35);
            this.btnConnect.TabIndex = 2;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnSendResponse);
            this.panel1.Controls.Add(this.txtSendResponse);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.txtSourceId);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.txtReceivedData);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.btnRegisterListener);
            this.panel1.Controls.Add(this.btnSendRequest);
            this.panel1.Controls.Add(this.txtSendRequest);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.txtMsgChannelName);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Location = new System.Drawing.Point(48, 81);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(636, 299);
            this.panel1.TabIndex = 3;
            // 
            // btnSendResponse
            // 
            this.btnSendResponse.Enabled = false;
            this.btnSendResponse.Location = new System.Drawing.Point(476, 234);
            this.btnSendResponse.Name = "btnSendResponse";
            this.btnSendResponse.Size = new System.Drawing.Size(141, 44);
            this.btnSendResponse.TabIndex = 13;
            this.btnSendResponse.Text = "Send Response";
            this.btnSendResponse.UseVisualStyleBackColor = true;
            this.btnSendResponse.Click += new System.EventHandler(this.btnSendResponse_Click);
            // 
            // txtSendResponse
            // 
            this.txtSendResponse.Location = new System.Drawing.Point(143, 243);
            this.txtSendResponse.Name = "txtSendResponse";
            this.txtSendResponse.Size = new System.Drawing.Size(315, 26);
            this.txtSendResponse.TabIndex = 12;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(13, 246);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(124, 20);
            this.label7.TabIndex = 11;
            this.label7.Text = "Send Response";
            // 
            // txtSourceId
            // 
            this.txtSourceId.Location = new System.Drawing.Point(143, 200);
            this.txtSourceId.Name = "txtSourceId";
            this.txtSourceId.Size = new System.Drawing.Size(248, 26);
            this.txtSourceId.TabIndex = 10;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 204);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(78, 20);
            this.label6.TabIndex = 9;
            this.label6.Text = "Source Id";
            // 
            // txtReceivedData
            // 
            this.txtReceivedData.Location = new System.Drawing.Point(143, 159);
            this.txtReceivedData.Name = "txtReceivedData";
            this.txtReceivedData.Size = new System.Drawing.Size(315, 26);
            this.txtReceivedData.TabIndex = 8;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 163);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(75, 20);
            this.label5.TabIndex = 7;
            this.label5.Text = "Received";
            // 
            // btnRegisterListener
            // 
            this.btnRegisterListener.Enabled = false;
            this.btnRegisterListener.Location = new System.Drawing.Point(17, 103);
            this.btnRegisterListener.Name = "btnRegisterListener";
            this.btnRegisterListener.Size = new System.Drawing.Size(154, 44);
            this.btnRegisterListener.TabIndex = 6;
            this.btnRegisterListener.Text = "Register Listener";
            this.btnRegisterListener.UseVisualStyleBackColor = true;
            this.btnRegisterListener.Click += new System.EventHandler(this.btnRegisterListener_Click);
            // 
            // btnSendRequest
            // 
            this.btnSendRequest.Enabled = false;
            this.btnSendRequest.Location = new System.Drawing.Point(457, 52);
            this.btnSendRequest.Name = "btnSendRequest";
            this.btnSendRequest.Size = new System.Drawing.Size(146, 38);
            this.btnSendRequest.TabIndex = 5;
            this.btnSendRequest.Text = "Send Request";
            this.btnSendRequest.UseVisualStyleBackColor = true;
            this.btnSendRequest.Click += new System.EventHandler(this.btnSendRequest_Click);
            // 
            // txtSendRequest
            // 
            this.txtSendRequest.Location = new System.Drawing.Point(131, 58);
            this.txtSendRequest.Name = "txtSendRequest";
            this.txtSendRequest.Size = new System.Drawing.Size(315, 26);
            this.txtSendRequest.TabIndex = 4;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 61);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(112, 20);
            this.label4.TabIndex = 3;
            this.label4.Text = "Send Request";
            // 
            // txtMsgChannelName
            // 
            this.txtMsgChannelName.Location = new System.Drawing.Point(295, 8);
            this.txtMsgChannelName.Name = "txtMsgChannelName";
            this.txtMsgChannelName.Size = new System.Drawing.Size(308, 26);
            this.txtMsgChannelName.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(175, 11);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(114, 20);
            this.label3.TabIndex = 1;
            this.label3.Text = "Channel Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 11);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 20);
            this.label2.TabIndex = 0;
            this.label2.Text = "Messaging";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnBroadcast);
            this.panel2.Controls.Add(this.txtReceivedPublishMessage);
            this.panel2.Controls.Add(this.label11);
            this.panel2.Controls.Add(this.txtPublishMessage);
            this.panel2.Controls.Add(this.label10);
            this.panel2.Controls.Add(this.btnSubscribe);
            this.panel2.Controls.Add(this.txtBcastChannelName);
            this.panel2.Controls.Add(this.label9);
            this.panel2.Controls.Add(this.label8);
            this.panel2.Location = new System.Drawing.Point(48, 407);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(636, 259);
            this.panel2.TabIndex = 4;
            // 
            // btnBroadcast
            // 
            this.btnBroadcast.Enabled = false;
            this.btnBroadcast.Location = new System.Drawing.Point(502, 82);
            this.btnBroadcast.Name = "btnBroadcast";
            this.btnBroadcast.Size = new System.Drawing.Size(115, 36);
            this.btnBroadcast.TabIndex = 8;
            this.btnBroadcast.Text = "Broadcast";
            this.btnBroadcast.UseVisualStyleBackColor = true;
            this.btnBroadcast.Click += new System.EventHandler(this.btnBroadcast_Click);
            // 
            // txtReceivedPublishMessage
            // 
            this.txtReceivedPublishMessage.Location = new System.Drawing.Point(179, 173);
            this.txtReceivedPublishMessage.Name = "txtReceivedPublishMessage";
            this.txtReceivedPublishMessage.Size = new System.Drawing.Size(438, 26);
            this.txtReceivedPublishMessage.TabIndex = 7;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(13, 176);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(135, 20);
            this.label11.TabIndex = 6;
            this.label11.Text = "Receive Message";
            // 
            // txtPublishMessage
            // 
            this.txtPublishMessage.Location = new System.Drawing.Point(179, 124);
            this.txtPublishMessage.Name = "txtPublishMessage";
            this.txtPublishMessage.Size = new System.Drawing.Size(438, 26);
            this.txtPublishMessage.TabIndex = 5;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(13, 127);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(151, 20);
            this.label10.TabIndex = 4;
            this.label10.Text = "Broadcast Message";
            // 
            // btnSubscribe
            // 
            this.btnSubscribe.Enabled = false;
            this.btnSubscribe.Location = new System.Drawing.Point(17, 64);
            this.btnSubscribe.Name = "btnSubscribe";
            this.btnSubscribe.Size = new System.Drawing.Size(133, 40);
            this.btnSubscribe.TabIndex = 3;
            this.btnSubscribe.Text = "Subscribe";
            this.btnSubscribe.UseVisualStyleBackColor = true;
            this.btnSubscribe.Click += new System.EventHandler(this.btnSubscribe_Click);
            // 
            // txtBcastChannelName
            // 
            this.txtBcastChannelName.Location = new System.Drawing.Point(305, 11);
            this.txtBcastChannelName.Name = "txtBcastChannelName";
            this.txtBcastChannelName.Size = new System.Drawing.Size(298, 26);
            this.txtBcastChannelName.TabIndex = 2;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(175, 14);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(114, 20);
            this.label9.TabIndex = 1;
            this.label9.Text = "Channel Name";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(17, 14);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(82, 20);
            this.label8.TabIndex = 0;
            this.label8.Text = "Broadcast";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(44, 38);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(141, 20);
            this.label12.TabIndex = 6;
            this.label12.Text = "Connect To Server";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(721, 678);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.txtServerName);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Middleware Test";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtServerName;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnSendResponse;
        private System.Windows.Forms.TextBox txtSendResponse;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtSourceId;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtReceivedData;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnRegisterListener;
        private System.Windows.Forms.Button btnSendRequest;
        private System.Windows.Forms.TextBox txtSendRequest;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtMsgChannelName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtBcastChannelName;
        private System.Windows.Forms.Button btnSubscribe;
        private System.Windows.Forms.Button btnBroadcast;
        private System.Windows.Forms.TextBox txtReceivedPublishMessage;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtPublishMessage;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label12;
    }
}

