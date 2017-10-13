#include "stdafx.h"
#include "easywsclient.hpp"
#include "MiddlewareClientLib.h"
#include <boost/shared_ptr.hpp>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>

using namespace easywsclient;

namespace MiddlewareLib
{
	class Session : public ISession
	{
	public:
		Session(char const* url) : poll_(100)
		{
			WSADATA wsaData;
			int iResult;
			iResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
			if (iResult != 0) {
				std::cout << "WSAStartup failed:" << iResult << std::endl;
			}

			hShutdownEvent_ = CreateEvent(NULL, FALSE, FALSE, NULL);
			hClosedEvent_ = CreateEvent(NULL, FALSE, FALSE, NULL);
			connection_ = WebSocketPtr_t(WebSocket::from_url(url));
		}

		virtual ~Session()
		{
			//first, stop dispatching
			SetEvent(hShutdownEvent_);
			WaitForSingleObject(hClosedEvent_, 1000);
			CloseHandle(hShutdownEvent_);
			CloseHandle(hClosedEvent_);
			WSACleanup();
		}

		void SendData(const std::string& data)
		{
			if (connection_ != NULL)
			{
				connection_->send(data);
			}
		}

		//this call will start dispatching messages to and from the socket
		//on this thread!!
		void StartDispatcher(CALLBACK_FUNC handler)
		{
			//_handler = handler;
			if (connection_ != NULL)
			{
				while (WaitForSingleObject(hShutdownEvent_, poll_) == WAIT_TIMEOUT)
				{
					connection_->poll();
					connection_->dispatch([handler](const std::string& message, void* context)
					{
						auto session = (Session*)context;
						if (session) {
							//CALLBACK_FUNC handler = session->GetHandler();
							if (handler) {
								handler(session, message);
							}
						}
					}, this);
				}

				if ((connection_->getReadyState() != WebSocket::CLOSED) &&
					(connection_->getReadyState() != WebSocket::CLOSING))
				{
					connection_->close();

					SetEvent(hClosedEvent_);
				}
			}
		}

		//CALLBACK_FUNC GetHandler()
		//{
		//	return _handler;
		//}

	private:
		//CALLBACK_FUNC _handler;
		typedef  boost::shared_ptr<WebSocket> WebSocketPtr_t;
		WebSocketPtr_t connection_;
		HANDLE hShutdownEvent_;
		HANDLE hClosedEvent_;
		DWORD poll_;
	};

	MIDDLEWARE_EXP ISession* CreateSession(char const* url)
	{
		return new Session(url);
	}

	void MIDDLEWARE_EXP DestroySession(ISession* session)
	{
		delete session;
	}
}
