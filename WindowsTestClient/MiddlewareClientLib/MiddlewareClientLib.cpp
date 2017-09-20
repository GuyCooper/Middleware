// MiddlewareClientLib.cpp : Defines the exported functions for the DLL application.
//
#include "stdafx.h"

#include <boost/property_tree/ptree.hpp>
#include <boost/property_tree/json_parser.hpp>
#include <boost/uuid/uuid.hpp>
#include <boost/uuid/uuid_io.hpp>
#include <boost/uuid/uuid_generators.hpp>
#include <map>
#include <sstream>
#include "MiddlewareClientLib.h"

//#include <boost/uuid/uuid_generators.hpp>
//#include "IMiddlewareHandler.h"

namespace pt = boost::property_tree;

namespace MiddlewareLib
{
	Message fromJSON(const std::string& data)
	{
		Message msg;
		pt::ptree tree;

		std::istringstream in(data);
		pt::read_json(in, tree);
		msg.type_ = (MessageType)tree.get<int>("Type");
		msg.requestId_ = tree.get<std::string>("RequestId");
		msg.command_ = tree.get<std::string>("Command");
		msg.channel_ = tree.get<std::string>("Channel");
		msg.destinationId_ = tree.get<std::string>("DestinationId");
		msg.payload_ = tree.get<std::string>("Payload");

		return msg;
	}

	std::string toJSON(const Message& msg)
	{
		pt::ptree tree;
		tree.put("Type", (int)msg.type_);
		tree.put("RequestId", msg.requestId_);
		tree.put("Command", msg.command_);
		tree.put("Channel", msg.channel_);
		tree.put("DestinationId", msg.destinationId_);
		tree.put("Payload", msg.payload_);
			
		std::ostringstream out;
		pt::write_json(out, tree);
		return out.str();
	}

	MSG_CALLBACK_FUNC g_msgCallback = NULL;

	//IMiddlewareHandler* g_handler = NULL;

	typedef std::map<std::string, MiddlewareRequestParams> REQUEST_LIST_T;
	REQUEST_LIST_T g_currentCalls;

	void callbackHandler(const std::string& data)
	{
		//deserailise data into a Message object
		Message msg = fromJSON(data);
		
		if (msg.type_ == REQUEST || msg.type_ == UPDATE)
		{
			//just send message to client
			if (g_msgCallback != NULL)
			{
				g_msgCallback(msg);
			}
			return;
		}

		REQUEST_LIST_T::const_iterator it = g_currentCalls.find(msg.requestId_);
		if (it != g_currentCalls.end())
		{
			MiddlewareRequestParams const& params = it->second;
			if (msg.type_ == RESPONSE_SUCCESS)
				params.on_success(msg.payload_);
			else if (msg.type_ == RESPONSE_ERROR)
				params.on_error(msg.payload_);

			g_currentCalls.erase(it);
		}
	}
	 
	bool doRequestInternal(ISession *session, const MiddlewareRequestParams& params, const std::string& command, const std::string& payload)
	{
		Message msg;
		msg.channel_ = params.channel;
		msg.command_ = command;
		msg.type_ = REQUEST;
		msg.payload_ = payload;

		boost::uuids::random_generator gen;
		boost::uuids::uuid u = gen();
		msg.requestId_ = boost::uuids::to_string(u);

		//add to current call list
		auto it = g_currentCalls.find(msg.requestId_);
		if (it != g_currentCalls.end())
		{
			throw new std::runtime_error("request already pending!!!");
		}

		g_currentCalls.insert(REQUEST_LIST_T::value_type(msg.requestId_, params));

		if (session != NULL)
		{
			session->SendData(toJSON(msg));
			return true;
		}

		return false;
	}

	bool MIDDLEWARE_EXP SubscribeToChannel(ISession *session, const MiddlewareRequestParams& params)
	{
		return doRequestInternal(session, params, "SUBSCRIBETOCHANNEL", "");
	}

	bool MIDDLEWARE_EXP SendMessageToChannel(ISession *session, const MiddlewareRequestParams& params, const std::string& payload)
	{
		return doRequestInternal(session, params, "SENDMESSAGE", payload);
	}

	bool MIDDLEWARE_EXP AddChannelListener(ISession *session, const MiddlewareRequestParams& params)
	{
		return doRequestInternal(session, params, "ADDLISTENER", "");
	}

	bool MIDDLEWARE_EXP SendRequest(ISession *session, const MiddlewareRequestParams& params, const std::string& payload)
	{
		return doRequestInternal(session, params, "SENDREQUEST", payload);
	}

	bool MIDDLEWARE_EXP PublishMessage(ISession *session, const MiddlewareRequestParams& params, const std::string& payload)
	{
		return doRequestInternal(session, params, "PUBLISHMESSAGE", payload);
	}

	void MIDDLEWARE_EXP RegisterMessageCallbackFunction(ISession *session, MSG_CALLBACK_FUNC msgCallback)
	{
		session->RegisterCallbackHandler(callbackHandler);
		g_msgCallback = msgCallback;
	}
}

