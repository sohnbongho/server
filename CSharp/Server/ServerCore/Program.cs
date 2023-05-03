using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class Program
    {
        static void MainThread(object state)
        {
            for(int i = 0; i< 5; i++)
            {
                Console.WriteLine("Hello Thread!");
            }
        }

        static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(5, 5);

            for (int i = 0; i < 5; i++)
            {
                // LongRunning옵션을 사용하였지만 자동으로 추가 쓰레드를 만들어준다.
                // 아주 오래 걸리는 작업의경우 Task를 만들어서 처리하자.
                Task t = new Task(() => { while (true) { } });
                t.Start();
            }                

            //for (int i= 0; i< 4;i ++)
            //{
            //    ThreadPool.QueueUserWorkItem((obj) => { while (true) { } });
            //}

            // 위에 쓰레드를 다 사용(5개) 하고 있지만 Task로 만들었기에 쓰레드풀이 남아있다.
            ThreadPool.QueueUserWorkItem(MainThread); 
            // 동시에 돌릴수 있는 쓰레드 수를 가지고 
            // 기존의 작업 중인 것들이 돌아왔을 때 다음 일감을 받아서 처리한다.

            //for(int i = 0; i< 1000; i++)
            //{
            //    Thread t = new Thread(MainThread);
            //    //t.Name = "Test Thread";
            //    t.IsBackground = true;  // 메인쓰레드가 종료되면 BackGround쓰레드도 종료된다.
            //    t.Start(); // 별도의 쓰레드 실행
            //}


            //Console.WriteLine("Waiting for Thread!");

            //t.Join();
            //Console.WriteLine("Hello World!");
            while (true)
            {

            }
        }
    }
}
