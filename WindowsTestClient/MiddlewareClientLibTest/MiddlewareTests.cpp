#include "stdafx.h"
#include <boost\test\unit_test.hpp>
#include "MiddlewareClientLib.h"
#include <boost/shared_ptr.hpp>
#include <boost/algorithm/string/replace.hpp>

namespace
{
	class TestSession : public MiddlewareLib::ISession
	{
	public:
		TestSession() : _handler(NULL) {}
		virtual ~TestSession() {}
		virtual void SendData(const std::string& data)
		{
			data_ = data;
		}

		virtual void RegisterCallbackHandler(MiddlewareLib::CALLBACK_FUNC handler)
		{
			_handler = handler;
		}

		MiddlewareLib::CALLBACK_FUNC _handler;
		std::string data_;
	};

	typedef  boost::shared_ptr<TestSession> TestSessionPtr_t;;

	std::string TestChannel = "TestChannel";
	std::string TestPayload = "tes payload data";
	std::string TestSendRequestMessage = "{\"Type\": \"0\", \"RequestId\": \"123\", \"Command\": \"SENDREQUEST\", \"Channel\": \"TestChannel\", \"DestinationId\": \"xyz\", \"Payload\": \"hello\"}";
	std::string TestPublishUpdateMessage = "{\"Type\": \"1\", \"RequestId\": \"123\", \"Command\": \"PUBLISHMESSAGE\", \"Channel\": \"TestChannel\", \"DestinationId\": \"xyz\", \"Payload\": \"goodbye\"}";
	std::string receivedPayload;
	std::string receivedCommand;

	static void handler_callback(const MiddlewareLib::Message& message)
	{
		receivedCommand = message.command_;
		receivedPayload = message.payload_;
	}

	TestSessionPtr_t CreateTestSession()
	{
		TestSessionPtr_t session(new TestSession());
		MiddlewareLib::RegisterMessageCallbackFunction(session.get(), handler_callback);
		BOOST_CHECK(session->_handler != NULL);
		return session;
	}

	std::string replaceMessageChars(std::string const& message, char const* oldChars, char const* newChars)
	{
		std::size_t first = message.find(oldChars, 0);
		std::size_t second = first + strlen(oldChars);
		std::ostringstream ss;
		ss << message.substr(0, first) << newChars << message.substr(second);
		return ss.str();
	}
}

BOOST_AUTO_TEST_SUITE(middleware_tests)

BOOST_AUTO_TEST_CASE(when_registering_session)
{
	TestSessionPtr_t session = CreateTestSession();
}

BOOST_AUTO_TEST_CASE(when_subscribing_to_a_channel)
{
	TestSessionPtr_t session = CreateTestSession();
	MiddlewareLib::MiddlewareRequestParams params{ TestChannel , NULL, NULL };
	bool success = MiddlewareLib::SubscribeToChannel(session.get(), params);
	BOOST_CHECK(success);

	BOOST_CHECK(session->data_.find("SUBSCRIBETOCHANNEL") != std::string::npos);
	BOOST_CHECK(session->data_.find("TestChannel") != std::string::npos);

}

BOOST_AUTO_TEST_CASE(when_subscribing_to_a_channel_with_success)
{
	TestSessionPtr_t session = CreateTestSession();
	bool result = false;

	MiddlewareLib::MiddlewareRequestParams params{ TestChannel ,
		[](const std::string& data) -> void { BOOST_CHECK(true); },
		[](const std::string& data) -> void { BOOST_CHECK(false); } };

	bool success = MiddlewareLib::SubscribeToChannel(session.get(), params);
	BOOST_CHECK(success);

	boost::replace_first(session->data_, "\"0\"", "\"3\"");
	session->_handler(session->data_);
}

BOOST_AUTO_TEST_CASE(when_subscribing_to_a_channel_with_failure)
{
	TestSessionPtr_t session = CreateTestSession();
	bool result = false;

	MiddlewareLib::MiddlewareRequestParams params{ TestChannel ,
		[](const std::string& data) -> void { BOOST_CHECK(false); },
		[](const std::string& data) -> void { BOOST_CHECK(true); } };

	bool success = MiddlewareLib::SubscribeToChannel(session.get(), params);
	BOOST_CHECK(success);

	boost::replace_first(session->data_, "\"0\"", "\"2\"");
	//std::string newMsg = replaceMessageChars(session->data_, "\"0\"", "\"2\"");
	session->_handler(session->data_);
}

BOOST_AUTO_TEST_CASE(when_Adding_listener_to_a_channel_with_success)
{
	TestSessionPtr_t session = CreateTestSession();
	bool result = false;

	MiddlewareLib::MiddlewareRequestParams params{ TestChannel ,
		[](const std::string& data) -> void { BOOST_CHECK(true); },
		[](const std::string& data) -> void { BOOST_CHECK(false); } };

	bool success = MiddlewareLib::AddChannelListener(session.get(), params);
	BOOST_CHECK(success);

	//std::string newMsg = replaceMessageChars(session->data_, "\"0\"", "\"3\"");
	boost::replace_first(session->data_, "\"0\"", "\"3\"");
	session->_handler(session->data_);
}

BOOST_AUTO_TEST_CASE(when_Adding_listener_to_a_channel_with_failure)
{
	TestSessionPtr_t session = CreateTestSession();
	bool result = false;

	MiddlewareLib::MiddlewareRequestParams params{ TestChannel ,
		[](const std::string& data) -> void { BOOST_CHECK(false); },
		[](const std::string& data) -> void { BOOST_CHECK(true); } };

	bool success = MiddlewareLib::AddChannelListener(session.get(), params);
	BOOST_CHECK(success);

	//std::string newMsg = replaceMessageChars(session->data_, "\"0\"", "\"2\"");
	boost::replace_first(session->data_, "\"0\"", "\"2\"");
	session->_handler(session->data_);
}

BOOST_AUTO_TEST_CASE(when_sending_message_to_a_channel_with_success)
{
	TestSessionPtr_t session = CreateTestSession();
	bool result = false;

	MiddlewareLib::MiddlewareRequestParams params{ TestChannel ,
		[](const std::string& data) -> void { 
										BOOST_CHECK(true);
										BOOST_CHECK(data == TestPayload); },
		[](const std::string& data) -> void { BOOST_CHECK(false); } };

	bool success = MiddlewareLib::SendMessageToChannel(session.get(), params, TestPayload);
	BOOST_CHECK(success);

	boost::replace_first(session->data_, "\"0\"", "\"3\""); 
	//std::string newMsg = replaceMessageChars(session->data_, "\"0\"", "\"3\"");
	session->_handler(session->data_);
}

BOOST_AUTO_TEST_CASE(when_sending_message_to_a_channel_with_failure)
{
	TestSessionPtr_t session = CreateTestSession();
	bool result = false;

	MiddlewareLib::MiddlewareRequestParams params{ TestChannel ,
		[](const std::string& data) -> void { BOOST_CHECK(false); },
		[](const std::string& data) -> void { BOOST_CHECK(true); } };

	bool success = MiddlewareLib::SendMessageToChannel(session.get(), params, TestPayload);
	BOOST_CHECK(success);

	boost::replace_first(session->data_, "\"0\"", "\"2\"");
	//std::string newMsg = replaceMessageChars(session->data_, "\"0\"", "\"2\"");
	session->_handler(session->data_);
}

BOOST_AUTO_TEST_CASE(when_receiving_a_send_request_message)
{
	TestSessionPtr_t session = CreateTestSession();
	session->_handler(TestSendRequestMessage);
	BOOST_CHECK(receivedCommand == "SENDREQUEST");
	BOOST_CHECK(receivedPayload == "hello");
}

BOOST_AUTO_TEST_CASE(when_receiving_a_publish_update_message)
{
	TestSessionPtr_t session = CreateTestSession();
	session->_handler(TestPublishUpdateMessage);
	BOOST_CHECK(receivedCommand == "PUBLISHMESSAGE");
	BOOST_CHECK(receivedPayload == "goodbye");
}

BOOST_AUTO_TEST_SUITE_END()
