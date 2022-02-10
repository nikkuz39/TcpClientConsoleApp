using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TcpClientConsoleApp
{
    public class ClientCore
    {        
        // Переменная хранит количество цифр для отправки на сервер
        private int numbersSendServer;

        // Переменная хранит количество потоков
        private int threadsCount;

        // Переменная хранит путь к файлу, в котором хранится результат обработки данных
        private string filePath = @"DataFile.txt";

        // Массив задач
        private Task[] taskArray;

        // Список хранит полученные от сервера цифры
        private List<int> numbersReceived = new List<int>();

        // Потокобезопасная коллекция
        private static BlockingCollection<int> blocingCollection;

        // Список для сохранения полученных данных в файл и загрузки этих дынных из файла
        private Dictionary<int, int> saveData = new Dictionary<int, int>();

        // Конструктор инициализирует поля класса
        public ClientCore(int threadsCou, int numbers)
        {
            numbersSendServer = numbers;
            threadsCount = threadsCou;
            taskArray = new Task[threadsCount];
        }

        // Метод подключается к серверу
        private TcpClient ConnectToServer()
        {
            var client = new TcpClient();
            client.Connect("88.212.241.115", 2013);

            return client;
        }

        // Метод выводит на консоль условия задачи, либо проверка медианы
        public void GetTask(string message)
        {
            var data = Encoding.UTF8.GetBytes(message + "\n");
            var stream = ConnectToServer().GetStream();
            stream.Write(data, 0, data.Length);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(20866), false);
            string inboxMessage = reader.ReadToEnd();

            Console.WriteLine(inboxMessage);
        }

        // Метод записывает полученные данные в файл
        private void WriteDictionaryToFile(Dictionary<int, int> data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<int, int> kvp in saveData)
            {
                sb.AppendLine(string.Format("{0}-{1}", kvp.Key, kvp.Value));
            }

            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                using (TextWriter tw = new StreamWriter(fs))
                    tw.Write(sb.ToString());
            }
        }

        // Метод читает данные из файла
        private void ReadDictionaryToFile()
        {
            saveData = File.ReadLines(filePath)
                        .Select(line => line.Split('-'))
                        .Where(arr => arr.Length == 2)
                        .ToDictionary(arr => Convert.ToInt32(arr[0]), arr => Convert.ToInt32(arr[1]));
        }

        // Метод выводит количество выполненных задач
        private void UpdateStatusBar()
        {
            Console.Clear();
            Console.WriteLine($"{numbersReceived.Count}/{numbersSendServer}");
        }

        // Метод считает медиан
        private void MedianCalculation(List<int> listNumbers)
        {
            listNumbers.Sort();

            if (numbersSendServer % 2 == 1)
            {
                Console.WriteLine($"{(decimal)listNumbers.ElementAt(numbersSendServer / 2)}");
            }
            else
            {
                Console.WriteLine($"{(decimal)(listNumbers.ElementAt(numbersSendServer / 2 - 1) + listNumbers.ElementAt(numbersSendServer / 2)) / 2}");
            }
        }

        // Метод запускает основной процесс программы
        public void StartProcess()
        {
            ReadDictionaryToFile();

            for (int i = 0; i <= saveData.Count; i++)
            {
                if (saveData.ContainsKey(i) == false)
                    continue;

                numbersReceived.Add(saveData[i]);
            }

            using (blocingCollection = new BlockingCollection<int>())
            {
                for (int i = 1; i <= numbersSendServer; i++)
                {
                    if (saveData.ContainsKey(i))
                        continue;

                    blocingCollection.Add(i);
                }

                blocingCollection.CompleteAdding();

                for (int i = 0; i < threadsCount; i++)
                {
                    taskArray[i] = Task.Run(SendAndReceiveDataFromServer);
                }

                Task.WaitAll(taskArray);
            }

            MedianCalculation(numbersReceived);
        }

        // Метод отправляет цифры на сервер, получает ответ и передает данные в список
        private void SendAndReceiveDataFromServer()
        {
            var stream = ConnectToServer().GetStream();

            foreach (var item in blocingCollection.GetConsumingEnumerable())
            {
                var data = Encoding.UTF8.GetBytes($"{item}\n");
                int keyData = item;
                stream.Write(data, 0, data.Length);

                var bytesList = new List<byte>();
                int symb;

                do
                {
                    symb = stream.ReadByte();
                    bytesList.Add((byte)symb);
                } while ((char)symb != '\n');

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                string incomingMessage = Convert.ToString(Encoding.GetEncoding(20866).GetString(bytesList.ToArray(), 0, bytesList.Count));

                string cleanMessage = Regex.Replace(incomingMessage, @"\D", "");
                
                lock (numbersReceived)
                    numbersReceived.Add(Convert.ToInt32(cleanMessage));

                int keyValue = Convert.ToInt32(cleanMessage);
                saveData.Add(keyData, keyValue);

                WriteDictionaryToFile(saveData);
                
                UpdateStatusBar();
                Thread.Sleep(3000);                
            }

            ConnectToServer().Close();
        }
    }
}
