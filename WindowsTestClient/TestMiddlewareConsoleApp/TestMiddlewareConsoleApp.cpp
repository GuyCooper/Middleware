// TestMiddlewareConsoleApp.cpp : Defines the entry point for the console application.
//
#include "stdafx.h"
#include <MiddlewareClientLib.h>
#include <boost/shared_ptr.hpp>

namespace
{
	std::string TestChannel = "TestChannel23";
}

static void middlewareHandler(const MiddlewareLib::Message& message)
{

}

static void sendFailed(const std::string& message)
{

}

static void sendSucceded(const std::string& message)
{

}

int main()
{
	MiddlewareLib::ISession* session = MiddlewareLib::CreateSession("ws://localhost:8080");
	MiddlewareLib::RegisterMessageCallbackFunction(session, middlewareHandler);

	//register as a listener to test channel
	MiddlewareLib::MiddlewareRequestParams params{ TestChannel, sendSucceded, sendFailed };

	while (true)
	{
		Sleep(1000);
	}

	MiddlewareLib::DestroySession(session);
    return 0;
}

