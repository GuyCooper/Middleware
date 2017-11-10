// JavaScript source code
'use strict'

//const uuidv1 = require('uuid/v1');

var uid = 1;

var MiddleWare = function () {
    var ws;
    var callQueue = [];

    this.Connect = function (server, username, password, onopen, onerror, onmessage) {
        ws = new WebSocket(server);

       ws.onopen = function () {
            console.log("onopen");
            sendLoginRequest(username, password);
        }.bind(this);

        ws.onerror = function (error) {
            console.log("error received: " + error);
            onerror(error);
        }.bind(this);

        var processResponse = function(message, success) {
            for(var i = 0; i < callQueue.length; i++) {
                var msg = callQueue[i];
                if(msg.id === message.RequestId) {
                    //message found. remove it from queue
                    callQueue.splice(i, 1);
                    if(success) {
                        msg.succeed(message.Payload);
                    }
                    else {
                        msg.failed(message.Payload);
                    }
                    break;
                }
            }
        };

        var loginSuccess = function (message) {
            if (onopen != null) {
                onopen(message);
            }
        };

        var loginFail = function(message) {
            if (onerror != null) {
                onerror(message);
            }
        };

        var sendLoginRequest = function (username, password) {
            var loginRequest = {
                UserName: username,
                Password: password,
                Source: window.location.hostname,
                AppName: "Javascript App",
                Version: "1.0"
            };

            processRequestInternal("LOGIN", "DOLOGIN", 0, JSON.stringify(loginRequest), null, loginSuccess, loginFail);
        };

        ws.onmessage = function (data) {
            console.log("data received: " + data.data);
            var message = JSON.parse(data.data);
            if (message != null && message != undefined) {
                switch (message.Type) {
                    case 0:
                    case 1:
                        //request or update. just forward to client
                        onmessage(message);
                        break;
                    case 2:
                        //error response
                        processResponse(message, false);
                        break;
                    case 3:
                        //succes response
                        processResponse(message, true);
                        break;
                    default:
                        console.error("invalid message type: " + message.Type);
                        break;
                }
            }

        };
    };

    var processRequestInternal = function(channel, command, type, data, destination, resolve, reject) {
        var id = "test_" + uid++;
        var payload = {
            RequestId: id,
            Command: command,
            Channel: channel,
            Type: type,
            Payload: data,
            DestinationId: destination
        };

        //add al request type messages to call queue. this means that we expect a response
        //for thenm from the server
        if (type === 0) {
            callQueue.push({
                id: id,
                succeed: resolve,
                failed: reject
            });
        }

        ws.send(JSON.stringify(payload));
    }

    var processRequest = function(channel, command, type, data, destination) {
        return new Promise(function (resolve, reject) {
            processRequestInternal(channel, command, type, data, destination, resolve, reject);
        });

    };

    this.SubscribeChannel = function (channel) {
        return processRequest(channel, "SUBSCRIBETOCHANNEL", 0, null, null);
    };

    this.SendMessage  = function(channel, message, destination) {
        return processRequest(channel, "SENDMESSAGE", 1, message, destination);
    }

    this.AddListener = function (channel) {
        return processRequest(channel, "ADDLISTENER", 0, null, null);
    }

    this.SendRequest = function(channel, message) {
        return processRequest(channel, "SENDREQUEST", 0, message);
    }

    this.PublishMessage = function(channel, message) {
        return processRequest(channel, "PUBLISHMESSAGE", 1, message);
    }
}