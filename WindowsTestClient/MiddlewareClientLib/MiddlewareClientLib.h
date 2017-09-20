#pragma once

#ifdef MIDDLEWARECLIENTLIB_EXPORTS
#define MIDDLEWARE_EXP __declspec(dllexport)
#else
#define MIDDLEWARE_EXP __declspec(dllimport)
#endif

namespace MiddlewareLib
{
	enum MessageType
	{
		REQUEST = 0,
		UPDATE = 1,
		RESPONSE_ERROR = 2,
		RESPONSE_SUCCESS = 3
	};

	struct Message
	{
		MessageType type_;
		std::string requestId_;
		std::string command_;
		std::string channel_;
		std::string destinationId_;
		std::string payload_;

	};

	typedef void(*CALLBACK_FUNC)(const std::string& message);
	typedef void(*MSG_CALLBACK_FUNC)(const Message& message);

	class ISession
	{
	public:
		virtual void SendData(const std::string& data) = 0;
		virtual void RegisterCallbackHandler(CALLBACK_FUNC handler) = 0;
	};

	struct MiddlewareRequestParams
	{
		std::string channel;
		CALLBACK_FUNC on_success;
		CALLBACK_FUNC on_error;
	};

	bool MIDDLEWARE_EXP SubscribeToChannel(ISession *session, const MiddlewareRequestParams& params);
	bool MIDDLEWARE_EXP SendMessageToChannel(ISession *session, const MiddlewareRequestParams& params, const std::string& payload);
	bool MIDDLEWARE_EXP AddChannelListener(ISession *session, const MiddlewareRequestParams& params);
	bool MIDDLEWARE_EXP SendRequest(ISession *session, const MiddlewareRequestParams& params, const std::string& payload);
	bool MIDDLEWARE_EXP PublishMessage(ISession *session, const MiddlewareRequestParams& params, const std::string& payload);
	MIDDLEWARE_EXP ISession*  CreateSession(char const* url);
	void MIDDLEWARE_EXP DestroySession(ISession* session);
	void MIDDLEWARE_EXP RegisterMessageCallbackFunction(ISession *session, MSG_CALLBACK_FUNC msgCallback);
}


