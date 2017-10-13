// TestMiddlewareConsoleApp.cpp : Defines the entry point for the console application.
//
#include "stdafx.h"
#include <MiddlewareClientLib.h>
#include <boost/shared_ptr.hpp>
#include <iostream>

namespace
{
	std::string TestChannel = "TestChannel23";
}

static void middlewareHandler(MiddlewareLib::ISession* session, const MiddlewareLib::Message& message)
{
	std::cout << "command received: " << message.command_ << " on channel: "
		<< message.channel_ << ". Payload: " << message.payload_ << std::endl;

	if (message.command_ == "SENDREQUEST")
	{
		//send a reply back to the sender
		if (session != NULL)
		{
			std::string response = "you just said: " + message.payload_;
			MiddlewareLib::MiddlewareRequestParams params{ message.channel_ , NULL, NULL };
			MiddlewareLib::SendMessageToChannel(session, params, response, message.sourceId_);
		}
	}
}

static void sendFailed(MiddlewareLib::ISession*, const std::string& message)
{
	std::cout << "send message failed" << std::endl;
}

static void sendSucceded(MiddlewareLib::ISession*, const std::string& message)
{
	std::cout << "send message succeded" << std::endl;
}

typedef boost::shared_ptr<MiddlewareLib::ISession> SessionPtr_t;

int main()
{
	SessionPtr_t session(MiddlewareLib::CreateSession("ws://localhost:8080"), MiddlewareLib::DestroySession);
	MiddlewareLib::RegisterMessageCallbackFunction( middlewareHandler);

	//register as a listener to test channel
	MiddlewareLib::MiddlewareRequestParams params{ TestChannel, sendSucceded, sendFailed };
	MiddlewareLib::AddChannelListener(session.get(), params);

	MiddlewareLib::StartDispatching(session.get());

    return 0;
}

