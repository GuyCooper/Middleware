#include "stdafx.h"
#include "easywsclient.hpp"
#include "MiddlewareClientLib.h"
#include <boost/shared_ptr.hpp>

using namespace easywsclient;

namespace MiddlewareLib
{
	class Session : public ISession
	{
	public:
		Session(char const* url)
		{
			connection_ = WebSocketPtr_t(WebSocket::from_url(url));
		}

		virtual ~Session()
		{
			if (connection_ != NULL)
			{
				if ((connection_->getReadyState() != WebSocket::CLOSED) &&
					(connection_->getReadyState() != WebSocket::CLOSING))
				{
					connection_->close();
				}
			}
		}

		void SendData(const std::string& data)
		{
			if (connection_ != NULL)
			{
				connection_->send(data);
			}
		}

		void RegisterCallbackHandler(CALLBACK_FUNC handler)
		{
			if (connection_ != NULL)
			{
				connection_->dispatch(handler);
			}
		}

	private:
		typedef  boost::shared_ptr<WebSocket> WebSocketPtr_t;
		WebSocketPtr_t connection_;
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
