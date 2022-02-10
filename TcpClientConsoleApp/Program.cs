using System;
using System.Threading;

namespace TcpClientConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Вывести в консоль количество минимальных и максимальных потоков
            // доступных для обработки задачи
            MaxThreads();
            MinThreads();
            Console.WriteLine();

            // Ввести количество потоков
            Console.WriteLine("Tasks:");
            int numberThreads = Convert.ToInt32(Console.ReadLine());
            if (numberThreads <= 0)
                numberThreads = 1;

            // Ввести количество цифр, которые будут отправлены на сервер
            Console.WriteLine("Numbers:");
            int numbersSend = Convert.ToInt32(Console.ReadLine());
            if (numbersSend <= 0)
                numbersSend = 1;

            ClientCore clientCore = new ClientCore(numberThreads, numbersSend);

            // Получить условия задачи
            //clientCore.GetTask("Greetings");
            // Проверить медиан
            //clientCore.GetTask("Check 4925680,5");

            // Запустить процесс
            clientCore.StartProcess();            
        }

        // Метод показывает минимальное число потоков
        static void MinThreads()
        {
            int minWorker;
            int minIOC;

            ThreadPool.GetMinThreads(out minWorker, out minIOC);

            Console.WriteLine($"Min. worker: {minWorker} / Min. IOC: {minIOC}");
        }

        // Метод показывает максимальное число потоков
        static void MaxThreads()
        {
            int workerThreads;
            int portThreads;

            ThreadPool.GetMaxThreads(out workerThreads, out portThreads);

            Console.WriteLine($"Max. worker: {workerThreads} / Max. IOC: {portThreads}");
        }
    }
}
