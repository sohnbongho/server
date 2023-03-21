#include <iostream>
#include <sdkddkver.h>
#include <unordered_map> //이건 왜 ? 
//다중접속 서버이기 때문에. 소켓 여러개의 연결을 유지하기 위해 그걸 담아둘 컨테이너.
//클라 구조체를 만들어서 그걸 담아두도록 하자.

#include <boost/asio.hpp>
#include <memory>

using boost::asio::ip::tcp; //이걸 한 이유는 boost::asio::ip 이걸 쓸데없이 쓰면 길어지니까. 
//주의점 : 에러, 버퍼 같은 것 헷갈릴 수 있다.

using namespace std;

constexpr int PORT = 3500;

class session; // 클라 정보를 저장할 자료구조
unordered_map <int, session> g_clients;
atomic_int g_client_id = 0;

//클라 정보를 저장할 자료구조.
class session
{
    int my_id;
    tcp::socket socket_;

    enum my_enum { max_length = 1024};
    char data_[max_length];

public:
    session() : socket_(nullptr)
    {
        cout << "Session Creation Error. \n";
    }
    session(tcp::socket socket, int id): socket_(std::move(socket)), my_id(id)
    {
        do_read();
    }
    void do_read()
    {
        socket_.async_read_some(boost::asio::buffer(data_, max_length),
            [this](boost::system::error_code ec, std::size_t length)
            {
                if(ec)
                {
                    cout << "Disconnetced Client [" << my_id << "]." << endl;
                    g_clients.erase(my_id);
                }
                else
                {
                    data_[length] = 0;
                    cout << "Client [" << my_id << "]: " << data_ << endl;
                    g_clients[my_id].do_write(length);
                }

            });	    
    }

    void do_write(std::size_t length)
    {
        boost::asio::async_write(socket_, boost::asio::buffer(data_, length),
            [this](boost::system::error_code ec, std::size_t length)
            {
                if (!ec)g_clients[my_id].do_read();
                else g_clients.erase(my_id);
            });	    
    }
	
}; 



void accept_callback(boost::system::error_code ec, tcp::socket& socket, tcp::acceptor& my_acceptor)
{
    int new_id = g_client_id++;
    cout << "New Client [" << new_id << "] connected. \n";
    g_clients.try_emplace(new_id, move(socket), new_id);

    my_acceptor.async_accept([&my_acceptor](boost::system::error_code ec, tcp::socket socket){
            accept_callback(ec, socket, my_acceptor);
        });
}

int main() {
    try
    {
        boost::asio::io_context io_context;

        tcp::acceptor my_acceptor{ io_context, tcp::endpoint(tcp::v4(), PORT) };

        cout << "Server started at port " << PORT << ".\n";

        my_acceptor.async_accept([&my_acceptor](boost::system::error_code ec, tcp::socket socket)
            {
                accept_callback(ec, socket, my_acceptor);
            });

        io_context.run();

    }
    catch (std::exception& e)
    {
        std::cerr << "Exception : " << e.what() << "\n";
    }
    
    return 0;
}
