using ALARm.Core.Report;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
//using System.Windows.Forms;

namespace ALARm.Core
{
    public static class Helper
    {
        /// <summary>
        /// Пороговые величины поперечного непогашенного ускорения и ψ
        /// </summary>
        /// 


        public static ThresholdsAcceleration GetAnp(VagonType vagonType, int curveRadius = -1)
        {
            ThresholdsAcceleration temp = new ThresholdsAcceleration { };

            switch (vagonType)
            {
                case VagonType.Sapsan:
                    if (curveRadius < 1600)
                    {
                        temp.AnpBliskoeKDopusku = 0.67;
                        temp.AnpOgrSkor = 0.7;
                        temp.AgOgrSkor = 0.9;
                    }
                    else if (curveRadius >= 1600 && curveRadius <= 3000)
                    {
                        temp.AnpBliskoeKDopusku = 0.77;
                        temp.AnpOgrSkor = 0.8;
                        temp.AgOgrSkor = 1;
                    }
                    else if (curveRadius > 3000)
                    {
                        temp.AnpBliskoeKDopusku = 0.87;
                        temp.AnpOgrSkor = 0.9;
                        temp.AgOgrSkor = 1.1;
                    }
                    break;

                case VagonType.Lastochka:
                    if (curveRadius <= 400)
                    {
                        temp.AnpBliskoeKDopusku = 0.67;
                        temp.AnpOgrSkor = 0.7;
                        temp.AgOgrSkor = 0.9;
                    }
                    else if (curveRadius > 400)
                    {
                        temp.AnpBliskoeKDopusku = 0.87;
                        temp.AnpOgrSkor = 0.9;
                        temp.AgOgrSkor = 1.1;
                    }
                    break;

                case VagonType.Strizh:
                    temp.AnpBliskoeKDopusku = 1.1;
                    temp.AnpOgrSkor = 1.2;
                    temp.AgOgrSkor = 1.4;
                    break;

                case VagonType.Allegro:
                    temp.AnpBliskoeKDopusku = 0.95;
                    temp.AnpOgrSkor = 1;
                    temp.AgOgrSkor = 1.2;
                    break;

                case VagonType.PassengerAndFreight:
                    temp.AnpBliskoeKDopusku = 0.65;
                    temp.AnpOgrSkor = 0.7;
                    temp.AgOgrSkor = 0.9;
                    break;

                default:
                    break;
            }

            return temp;
        }

        //public static SocketState SendMessageFromRabbitMQ(string Host, long TripId, int Msg)
        //{
        //    try
        //    {
        //        //var factory = new ConnectionFactory() { HostName = "mycomputer", UserName = "beebo", Password = "beebo" };
        //        var factory = new ConnectionFactory() { HostName = "mycomputer", UserName = "alarm", Password = "alarm" };

        //        using (var connection = factory.CreateConnection())
        //        using (var channel = connection.CreateModel())
        //        {
        //            channel.QueueDeclare(queue: Host,
        //                                 durable: false,
        //                                 exclusive: false,
        //                                 autoDelete: false,
        //                                 arguments: null);
        //            var json = "{'Host':'" + Host + "', 'TripId':'" + TripId + "', 'Msg':" + Msg + "}";
        //            var body = Encoding.UTF8.GetBytes(json);

        //            channel.BasicPublish(exchange: "",
        //                                 routingKey: Host,
        //                                 basicProperties: null,
        //                                 body: body);
        //            Console.WriteLine(" [x] Sent {0}", json);

        //        }
        //        return SocketState.Success;
        //    }
        //    catch (Exception e)
        //    {
        //        return SocketState.Abortively;
        //    }
        //}

        public static SocketState SendMessageFromRabbitMQ(string Host, long TripId, int Msg)
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = "localhost",
                    Port = 5672,   // ✅ Правильный порт RabbitMQ
                    UserName = "beebo",
                    Password = "beebo123"

                };
                ////  HostName = "localhost", // можно передавать Host извне
                ////UserName = "alarm",
                ////Password = "alarm"

                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: Host,
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    var json = $"{{\"Host\":\"{Host}\", \"TripId\":{TripId}, \"Msg\":{Msg}}}";
                    var body = Encoding.UTF8.GetBytes(json);

                    channel.BasicPublish(exchange: "",
                                         routingKey: Host,
                                         basicProperties: null,
                                         body: body);

                    Console.WriteLine(" [x] Sent: {0}", json);
                }

                return SocketState.Success;
            }
            catch (BrokerUnreachableException ex)
            {
                Console.WriteLine($"[RabbitMQ Error] Broker unreachable: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return SocketState.Abortively;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[Network Error] Cannot reach host: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return SocketState.Abortively;
            }
            catch (AuthenticationFailureException ex)
            {
                Console.WriteLine($"[Auth Error] Authentication failed: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return SocketState.Abortively;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General Error] {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return SocketState.Abortively;
            }
        }

        public static SocketState SendMessageToRabbitMQExchange(IConnection connection, string exchange, String message)
        {
            try
            {


                using (var channel = connection.CreateModel())
                {

                    //ToDo queue жоқ болып құрылса extension қосу керек


                    var body = Encoding.UTF8.GetBytes(message);



                    channel.BasicPublish(exchange: exchange,
                                         routingKey: "",
                                         basicProperties: null,
                                         body: body);
                    Console.WriteLine(" [x] Sent {0}", message);

                }
                return SocketState.Success;
            }
            catch (Exception e)
            {
                return SocketState.Abortively;
            }
        }
        public static string GetShortForm(string name)
        {
            if (name == null)
                return "";
            name = name.ToUpper();
            string vowelsList = "АЕЁИОУЫЭЮЯ";
            string result = name.Length > 4 ? name.Substring(0, 4) : name;
            if ((result[3] == 'Ь') && (name.Length > 4))
                result += name[4];
            if (vowelsList.Contains(result[3]))
                result = result.Substring(0, 3);

            return result;
        }

        public static string GetShortFormInNormalString(string name)
        {
            name = GetShortForm(name);
            if (name.Length > 1)
                name = name[0] + name.Substring(1, name.Length - 1).ToLower();
            return name;
        }

        public static string GetResourceName(string name)
        {
            return Properties.Resources.ResourceManager.GetString(name.ToString());
        }
        public static int RoundTo10(this int digit)
        {
            int mod = digit % 10;
            return digit - mod;

        }
        public static int GetNextTop(this List<int> tops, int start, int top, int number)
        {
            top = top.RoundTo10();
            for (int i = top; i < number * 100; i += 10)
            {
                if (!tops.Contains(i))
                    return i;
            }
            for (int i = top; i > start.RoundTo10(); i -= 10)
            {
                if (!tops.Contains(i))
                    return i;
            }
            return 10;
        }
        public static int GetNextTopFromBottom(this List<int> tops, int start)
        {

            for (int i = start.RoundTo10() + 10; i <= (start + 1) * 100; i += 10)
            {
                if (!tops.Contains(i))
                    return i;
            }
            return 10;

        }
        public static Picket GetPicket(this List<Picket> pickets, int meter)
        {
            Picket result = null;
            foreach (var picket in pickets)
            {
                if ((meter / 100 + 1) == picket.Number)
                    result = picket;
            }
            if (result == null)
            {
                try
                {
                    if (pickets.Count > 0)
                    {
                        result = new Picket() { Number = pickets[pickets.Count - 1].Number + 1, Start = pickets[pickets.Count - 1].Number * 100 };
                    }
                    else
                    {
                        result = new Picket() { Number = meter / 100 + 1, Start = (meter / 100) * 100 };

                    }
                    pickets.Add(result);
                    return result;
                }
                catch (Exception e)
                {
                    Console.WriteLine("GetPicket" + e.Message);
                }
            }
            if (result == null)
                return result;
            if (!result.IsFilled)
                return result;

            int notFillIndex = -1;
            for (int i = pickets.IndexOf(result) - 1; i >= 0; i--)
            {
                if (!pickets[i].IsFilled)
                {
                    notFillIndex = i;
                    break;
                }
            }
            if (notFillIndex != -1)
            {
                for (int i = notFillIndex; i < pickets.IndexOf(result); i++)
                {
                    var first = pickets[i + 1].Digression.GetFirstMove();
                    if (first != null)
                    {
                        pickets[i].Digression.Add(first);
                        pickets[i + 1].Digression.Remove(first);
                    }

                }
                return result;
            }
            notFillIndex = -1;
            for (int i = pickets.IndexOf(result); i < pickets.Count; i++)
            {
                if (!pickets[i].IsFilled)
                {
                    notFillIndex = i;
                    break;
                }
            }
            if (notFillIndex != -1)
            {
                return pickets[notFillIndex];
            }
            result = new Picket() { Number = pickets[pickets.Count - 1].Number + 1, Start = pickets[pickets.Count - 1].Number * 100 };
            pickets.Add(result);
            return result;
        }
        public static DigressionMark GetFirstMove(this List<DigressionMark> notes)
        {
            DigressionMark result = null;
            for (int i = 0; i < notes.Count; i++)
            {
                if (!(notes[i].Note().Contains("Уст.ск:") || notes[i].Note().Contains("R") || notes[i].NotMoveAlert))
                {
                    result = notes[i];
                    break;
                }
            }
            return result;
        }
        public static Bitmap ToBitmap(this int[,] GreyScaleArray)
        {
            int width = GreyScaleArray.GetLength(1); // read from file
            int height = GreyScaleArray.GetLength(0); // read from file
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    int red = GreyScaleArray[y, x]; // read from array
                    int green = GreyScaleArray[y, x]; // read from array
                    int blue = GreyScaleArray[y, x]; // read from array
                    bitmap.SetPixel(x, y, Color.FromArgb(0, red, green, blue));
                }
            return bitmap;
        }
        public static Bitmap ToRedBitmap(this int[,] GreyScaleArray)
        {
            int width = GreyScaleArray.GetLength(1); // read from file
            int height = GreyScaleArray.GetLength(0); // read from file
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    int red = GreyScaleArray[y, x]; // read from array
                    //int green = GreyScaleArray[y, x]; // read from array
                    //int blue = GreyScaleArray[y, x]; // read from array
                    bitmap.SetPixel(x, y, Color.FromArgb(0, red, 0, 0));
                }
            return bitmap;
        }
        public static int[,] ToRedMatrix(this Bitmap bitmap)
        {
            int hight = bitmap.Height;
            int width = bitmap.Width;

            int[,] matrix = new int[hight, width];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < hight; j++)
                {
                    var pixelColor = bitmap.GetPixel(i, j);
                    matrix[j, i] = (int)(pixelColor.R * 0.3 + pixelColor.G * 0.59 + pixelColor.B * 0.11); ;
                }
            }
            return matrix;
        }
        public static int[,] ToMatrix(this Bitmap bitmap)
        {
            int hight = bitmap.Height;
            int width = bitmap.Width;

            int[,] matrix = new int[width, hight];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < hight; j++)
                {
                    var pixelColor = bitmap.GetPixel(i, j);
                    matrix[i, j] = (int)(pixelColor.R * 0.3 + pixelColor.G * 0.59 + pixelColor.B * 0.11); ;
                }
            }
            return matrix;
        }
        //public static SocketState SendMessageFromSocket(string ip, int port, string message)
        //{
        //    try
        //    {
        //        // Буфер для входящих данных
        //        byte[] bytes = new byte[1024];

        //        // Соединяемся с удаленным устройством

        //        // Устанавливаем удаленную точку для сокета
        //        IPHostEntry ipHost = Dns.GetHostEntry(ip);
        //        IPAddress ipAddr = ipHost.AddressList[0];
        //        IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

        //        Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        //        // Соединяем сокет с удаленной точкой
        //        sender.Connect(ipEndPoint);


        //        byte[] msg = Encoding.UTF8.GetBytes(message);

        //        // Отправляем данные через сокет
        //        int bytesSent = sender.Send(msg);

        //        // Получаем ответ от сервера
        //        int bytesRec = sender.Receive(bytes);

        //        var reply = Encoding.UTF8.GetString(bytes, 0, bytesRec);

        //        // Используем рекурсию для неоднократного вызова SendMessageFromSocket()
        //        if (reply.IndexOf("<Success>") == -1)
        //            return SocketState.Abortively;

        //        // Освобождаем сокет
        //        sender.Shutdown(SocketShutdown.Both);
        //        sender.Close();
        //        return SocketState.Success;


        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        return SocketState.Abortively;
        //    }
        //}
        public static SocketState SendMessageFromSocket(string ip, int port, string message)
        {
            try
            {
                Console.WriteLine($"[INFO] Connecting to {ip}:{port}...");

                byte[] bytes = new byte[1024];

                IPHostEntry ipHost = Dns.GetHostEntry(ip);
                IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

                Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sender.Connect(ipEndPoint);
                Console.WriteLine("[INFO] Connected successfully.");

                byte[] msg = Encoding.UTF8.GetBytes(message);
                int bytesSent = sender.Send(msg);
                Console.WriteLine($"[INFO] Sent {bytesSent} bytes: {message}");

                int bytesRec = sender.Receive(bytes);
                var reply = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                Console.WriteLine($"[INFO] Received reply: {reply}");

                if (!reply.Contains("<Success>"))
                {
                    Console.WriteLine("[WARNING] Reply does not contain <Success>");
                    return SocketState.Abortively;
                }

                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
                Console.WriteLine("[INFO] Socket closed.");
                return SocketState.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return SocketState.Abortively;
            }
        }





        public static double ToDoubleCoordinate(this int km, int meter)
        {
            return km + meter / 10000.0;
        }
        public static bool Between(this double number, double a, double b)
        {
            return a <= number && number <= b;
        }

        public static bool Between(this int number, int a, int b)
        {
            return a <= number && number <= b;
        }
        public static double RadiusToAvg(this double radius)
        {
            return radius switch
            {
                double rad when rad < 5102 && rad >= 4464 => 3.5,
                double rad when rad < 4464 && rad >= 3968 => 4,
                double rad when rad < 3968 && rad >= 3571 => 5,
                double rad when rad < 3571 && rad >= 3247 => 5.5,
                double rad when rad < 3247 && rad >= 2976 => 6,
                double rad when rad < 2976 && rad >= 2758 => 6.5,
                double rad when rad < 2758 && rad >= 2552 => 7,
                double rad when rad < 2552 && rad >= 2380 => 7.5,
                double rad when rad < 2380 && rad >= 2232 => 8,
                double rad when rad < 2232 && rad >= 2101 => 8.5,
                double rad when rad < 2101 && rad >= 1984 => 9,
                double rad when rad < 1984 && rad >= 1880 => 9.5,
                double rad when rad < 1880 && rad >= 1786 => 10,
                double rad when rad < 1786 && rad >= 1701 => 10.5,
                double rad when rad < 1701 && rad >= 1623 => 11,
                double rad when rad < 1623 && rad >= 1553 => 11.5,
                double rad when rad < 1553 && rad >= 1488 => 12,
                double rad when rad < 1488 && rad >= 1429 => 12.5,
                double rad when rad < 1429 && rad >= 1374 => 13,
                double rad when rad < 1374 && rad >= 1323 => 13.5,
                double rad when rad < 1323 && rad >= 1276 => 14,
                double rad when rad < 1276 && rad >= 1232 => 14.5,
                double rad when rad < 1232 && rad >= 1190 => 15,
                double rad when rad < 1190 && rad >= 1152 => 15.5,
                double rad when rad < 1152 && rad >= 1116 => 16,
                double rad when rad < 1116 && rad >= 1082 => 16.5,
                double rad when rad < 1082 && rad >= 1050 => 17,
                double rad when rad < 1050 && rad >= 1020 => 17.5,
                double rad when rad < 1020 && rad >= 992 => 18,
                double rad when rad < 992 && rad >= 965 => 18.5,
                double rad when rad < 965 && rad >= 940 => 19,
                double rad when rad < 940 && rad >= 916 => 19.5,
                double rad when rad < 916 && rad >= 893 => 20,
                double rad when rad < 893 && rad >= 871 => 20.5,
                double rad when rad < 871 && rad >= 850 => 21,
                double rad when rad < 850 && rad >= 831 => 21.5,
                double rad when rad < 831 && rad >= 812 => 22,
                double rad when rad < 812 && rad >= 794 => 22.5,
                double rad when rad < 794 && rad >= 776 => 23,
                double rad when rad < 776 && rad >= 760 => 23.5,
                double rad when rad < 760 && rad >= 744 => 24,
                double rad when rad < 744 && rad >= 729 => 24.5,
                double rad when rad < 729 && rad >= 714 => 25,
                double rad when rad < 714 && rad >= 700 => 25.5,
                double rad when rad < 700 && rad >= 687 => 26,
                double rad when rad < 687 && rad >= 674 => 26.5,
                double rad when rad < 674 && rad >= 661 => 27,
                double rad when rad < 661 && rad >= 649 => 27.5,
                double rad when rad < 649 && rad >= 638 => 28,
                double rad when rad < 638 && rad >= 627 => 28.5,
                double rad when rad < 627 && rad >= 616 => 29,
                double rad when rad < 616 && rad >= 605 => 29.5,
                double rad when rad < 605 && rad >= 595 => 30,
                double rad when rad < 595 && rad >= 585 => 30.5,
                double rad when rad < 585 && rad >= 576 => 31,
                double rad when rad < 576 && rad >= 567 => 31.5,
                double rad when rad < 567 && rad >= 558 => 32,
                double rad when rad < 558 && rad >= 549 => 32.5,
                double rad when rad < 549 && rad >= 541 => 33,
                double rad when rad < 541 && rad >= 533 => 33.5,
                double rad when rad < 533 && rad >= 525 => 34,
                double rad when rad < 525 && rad >= 518 => 34.5,
                double rad when rad < 518 && rad >= 510 => 35,
                double rad when rad < 510 && rad >= 503 => 35.5,
                double rad when rad < 503 && rad >= 496 => 36,
                double rad when rad < 496 && rad >= 489 => 36.5,
                double rad when rad < 489 && rad >= 483 => 37,
                double rad when rad < 483 && rad >= 476 => 37.5,
                double rad when rad < 476 && rad >= 470 => 38,
                double rad when rad < 470 && rad >= 464 => 38.5,
                double rad when rad < 464 && rad >= 458 => 39,
                double rad when rad < 458 && rad >= 452 => 39.5,
                double rad when rad < 452 && rad >= 446 => 40,
                double rad when rad < 446 && rad >= 441 => 40.5,
                double rad when rad < 441 && rad >= 436 => 41,
                double rad when rad < 436 && rad >= 430 => 41.5,
                double rad when rad < 430 && rad >= 425 => 42,
                double rad when rad < 425 && rad >= 420 => 42.5,
                double rad when rad < 420 && rad >= 415 => 43,
                double rad when rad < 415 && rad >= 411 => 43.5,
                double rad when rad < 411 && rad >= 406 => 44,
                double rad when rad < 406 && rad >= 401 => 44.5,
                double rad when rad < 401 && rad >= 397 => 45,
                double rad when rad < 397 && rad >= 392 => 45.5,
                double rad when rad < 392 && rad >= 388 => 46,
                double rad when rad < 388 && rad >= 384 => 46.5,
                double rad when rad < 384 && rad >= 380 => 47,
                double rad when rad < 380 && rad >= 376 => 47.5,
                double rad when rad < 376 && rad >= 372 => 48,
                double rad when rad < 372 && rad >= 368 => 48.5,
                double rad when rad < 368 && rad >= 364 => 49,
                double rad when rad < 364 && rad >= 361 => 49.5,
                double rad when rad < 361 && rad >= 357 => 50,
                double rad when rad < 357 && rad >= 354 => 50.5,
                double rad when rad < 354 && rad >= 350 => 51,
                double rad when rad < 350 && rad >= 347 => 51.5,
                double rad when rad < 347 && rad >= 343 => 52,
                double rad when rad < 343 && rad >= 340 => 52.5,
                double rad when rad < 340 && rad >= 337 => 53,
                double rad when rad < 337 && rad >= 334 => 53.5,
                double rad when rad < 334 && rad >= 331 => 54,
                double rad when rad < 331 && rad >= 328 => 54.5,
                double rad when rad < 328 && rad >= 325 => 55,
                double rad when rad < 325 && rad >= 322 => 55.5,
                double rad when rad < 322 && rad >= 319 => 56,
                double rad when rad < 319 && rad >= 316 => 56.5,
                double rad when rad < 316 && rad >= 313 => 57,
                double rad when rad < 313 && rad >= 311 => 57.5,
                double rad when rad < 311 && rad >= 308 => 58,
                double rad when rad < 308 && rad >= 305 => 58.5,
                double rad when rad < 305 && rad >= 303 => 59,
                double rad when rad < 303 && rad >= 300 => 59.5,
                double rad when rad < 300 && rad >= 298 => 60,
                double rad when rad < 298 && rad >= 295 => 60.5,
                double rad when rad < 295 && rad >= 293 => 61,
                double rad when rad < 293 && rad >= 288 => 62.5,
                double rad when rad < 288 && rad >= 283 => 63,
                double rad when rad < 283 && rad >= 279 => 63.5,
                double rad when rad < 279 && rad >= 274 => 64,
                double rad when rad < 274 && rad >= 270 => 64.5,
                double rad when rad < 270 && rad >= 266 => 65,
                double rad when rad < 266 && rad >= 263 => 65.5,
                double rad when rad < 263 && rad >= 259 => 66,
                double rad when rad < 259 && rad >= 255 => 66.5,
                double rad when rad < 255 && rad >= 252 => 67,
                double rad when rad < 252 && rad >= 250 => 67.5,
                double rad when rad < 250 && rad >= 247 => 68,
                double rad when rad < 247 && rad >= 245 => 68.5,
                double rad when rad < 245 && rad >= 244 => 69,
                double rad when rad < 244 && rad >= 238 => 71.5,
                double rad when rad < 238 && rad >= 234 => 73,
                double rad when rad < 234 && rad >= 231 => 74.5,
                double rad when rad < 231 && rad >= 228 => 76,
                double rad when rad < 228 && rad >= 225 => 77.5,
                double rad when rad < 225 && rad >= 223 => 78,
                double rad when rad < 223 && rad >= 220 => 79.5,
                double rad when rad < 220 && rad >= 217 => 81,
                double rad when rad < 217 && rad >= 215 => 81.5,
                double rad when rad < 215 && rad >= 212 => 83,
                double rad when rad < 212 && rad >= 210 => 83.5,
                double rad when rad < 210 && rad >= 207 => 85,
                double rad when rad < 207 && rad >= 205 => 85.5,
                double rad when rad < 205 && rad >= 203 => 86,
                double rad when rad < 203 && rad >= 200 => 87.5,
                double rad when rad < 200 && rad >= 198 => 88,
                double rad when rad < 198 && rad >= 196 => 88.5,
                double rad when rad < 196 && rad >= 194 => 89,
                double rad when rad < 194 && rad >= 191 => 90.5,
                double rad when rad < 191 && rad >= 189 => 91,
                double rad when rad < 189 && rad >= 187 => 91.5,
                double rad when rad < 187 && rad >= 185 => 92,
                double rad when rad < 185 && rad >= 183 => 92.5,
                double rad when rad < 183 && rad >= 181 => 93,
                double rad when rad < 181 && rad >= 180 => 93.5,
                double rad when rad < 180 && rad >= 178 => 94,
                double rad when rad < 178 && rad >= 176 => 94.5,
                double rad when rad < 176 && rad >= 174 => 95,
                double rad when rad < 174 && rad >= 172 => 95.5,
                double rad when rad < 172 && rad >= 170 => 96,
                double rad when rad < 170 && rad >= 168 => 96.5,
                double rad when rad < 168 && rad >= 166 => 97,
                _ => 0,
            };
        }
        public static int[] RealCoordinateToKmMeter(this float real)
        {
            var result = new int[] { -1, -1 };
            result[0] = (int)real;
            result[1] = (int)((real % 1) * 10000);
            return result;

        }
        public static int[] RealCoordinateToKmMeter(this double real)
        {
            var result = new int[] { -1, -1 };
            result[0] = (int)real;
            result[1] = (int)((real % 1) * 10000);
            return result;

        }


        public static List<double> GetTrapezoidPM(
            this List<double> avg, List<double> prev, List<double> next,
            int height, ref List<NatureCurves> CurveForLvl,
            bool getPairs = false, Direction naprav = Direction.Direct, List<double> strRealData = null)
        {


            var ZeroDataStright = avg.Select(o => o * 0.0).ToList();
            var MashtPM = 1;
            var MashtR = 1;
            try
            {
                var listY = new List<double>();
                var listX = new List<double>();
                var transitions = new List<double>();
                var result = new List<double>();
                var originalDbPoints = new List<double>();
                var RealoriginalDbPoints = new List<double>();
                var listYRevers = new List<double>();
                var listXRevers = new List<double>();
                var transitionsrevers = new List<double>();
                var resultRevers = new List<double>();
                var originalDbPointsRevers = new List<double>();
                var RealoriginalDbPointsRevers = new List<double>();

                var prev0 = new List<double>();
                var avg0 = new List<double>();
                var next0 = new List<double>();
                var promavg = new List<double>();
                var psdvig = new List<double>();
                int d = 10;
                int d2 = 0;
                int d1 = 0;
                int b = 10000;
                int a = 0;

                originalDbPoints.AddRange(psdvig);
                originalDbPoints.AddRange(prev);
                originalDbPoints.AddRange(avg);
                originalDbPoints.AddRange(next);

                originalDbPointsRevers.AddRange(prev);
                originalDbPointsRevers.AddRange(avg);
                originalDbPointsRevers.AddRange(next);
                originalDbPointsRevers.Reverse();
                RealoriginalDbPointsRevers.Reverse();

                if (height == 4 || height == 10)
                {
                    RealoriginalDbPoints.AddRange(prev);
                    RealoriginalDbPoints.AddRange(strRealData);
                    RealoriginalDbPoints.AddRange(next);

                    RealoriginalDbPointsRevers.AddRange(prev);
                    RealoriginalDbPointsRevers.AddRange(strRealData);
                    RealoriginalDbPointsRevers.AddRange(next);
                }


                if (naprav == Direction.Reverse)
                {


                    originalDbPointsRevers.Reverse();
                    RealoriginalDbPointsRevers.Reverse();
                    originalDbPoints.Reverse();
                    RealoriginalDbPoints.Reverse();

                }



                listY.Add(originalDbPoints[0]);
                listX.Add(0);


                int i = 0;

                for (int j = i + 1; j < originalDbPoints.Count(); j++)
                {

                    double
                        Bx = j - i,
                        By = (originalDbPoints[j] - originalDbPoints[i]);

                    double otnosh = By / Bx;

                    for (int k = i; k < j + 1; k++)
                    {
                        var A = -By;
                        var B = Bx;
                        var C = (i * originalDbPoints[j] - j * originalDbPoints[i]);

                        var maxDistance = Math.Abs(A * k + B * originalDbPoints[k] + C) / Math.Sqrt(A * A + B * B);


                        var h = Math.Abs(originalDbPoints[j - 1]);
                        var mDis = 2 + (4 * h * h) / (h * h + 200);

                        // var kk = 0.1;

                        var kk = 0.051;
                        if (maxDistance > mDis * kk)
                        {
                            listY.Add(originalDbPoints[k + 1]);
                            listX.Add(k + 1);

                            i = k + 1;
                            break;
                        }
                    }
                }





                if (!listX.Where(s => s == originalDbPoints.IndexOf(originalDbPoints.Count())).Any())
                {
                    listY.Add(originalDbPoints.Last());
                    listX.Add(originalDbPoints.Count - 1);
                }


                double linearY = originalDbPoints[0];
                var k_linear = new List<double> { };
                var linear_prom = new List<double> { };
                var b_linear = new List<double> { };
                var ind_k_min = new List<int> { };
                var ind_lk = new List<int> { };

                int ind = 0;

                var avglinear = new List<double> { };
                var maxlinear = new List<double> { };

                for (int t = 0; t < listY.Count() - 1; t++)
                {
                    var indAvg = new List<double> { };
                    var indMax = new List<double> { };

                    for (int c = 0; c < listX[t + 1] - listX[t]; c++)
                    {
                        double x = listX[t] + c;
                        double dx1 = x - listX[t];

                        double bottom_dx1 = listX[t + 1] - listX[t];

                        double y2 = listY[t + 1];
                        double dx2 = x - listX[t + 1];
                        double bottom_dx2 = listX[t] - listX[t + 1];

                        double y1 = listY[t];
                        linearY = ((y2 - y1) / bottom_dx1 * c + y1);

                        result.Add(linearY);

                        ind++;

                        indAvg.Add(linearY);
                        indMax.Add(Math.Abs(linearY));
                    }


                    //ToDo Tolegen
                    if (indAvg.Count > 0)
                        avglinear.Add(indAvg.Average());
                    if (indMax.Count > 0)
                        maxlinear.Add(indMax.Max());

                    ind_lk.Add(ind);
                    k_linear.Add((listY[t + 1] - listY[t]) / (listX[t + 1] - listX[t]));
                    b_linear.Add(listY[t]);
                }
                var resultTest = new List<double>();
                resultTest = result;
                var resultTestRevers = new List<double>();
                resultTestRevers = resultRevers;
                ////////////////////////////////
                var ResultTestCurve = new List<ResultTestCurve>();
                if (height == 4)
                //   if (height == 4 || height == 10)
                {
                    var tempRealY = new List<double> { };
                    var tempY = new List<double> { };
                    var tempX = new List<int> { };

                    //      var tempRes = result[0] > 0 ? 1 : result[0];

                    // tempRealY.Add(RealoriginalDbPoints[0]);
                    ///tempY.Add(result[0]);
                    //tempX.Add(0);
                    var flag_Plus = false;
                    var flag_Minus = false;
                    for (int resInd = 1; resInd < result.Count - 1; resInd++)
                    {
                        //var incVal = result[resInd] == 0 ? 1 : result[resInd];
                        //var incVal = result[resInd] ==0 ? 1 : result[resInd];


                        if (result[resInd] > 7)
                        {
                            tempRealY.Add(RealoriginalDbPoints[resInd]);
                            tempY.Add(result[resInd]);
                            tempX.Add(resInd);
                            flag_Plus = true;
                        }
                        if (result[resInd] < 7 && flag_Plus)
                        {
                            //ResultTestCurve.Add(
                            //    new ResultTestCurve
                            //    {
                            //        RealY = tempRealY,
                            //        Y = tempY,
                            //        X = tempX
                            //    });

                            tempRealY = new List<double> { };
                            tempY = new List<double> { };
                            tempX = new List<int> { };
                            flag_Plus = false;
                            //tempRes = result[resInd] == 0 ? 1 : result[resInd];
                        }



                        if (result[resInd] < -7)
                        {
                            tempRealY.Add(RealoriginalDbPoints[resInd]);
                            tempY.Add(result[resInd]);
                            tempX.Add(resInd);
                            flag_Minus = true;
                        }

                        if (result[resInd] > -7 && flag_Minus)
                        {
                            //ResultTestCurve.Add(
                            //    new ResultTestCurve
                            //    {
                            //        RealY = tempRealY,
                            //        Y = tempY,
                            //        X = tempX
                            //    });

                            tempRealY = new List<double> { };
                            tempY = new List<double> { };
                            tempX = new List<int> { };
                            flag_Minus = false;
                            //tempRes = result[resInd] == 0 ? 1 : result[resInd];
                        }







                    }


                    //ResultTestCurve.Add(
                    //            new ResultTestCurve
                    //            {
                    //                RealY = tempRealY,
                    //                Y = tempY,
                    //                X = tempX
                    //            });
                }
                if (ResultTestCurve.Any())
                {
                    var aaa = ResultTestCurve.Where(o => o.RealY.Count > 20).ToList();
                    var bbb = aaa.Where(o => o.RealY.Select(o => Math.Abs(o)).Max() >= 35).ToList();
                    var ttt = bbb.Where(o => o.RealY.Count < 1).ToList();

                    if (height == 4)
                    {
                        foreach (var item in ttt)
                        {
                            var localInd = 0;
                            for (int q = item.X.First(); q < item.X.Last(); q++)
                            {
                                result[q] = 0.0;
                                localInd++;
                            }
                            var ppx = item.X.First();
                            var ppy = item.X.Last();
                        }



                    }
                }
                if (height == 4 || height == 10)
                {

                    var height0 = 0.0;

                    var Curves = new List<NatureCurves> { };
                    var Curve = new NatureCurves { };

                    var CurveData = new List<double>();
                    var CurveIndex = new List<int>();

                    var height00 = 10;
                    var dz = 0.0;
                    for (int ii = 0; ii < Math.Min(k_linear.Count, maxlinear.Count) - 1; ii++)
                    {
                        linear_prom.Clear();


                        var z = maxlinear.Max(Math.Abs) + 2;

                        dz = 7 * z * z / (20 * 20 + z * z);

                        //if (height == 4) height0 = 4 + 0.5*dz;
                        //if (height == 10) height0 = 5 + dz;
                        if (height == 4) height0 = 7;
                        if (height == 10) height0 = 9;
                        if (Math.Abs(maxlinear[ii]) <= MashtPM * (height0))
                        {
                            linear_prom.Clear();



                            for (int c = 0; c < listX[ii + 1] - listX[ii]; c++)
                            {
                                var prom = 0.0;
                                linear_prom.Add(prom);
                            }
                            var p = 0;
                            for (int y = (int)listX[ii]; y < listX[ii + 1]; y++)
                            {
                                result[y] = linear_prom[p];
                                p++;
                            }
                            if (Curve.X.Count > 20 && (Curve.Y.Max() > MashtPM * height0 || Curve.Y.Min() < -MashtPM * height0))
                            {
                                Curve.Added = true;
                                Curves.Add(Curve);
                                Curve = new NatureCurves { };
                            }

                        }

                        //5-тен жогарылары 

                        if (Math.Abs(maxlinear[ii]) > (height0))
                        {
                            for (int y = (int)listX[ii]; y < listX[ii + 1]; y++)
                            {

                                if (Curve.Y.Any() && y - Curve.X.Last() > (height0))
                                //if (Curve.Y.Any() && Math.Abs(y )-> MashtPM * (height0))
                                {
                                    Curves.Add(Curve);
                                    Curve = new NatureCurves { };
                                }

                                CurveData.Add(result[y]);
                                CurveIndex.Add(y);

                                Curve.Y.Add(result[y]);
                                Curve.X.Add(y);
                            }
                        }
                    }


                    if (!Curve.Added)
                    {
                        Curve.Added = true;
                        Curves.Add(Curve);
                        Curve = new NatureCurves { };
                    }

                    //Если нужна средняя линия
                    //var resultCopy = result;
                    var resultCopy = new List<double>(new double[result.Count]);

                    Curves = Curves.Where(o => o.X.Count > 0).ToList();
                    ///                  Расширяем границы
                    ///                  



                    ///Расширяем границы кривой на 30 метров, с высотой меньше 10 мм
                    ///


                    //for (int cInd = 1; cInd < Curves.Count; cInd++)
                    //{
                    //    var tempCurveMaxY = Curves[cInd].Y.Max();
                    //    var tempCurveLastY = Curves[cInd - 1].Y.Last();
                    //    var tempCurveFirstY = Curves[cInd].Y.First();
                    //    var tempCurveLastX = Curves[cInd - 1].X.Last();
                    //    var tempCurveFirstX = Curves[cInd].X.First();
                    //    //var delta = 50;
                    //    if (Math.Abs(tempCurveLastY - tempCurveFirstY) < 5
                    //        && Math.Abs(tempCurveLastX - tempCurveFirstX) < 20
                    //        && Math.Abs(tempCurveLastY) > 5

                    //          && Math.Sign(tempCurveLastY) == Math.Sign(tempCurveLastY))
                    //    {



                    //        var startInd = Curves[cInd - 1].X.Last();

                    //        if (startInd < 0)
                    //        {
                    //            startInd = 0;
                    //        }
                    //        var tempCurveLen = Curves[cInd].X.Count();
                    //        //конец кривой






                    //        Curves[cInd - 1] = new NatureCurves { };
                    //        try
                    //        {
                    //            var tY = result.GetRange(startInd, tempCurveLen).ToList();
                    //            var tX = Enumerable.Range(startInd, tempCurveLen).ToList();
                    //            Curves[cInd - 1].X.AddRange(tX);
                    //            Curves[cInd - 1].Y.AddRange(tY);
                    //            Curves.RemoveAt(cInd);
                    //        }
                    //        catch (Exception e)
                    //        {
                    //            Console.WriteLine("CurvescInD" + e.Message);
                    //        }


                    //    }
                    //}









                    for (int cInd = 0; cInd < Curves.Count; cInd++)
                    {
                        var tempCurveMaxY = Curves[cInd].Y.Max();
                        var delta = 0;
                        //var delta = 50;
                        if (tempCurveMaxY <= 10 && Curves[cInd].X.Count() > delta)
                        {
                            //начало кривой

                            var startInd = Curves[cInd].X.First() - delta;

                            if (startInd < 0)
                            {
                                startInd = 0;
                            }
                            var tempCurveLen = Curves[cInd].X.Count() + delta + 0 * 15;
                            //конец кривой
                            tempCurveLen = Curves[cInd].X.Count();

                            ///  if (tempCurveLen + startInd > result.Count())  вылетеет за границы массива при таком условии



                            Curves[cInd] = new NatureCurves { };
                            try
                            {
                                var tY = result.GetRange(startInd, tempCurveLen).ToList();
                                var tX = Enumerable.Range(startInd, tempCurveLen).ToList();
                                Curves[cInd].X.AddRange(tX);
                                Curves[cInd].Y.AddRange(tY);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("CurvescInD" + e.Message);
                            }

                        }
                        //var delta = 50;
                        if (tempCurveMaxY > 20 && Curves[cInd].X.Count() > 30)
                        {
                            //начало кривой

                            var startInd = Curves[cInd].X.First() - delta;

                            if (startInd < 0)
                            {
                                startInd = 0;
                            }
                            var tempCurveLen = Curves[cInd].X.Count() + delta;
                            //конец кривой


                            if (tempCurveLen + startInd > result.Count())
                            {
                                tempCurveLen = result.Count() - 1 - startInd;
                            }

                            Curves[cInd] = new NatureCurves { };


                            var tY = result.GetRange(startInd, tempCurveLen).ToList();
                            var tX = Enumerable.Range(startInd, tempCurveLen).ToList();

                            Curves[cInd].X.AddRange(tX);
                            Curves[cInd].Y.AddRange(tY);
                        }




                    }

                    var DopLinear = new List<int>();

                    //Обнуляем места без кривых
                    if (Curves.Count > 0)
                    {
                        CurveForLvl.AddRange(Curves);
                    }

                    if (CurveForLvl.Count() < 1)
                    {
                        for (int index = 0; index < resultCopy.Count(); index++)  //0=25
                        {
                            resultCopy[index] = 0.0;
                        }
                        result = resultCopy;
                    }

                    foreach (var item in Curves)
                    {

                        var ModifiedCurve = new List<double>();

                        //работа с кривой

                        var CurveMax = item.Y.Max(o => Math.Abs(o));
                        var dVD = 0.85;
                        var SelectedData = item.Y.Where(o => CurveMax * dVD < Math.Abs(o)).ToList();
                        SelectedData.Clear();

                        bool hasValidElement = false;

                        foreach (var o in item.Y)
                        {
                            if (hasValidElement && Math.Abs(o) < 1)
                            {
                                break; // Прерываем цикл, если уже нашли элемент и встретили элемент < 1
                            }

                            if (CurveMax * dVD < Math.Abs(o))
                            {
                                hasValidElement = true;
                                SelectedData.Add(o); // Добавляем только подходящие элементы
                            }
                        }




                        if (CurveMax <= 35)
                        {
                            SelectedData.Clear();
                            SelectedData = item.Y.Where(o => CurveMax * 0.8 < Math.Abs(o)).ToList();
                        }
                        //if (CurveMax > 35 && CurveMax < 45)
                        //{
                        //    SelectedData.Clear();
                        //    SelectedData = item.Y.Where(o => CurveMax * 0.8 < Math.Abs(o)).ToList();
                        //}


                        //if (CurveMax > 35 &&item.Y.Count()<100)
                        //{
                        //    SelectedData.Clear();
                        //    SelectedData = item.Y.Where(o => CurveMax * 0. < Math.Abs(o)).ToList();
                        //}


                        if (CurveMax < 30 && height == 10 && item.Y.Count() < 100)
                        {



                        }


                        if (CurveMax > 72 && height == 10 && item.Y.Count() > 150)
                        {



                        }

                        var avgSelD = SelectedData.Average();
                        var SrMax = (avgSelD + CurveMax) / 2;
                        var SelectedData_Hill = item.Y.Where(o => avgSelD * 1.1 < Math.Abs(o));
                        //if (SelectedData_Hill.Count() > 30 && SelectedData.Count()>700 && CurveMax- avgSelD > 10)
                        //{
                        //    SelectedData.Clear();
                        //    SelectedData = item.Y.Where(o => SrMax * 0.9 < Math.Abs(o)).ToList();

                        //}
                        //CurveMax = item.Y.Max(o => Math.Abs(o));


                        var Head1 = item.Y.IndexOf(SelectedData.First());
                        var HeadX = item.X[item.Y.IndexOf(SelectedData.First())];


                        //X кордината первой(Первой(First)) точки трапеции  
                        //Y кордината первой(Первой(First)) точки трапеции  
                        var Head2 = item.Y.IndexOf(SelectedData.Last());            //Y кордината второй(Последней(Last)) точки трапеции
                        var HeadX2 = item.X[Head2];

                        var Head10 = 0;
                        var HeadX0 = 0;

                        var Head20 = 0;            //Y кордината второй(Последней(Last)) точки трапеции
                        var HeadX20 = 0;
                        //   (dVD - 0.05) * CurveMax > Math.Abs(o)
                        var SelectedData_Yama = item.Y.Where(o =>
                                       (dVD - 0.05) * CurveMax > Math.Abs(o)
                    && item.Y.IndexOf(o) < Head2 - 20
                    && item.Y.IndexOf(o) > Head1 + 20);

                        var Head1_Yama = item.Y.Where(o =>
                                      (dVD - 0.05) * CurveMax > Math.Abs(o));
                        var Points_manyR_str_YamaX = new List<int> { };


                        var Points_manyR_str_YamaYY = new List<double> { };

                        var Points_manyR_str_YamaY_Head = new List<double> { };




                        var SY = new List<double> { };
                        var X1Y = new List<int> { };
                        var X2Y = new List<int> { };
                        var Y1Y = new List<int> { };
                        var Y2Y = new List<int> { };
                        var YY = new List<double> { };
                        var Sl = new List<double> { };
                        var X1L = new List<int> { };
                        var X2L = new List<int> { };
                        var Y1L = new List<int> { };
                        var Y2L = new List<int> { };
                        var XA = new List<int> { };
                        var YA = new List<int> { };
                        var KA = new List<int> { };
                        var AverK = new List<double> { };
                        var AverK0 = new List<double> { };
                        var CountAverFirst = new List<int> { };
                        var CountAverLast = new List<int> { };
                        var OrderAver = new List<int> { };
                        var CountAver = new List<double> { };
                        var YL = new List<double> { };
                        var SR = new List<double> { };
                        var X1R = new List<int> { };
                        var X2R = new List<int> { };
                        var Y1R = new List<int> { };
                        var Y2R = new List<int> { };
                        var YR = new List<double> { };
                        var CountRight = new List<int> { };
                        var CountLeft = new List<int> { };
                        var SelectedTest0 = item.Y.Where(o => (item.Y[item.Y.IndexOf(o)] > HeadX && HeadX2 > item.X[item.Y.IndexOf(o)] && Math.Abs(o) < 10)).ToList();
                        var SelectedTest = SelectedTest0.Select(Math.Abs).ToList();



                        var S_Left = new List<double> { };
                        var S_Right = new List<double> { };
                        var Points_manyR_str_RightX = new List<int> { };
                        var Points_manyR_str_LeftX = new List<int> { };

                        var Points_manyR_str_LeftY = new List<double> { };
                        var Points_manyR_str_RightY = new List<double> { };
                        var Points_manyR_str_LeftY_Head = new List<double> { };
                        var Points_manyR_str_RightY_Head = new List<double> { };

                        var flagLeft = 0;
                        var flagRight = 0;

                        //////////////////////
                        var Step = 0.01 * CurveMax;
                        var Del_step = 0.03 * CurveMax;

                        int N_st = (int)Math.Round((CurveMax) / Step);
                        int N_st2 = (int)Math.Round(1 / Step);
                        var SelectedDataY = new List<double> { };
                        var SelectedDataYnext = new List<double> { };
                        var SelectedDataX = new List<double> { };
                        var SelectedDataStr = new List<double> { };
                        var SelectedDataXnext = new List<double> { };
                        var SelecteddeltaX = 0;
                        var SelecteddeltaY = 0;
                        var Head1Y = 0;
                        var HeadX1Y = 0;
                        var Head2Y = 0;
                        var HeadX2Y = 0;
                        var Flag_Many_Yama = false;


                        //Y кордината второй(Последней(Last)) точки трапеции
                        if (Head1 == 0)
                        {
                            //  continue;
                        }                                                            //Y кордината первой(Первой(First)) точки трапеции  

                        var min_SelectedData = SelectedData.Select(Math.Abs).Min();


                        /////////////  Yama
                        ///
                        var SelectedData_Yama_Down = new List<Double>();
                        var SelectedData_After_Yama = new List<Double>();
                        var SelectedData_Before_Yama = new List<Double>();
                        var Full_Yama = new List<Double>();
                        var Full_Yama_Down1 = new List<Double>();
                        var Full_Yama_Down2 = new List<Double>();
                        var Full_Yama_Down3 = new List<Double>();
                        var Full_YamaUp = new List<Double>();
                        var Full_Yama_Down = new List<Double>();
                        var Full_Yama_DownNext = new List<Double>();
                        var Full_Yama_DownPrev = new List<Double>();
                        var First_Yama = new List<Double>();
                        var Last_Yama = new List<Double>();

                        var Full_Yama_Left = new List<Double>();
                        var Full_Yama_Right = new List<Double>();

                        var Full_Yama_Der1 = new List<Double>();
                        var Full_Yama_Der2 = new List<Double>();
                        var Full_Yamanext = new List<Double>();
                        // var SelectedData_Yama = new List<Double>();
                        var Yama = false;
                        ///////////////////
                        ///

                        //var Yama_Ind_Min = SelectedData.IndexOf( Min_SelectedData_Yama);



                        var Head1_Yama_Up = 0;
                        var HeadX_Yama_Up = 0;
                        var Head2_Yama_Up = 0;            //Y кордината второй(Последней(Last)) точки трапеции
                        var HeadX2_Yama_Up = 0;                                 //Y кордината второй(Последней(Last)) точки трапеции




                        var Head1_Yama_down = 0;
                        var HeadX_Yama_down = 0;
                        var Head2_Yama_down = 0;            //Y кордината второй(Последней(Last)) точки трапеции
                        var HeadX2_Yama_down = 0;
                        var countYama = 0;
                        var Min_SelectedData_Yama = 0;

                        var Yama_max_down = 0.0;


                        Min_SelectedData_Yama = 0;
                        var averSelect = SelectedData.Average();



                        countYama = SelectedData_Yama.Count();






                        //  Yama = true;

                        if (SelectedData.Count() < 70 && CurveMax > 35 && item.Y.Count() < 100 && height == 4)

                        {

                            SelectedData.Clear();

                            SelectedData = item.Y.Where(o => CurveMax * 0.9 < Math.Abs(o)).ToList();
                            Head1 = item.Y.IndexOf(SelectedData.First());
                            HeadX = item.X[item.Y.IndexOf(SelectedData.First())];
                            Head2 = item.Y.IndexOf(SelectedData.Last());            //Y кордината второй(Последней(Last)) точки трапеции
                            HeadX2 = item.X[Head2];

                            SelectedDataStr = item.Y.Where(o => CurveMax * 0.6 < Math.Abs(o)).ToList();
                            var Head1Str = item.Y.IndexOf(SelectedData.First());
                            var HeadXStr = item.X[item.Y.IndexOf(SelectedData.First())];
                            var Head2Str = item.Y.IndexOf(SelectedData.Last());            //Y кордината второй(Последней(Last)) точки трапеции
                            var HeadX2Str = item.X[Head2];
                            if (Head1Str > Head1 + 5)
                            {
                                Full_Yama.Clear();

                                Full_Yama = item.Y.Where(o => CurveMax * 0.7 < Math.Abs(o)
                                 && HeadX > item.X[item.Y.IndexOf(o)]
                               ).ToList();

                                Head1Str = item.Y.IndexOf(SelectedData.First());
                                HeadXStr = item.X[item.Y.IndexOf(SelectedData.First())];
                                Head2Str = item.Y.IndexOf(SelectedData.Last());            //Y кордината второй(Последней(Last)) точки трапеции
                                HeadX2Str = item.X[Head2];
                                //    SY.Add((HeadX1Y + HeadX2Y) / 2);
                                YR.Add(Full_Yama.Average());
                                X1R.Add(HeadXStr);
                                X2R.Add(HeadX2Str);
                                Y1R.Add(Head1Str);
                                Y2R.Add(Head2Str);
                                YL.Add(Full_Yama.Average());
                                X1L.Add(HeadXStr);
                                X2L.Add(HeadX2Str);
                                Y1L.Add(Head1Str);
                                Y2L.Add(Head2Str);



                            }

                            if (Head2Str > Head2 + 10)
                            {
                                Full_Yama.Clear();

                                Full_Yama = item.Y.Where(o => CurveMax * 0.7 < Math.Abs(o)
                                 && HeadX2 < item.X[item.Y.IndexOf(o)]
                            ).ToList();

                                Head1Str = item.Y.IndexOf(SelectedData.First());
                                HeadXStr = item.X[item.Y.IndexOf(SelectedData.First())];
                                Head2Str = item.Y.IndexOf(SelectedData.Last());            //Y кордината второй(Последней(Last)) точки трапеции
                                HeadX2Str = item.X[Head2];
                                //    SY.Add((HeadX1Y + HeadX2Y) / 2);


                            }


                        }

                        if (height == 10 && CurveMax < 25 && CurveMax > 20)

                        {

                        }

                        if (height == 4)

                        {

                        }

                        if (SelectedData.Count() < 10) continue;

                        if (SelectedData.Count() > 30 && CurveMax > 15 || SelectedData.Count() > 10 && Head1 < 25 && CurveMax > 15)
                        //  if (SelectedData_Yama.Count() > 200)
                        {
                            var mashtY = 1.0;
                            var del = 0.03 * CurveMax * mashtY;
                            int Nd = (int)Math.Round(CurveMax / del);
                            var del0 = 0.06 * CurveMax * mashtY;




                            var Nmin = 1;
                            if (SelectedData.Count() > 800)
                            {

                                del = 1;
                                Nd = (int)Math.Round(CurveMax / del);
                                del0 = 3;
                            }
                            //del = 1;
                            //Nd = (int)Math.Round(CurveMax / del);
                            //del0 = 3;
                            var start = 0;

                            CountAverLast.Clear();
                            CountAverFirst.Clear();
                            var FinY = false;

                            var srIn = 0.0;

                            var srOut = 0.0;

                            var countY = 0;
                            for (int k = 1; k < Nd + 3; k++)

                            {



                                Full_Yama_Down.Clear();
                                Full_Yama_DownNext.Clear();
                                Full_Yama_DownPrev.Clear();

                                var ss = 0;
                                var ssPrev = 0;
                                var ssNext = 0;
                                start = 0;







                                Full_Yama_Down.Clear();
                                Full_Yama_DownNext.Clear();
                                Full_Yama_DownPrev.Clear();
                                Full_Yama_Down.AddRange(item.Y.Where(o =>
                                 Math.Abs(o - k * del * Math.Sign(o)) < del0 / 2
                             && item.X[item.Y.IndexOf(o)] < HeadX2
                             && item.X[item.Y.IndexOf(o)] > HeadX));
                                Full_Yama_DownNext.AddRange(item.Y.Where(o =>
                                 Math.Abs(o - (k + 1) * del * Math.Sign(o)) < del0 / 2
                             && item.X[item.Y.IndexOf(o)] < HeadX2
                             && item.X[item.Y.IndexOf(o)] > HeadX));
                                Full_Yama_DownPrev.AddRange(item.Y.Where(o =>
                                 Math.Abs(o - (k - 1) * del * Math.Sign(o)) < del0 / 2
                             && item.X[item.Y.IndexOf(o)] < HeadX2
                             && item.X[item.Y.IndexOf(o)] > HeadX));
                                if (

                                        Full_Yama_Down.Count() > 10
                                        &&
                                        Full_Yama_DownNext.Count() < Full_Yama_Down.Count() && Full_Yama_DownPrev.Count() < Full_Yama_Down.Count()



                                        )
                                {


                                    if (!AverK.Any())
                                    {
                                        AverK.Add(Full_Yama_Down.Average());
                                        XA.Add(Full_Yama_Down.Count());
                                        KA.Add(k);
                                        CountAver.Add(Full_Yama_Down.Count());


                                        CountAverLast.Add(item.Y.IndexOf(Full_Yama_Down.Last()));
                                        CountAverFirst.Add(item.Y.IndexOf(Full_Yama_Down.First()));
                                    }
                                    //  if (AverK.Any() && Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average()) > del0)

                                    if (AverK.Any() && Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average()) >= 1.0 / 2.0 * del0

                                    //  || AverK.Any() && Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average()) < 2 * del0
                                    //  XA[AverK.Count() - 1] - Full_Yama_Down.Count() < 0
                                    )
                                    //  XA.CountAverK[AverK.Count() - 1]- Full_Yama_Down.Count()
                                    // Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average())
                                    {

                                        AverK.Add(Full_Yama_Down.Average());
                                        XA.Add(Full_Yama_Down.Count());
                                        KA.Add(k);
                                        CountAver.Add(Full_Yama_Down.Count());
                                        CountAverLast.Add(item.Y.IndexOf(Full_Yama_Down.Last()));
                                        CountAverFirst.Add(item.Y.IndexOf(Full_Yama_Down.First()));

                                    }




                                    if (AverK.Any()
                                        && Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average()) < 1.0 / 2.0 * del0
                                    && 2 * XA[AverK.Count() - 1] - Full_Yama_Down.Count() < 0
                                   && XA[AverK.Count() - 1] < 50
                                         )
                                    {

                                        AverK[AverK.Count() - 1] =
                                                (Full_Yama_Down.Average() * Full_Yama_Down.Count() + AverK[AverK.Count() - 1])
                                                / (Full_Yama_Down.Count() + XA[AverK.Count() - 1]);
                                        XA[AverK.Count() - 1] = Full_Yama_Down.Count();
                                        KA[AverK.Count() - 1] = k;
                                        CountAver[AverK.Count() - 1] = Full_Yama_Down.Count();
                                    }






                                }




                                //}







                            }



                            if (AverK.Count() > 1)
                            {

                                if (Math.Abs(AverK[1] - AverK[0]) < 2)
                                {

                                    AverK.RemoveAt(1);

                                    XA.RemoveAt(1);

                                    KA.RemoveAt(1);
                                }
                            }

                            if (AverK.Count() > 2)
                            {

                                if (Math.Abs(AverK[2] - AverK[1]) < 2)
                                {

                                    AverK.RemoveAt(2);

                                    XA.RemoveAt(2);

                                    KA.RemoveAt(2);
                                }
                            }




                            ////////////////////////




                            //////////////////////

                            int ks = 0;

                            start = 0;



                            start = 0;





                            start = 0;

                            var FlafSt = false;
                            var FlafFin = false;

                            var shortTrees = new List<int> { };
                            //var Short = item.Y.Count();

                            //Head1_Yama_Up = item.Y.IndexOf(SelectedData_Yama.First());

                            //Head2_Yama_Up = item.Y.IndexOf(SelectedData_Yama.Last());


                            start = 0;
                            int k_f1 = 100;
                            int k_f2 = 100;
                            if (Head2 - Head1 > 1000 && CurveMax < 65 && CurveMax > 60 && height == 4)

                            {


                            }
                            var countYY = 0;
                            for (int z = Head1 + 6; z < Head2 - 6; z++)
                            {
                                srIn = 0;
                                srOut = 0;

                                //for (int t = -5; t < 5; t++)
                                //{

                                //    srIn = srIn + item.Y[z + t] / 10;
                                //}



                                //for (int t = -5; t < 5; t++)
                                //{

                                //    srOut = srOut + item.Y[z + t + 5] / 10;
                                //}

                                for (int t = 0; t < KA.Count(); t++)
                                {





                                    if ((k_f2 == 100 && k_f1 == 100 && (Math.Abs(item.Y[z] - AverK[t]) < del0
                                        && Math.Abs(item.Y[z + 5] - AverK[t]) < del0
                                        || Math.Abs(item.Y[z] - AverK[t]) < del0
                                        && Math.Abs(item.Y[z + 5] - AverK[t]) < del0)
                                         && (Math.Abs(item.Y[z + 5] - item.Y[z - 5]) < del0)

                                       //  ||  (k_f1 == Nd && z < Head1 + 10)
                                       )

                                         && start == 0)


                                    // && Math.Abs(item.Y[z] - item.Y[z - 10]) > 2 * Math.Abs(item.Y[z] - item.Y[z + 10]))

                                    {


                                        start = 1;

                                        k_f1 = t;
                                        Full_Yama_Down.Clear();
                                    }




                                }



                                if (start == 1 && (Math.Abs(item.Y[z + 5] - item.Y[z - 5]) < 2 * del0))
                                {
                                    countY = countY + 1;
                                    if (countY > 2) Full_Yama_Down.Add(item.Y[z]);

                                }




                                for (int t = 0; t < KA.Count(); t++)
                                {


                                    if (Full_Yama_Down.Count() > 15
                                        && start == 1
                                        && ((Math.Abs(item.Y[z + 5] - AverK[t]) < del0 / 2

                                        && k_f1 != t && k_f1 != 100
                                         && k_f1 != t
                                         && KA.Count() > 1
                                        ||
                                         Full_Yama_Down.Count() > 15
                                        && start == 1
                                       && Math.Abs(item.Y[z + 5] - AverK[t]) > del0
                                           && Math.Abs(item.Y[z - 5] - AverK[t]) > del0
                                   && k_f1 != 100

                                         && KA.Count() == 1

                                        )
                                          //  && 2 * Math.Abs(item.Y[z] - item.Y[z - 10]) <  Math.Abs(item.Y[z] - item.Y[z + 10])
                                          || z > Head2 - 30 && start == 1




                                          )

                                        )

                                    {
                                        FinY = true;
                                        k_f2 = t;
                                    }
                                }

                                if (k_f1 == Nd && z < Head1 + 10)


                                {
                                    start = 1;

                                    countY = 3;
                                }

                                if (FinY && Full_Yama_Down.Count() > 10 && FinY && k_f2 != 100 && k_f1 != 100
                                                                          )

                                {

                                    countY = 0;

                                    start = 0;
                                    Full_Yama.Clear();


                                    Head1Y = item.Y.IndexOf(Full_Yama_Down.First());
                                    HeadX1Y = item.X[Head1Y];
                                    Head2Y = item.Y.IndexOf(Full_Yama_Down.Last());
                                    HeadX2Y = item.X[Head2Y];
                                    Full_Yama.AddRange(item.Y.Where(o =>
                                      item.X[item.Y.IndexOf(o)] <= HeadX2Y
                                     && item.X[item.Y.IndexOf(o)] >= HeadX1Y
                                       && Math.Abs(o - AverK[k_f1]) <= del0)
                                            );
                                    if (YY.Count() > 0) if (HeadX2Y - HeadX1Y > 10 || X2Y[YY.Count() - 1] - X2Y[YY.Count() - 1] > 10 && HeadX2Y - HeadX1Y > 10)
                                        {
                                            Head1Y = item.Y.IndexOf(Full_Yama.First());
                                            HeadX1Y = item.X[Head1Y];
                                            Head2Y = item.Y.IndexOf(Full_Yama.Last());
                                            HeadX2Y = item.X[Head2Y];
                                            SY.Add((HeadX1Y + HeadX2Y) / 2);
                                            YY.Add(Full_Yama.Average());
                                            X1Y.Add(HeadX1Y);
                                            X2Y.Add(HeadX2Y);
                                            Y1Y.Add(Head1Y);
                                            Y2Y.Add(Head2Y);
                                        }
                                    if (YY.Count() == 0) if (HeadX2Y - HeadX1Y > 15)
                                        {
                                            Head1Y = item.Y.IndexOf(Full_Yama.First());
                                            HeadX1Y = item.X[Head1Y];
                                            Head2Y = item.Y.IndexOf(Full_Yama.Last());
                                            HeadX2Y = item.X[Head2Y];
                                            SY.Add((HeadX1Y + HeadX2Y) / 2);
                                            YY.Add(Full_Yama.Average());
                                            X1Y.Add(HeadX1Y);
                                            X2Y.Add(HeadX2Y);
                                            Y1Y.Add(Head1Y);
                                            Y2Y.Add(Head2Y);
                                        }
                                    // AverK[k_f1] = 10000;
                                    Full_Yama_Down.Clear();
                                    Full_Yama.Clear();
                                    k_f1 = 100;
                                    FinY = false;
                                    k_f2 = 100;

                                }






                            }













                        }













                        var iii_0 = 0;

                        if (YY.Any())
                        {
                            Yama = true;
                            //        //////////////////////  z0
                            var x1 = 0;
                            var x2 = 0;
                            var x0 = 0.0;




                            if (Y2Y.Any())
                            {

                                // if (Head2 - Y2Y.Max() > 25)
                                if (Head2 - Y2Y.Max() > 25)
                                {

                                    Full_Yama.Clear();


                                    Head1Y = Y2Y.Max();
                                    HeadX1Y = item.X[Head1Y];
                                    Head2Y = Head2;
                                    HeadX2Y = item.X[Head2Y];
                                    Full_Yama.AddRange(item.Y.Where(o =>
                                      item.X[item.Y.IndexOf(o)] <= HeadX2Y
                                     && item.X[item.Y.IndexOf(o)] >= HeadX1Y
                                       )
                                            );
                                    Full_Yama_Down.Clear();



                                    Full_Yama_Down.AddRange(item.Y.Where(o =>
                                      item.X[item.Y.IndexOf(o)] <= HeadX2Y
                                     && Math.Abs(o - Full_Yama.Average()) <= 5
                                     && item.X[item.Y.IndexOf(o)] >= HeadX1Y
                                       )
                                            );


                                    if (Full_Yama_Down.Count() > 30)
                                    {
                                        Head1Y = item.Y.IndexOf(Full_Yama_Down.First());
                                        HeadX1Y = item.X[Head1Y];
                                        Head2Y = item.Y.IndexOf(Full_Yama_Down.Last());
                                        HeadX2Y = item.X[Head2Y];
                                        SY.Add((HeadX1Y + HeadX2Y) / 2);
                                        YY.Add(Full_Yama_Down.Average());
                                        X1Y.Add(HeadX1Y);
                                        X2Y.Add(HeadX2Y);
                                        Y1Y.Add(Head1Y);
                                        Y2Y.Add(Head2Y);
                                    }
                                }





                                if (Y1Y.Min() - Head1 > 30)
                                // if (Y1Y.Min() - Head1 > 30 )//| Y1Y.Min() - Head1 > 10 && item.Y.Count() > 1000)
                                {

                                    Full_Yama.Clear();


                                    Head1Y = Head1;
                                    HeadX1Y = item.X[Head1Y];
                                    Head2Y = Y1Y.Min();

                                    HeadX2Y = item.X[Head2Y];
                                    Full_Yama.AddRange(item.Y.Where(o =>
                                      item.X[item.Y.IndexOf(o)] <= HeadX2Y
                                     && item.X[item.Y.IndexOf(o)] >= HeadX1Y
                                       )
                                            );
                                    Full_Yama_Down.Clear();
                                    Full_Yama_Down.AddRange(item.Y.Where(o =>
                                      item.X[item.Y.IndexOf(o)] <= HeadX2Y
                                     && Math.Abs(o - Full_Yama.Average()) <= 5
                                     && item.X[item.Y.IndexOf(o)] >= HeadX1Y
                                       )
                                            );

                                    if (Full_Yama_Down.Count() > 30)
                                    {

                                        Head1Y = item.Y.IndexOf(Full_Yama_Down.First());
                                        HeadX1Y = item.X[Head1Y];
                                        Head2Y = item.Y.IndexOf(Full_Yama_Down.Last());
                                        HeadX2Y = item.X[Head2Y];
                                        SY.Add((HeadX1Y + HeadX2Y) / 2);
                                        YY.Add(Full_Yama_Down.Average());
                                        X1Y.Add(HeadX1Y);
                                        X2Y.Add(HeadX2Y);
                                        Y1Y.Add(Head1Y);
                                        Y2Y.Add(Head2Y);
                                    }
                                }

                                //if (YY.Count() == 1)
                                //{
                                //    if (Y1Y.Min() - Head1 < 33 && Head2 - Y2Y.Max() < 33)

                                //    {

                                //    Y2Y.Clear();
                                //    Y1Y.Clear();
                                //    YY.Clear();
                                //    X2Y.Clear();
                                //    X1Y.Clear();
                                //    SY.Clear();
                                //    }
                                //           }
                            }




                            for (int j = 0; j < YY.Count; j++)
                            {


                                for (int i5 = j + 1; i5 < YY.Count; i5++)
                                {

                                    if (X1Y[i5] < X1Y[j])
                                    {
                                        x1 = X1Y[j];
                                        X1Y[j] = X1Y[i5];
                                        X1Y[i5] = x1;
                                        x1 = X2Y[j];
                                        X2Y[j] = X2Y[i5];
                                        X2Y[i5] = x1;
                                        x1 = Y1Y[j];
                                        Y1Y[j] = Y1Y[i5];
                                        Y1Y[i5] = x1;
                                        x1 = Y2Y[j];
                                        Y2Y[j] = Y2Y[i5];
                                        Y2Y[i5] = x1;
                                        x0 = YY[j];
                                        YY[j] = YY[i5];
                                        YY[i5] = x0;

                                    }

                                }
                            }

                            var countPl = 0;
                            var countMn = 0;
                            var JPl = 0;
                            var JMn = 0;

                            for (int j = 0; j < YY.Count(); j++)
                            {

                                Points_manyR_str_YamaY_Head.Add(Y1Y[j]);
                                Points_manyR_str_YamaY_Head.Add(Y2Y[j]);
                                Points_manyR_str_YamaX.Add(X1Y[j]);
                                Points_manyR_str_YamaX.Add(X2Y[j]);
                                Points_manyR_str_YamaYY.Add(YY[j]);
                                Points_manyR_str_YamaYY.Add(YY[j]);
                            }
                        }






                        /////////////////////






                        var ResultTestCurveLeft = new List<ResultTestCurveLeft>();
                        var ResultTestCurveRiht = new List<ResultTestCurveRiht>();


                        ///////////////  Start New Left




                        var Head1L = 0;
                        var HeadX1L = 0;
                        var Head2L = 0;
                        var HeadX2L = 0;


                        if (Head1 > 20 && CurveMax > 5)
                        //  if (SelectedData_Yama.Count() > 200)
                        {

                            var del = 0.01 * CurveMax;
                            int Nd = (int)Math.Round(CurveMax / del);
                            var del0 = 0.03 * CurveMax;




                            var Nmin = 1;


                            var start = 0;

                            CountAverLast.Clear();
                            CountAverFirst.Clear();
                            AverK.Clear();
                            XA.Clear();
                            KA.Clear();
                            CountAver.Clear();
                            var FinY = false;

                            var srIn = 0.0;

                            var srOut = 0.0;

                            var countY = 0;
                            for (int k = Nd + 2; k >= 0; k--)

                            {



                                Full_Yama_Down.Clear();
                                Full_Yama_DownNext.Clear();
                                Full_Yama_DownPrev.Clear();

                                var ss = 0;
                                var ssPrev = 0;
                                var ssNext = 0;
                                start = 0;







                                Full_Yama_Down.Clear();
                                Full_Yama_DownNext.Clear();
                                Full_Yama_DownPrev.Clear();
                                Full_Yama_Down.AddRange(item.Y.Where(o =>
                                 Math.Abs(o - k * del * Math.Sign(o)) < del0 / 2

                             && item.X[item.Y.IndexOf(o)] < HeadX));


                                Full_Yama_DownNext.AddRange(item.Y.Where(o =>
                                 Math.Abs(o - (k + 1) * del * Math.Sign(o)) < del0 / 2
                                                          && item.X[item.Y.IndexOf(o)] < HeadX));


                                Full_Yama_DownPrev.AddRange(item.Y.Where(o =>
                                 Math.Abs(o - (k - 1) * del * Math.Sign(o)) < del0 / 2
                                                 && item.X[item.Y.IndexOf(o)] < HeadX));
                                if (

                                        Full_Yama_Down.Count() > 10
                                        &&
                                       Full_Yama_DownPrev.Count() < Full_Yama_Down.Count()



                                        )
                                {
                                    if (CurveMax > 79 && CurveMax < 85)
                                    {


                                    }

                                    if (!AverK.Any())
                                    {
                                        AverK.Add(Full_Yama_Down.Average());
                                        XA.Add(Full_Yama_Down.Count());
                                        KA.Add(k);
                                        CountAver.Add(Full_Yama_Down.Count());


                                        CountAverLast.Add(item.Y.IndexOf(Full_Yama_Down.Last()));
                                        CountAverFirst.Add(item.Y.IndexOf(Full_Yama_Down.First()));
                                    }
                                    //  if (AverK.Any() && Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average()) > del0)

                                    if (AverK.Any() && Math.Abs(AverK.Last() - Full_Yama_Down.Average()) >= 1 / 2 * del0 ///2 

                                    //  || AverK.Any() && Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average()) < 2 * del0
                                    //  XA[AverK.Count() - 1] - Full_Yama_Down.Count() < 0
                                    )
                                    //  XA.CountAverK[AverK.Count() - 1]- Full_Yama_Down.Count()
                                    // Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average())
                                    {

                                        AverK.Add(Full_Yama_Down.Average());
                                        XA.Add(Full_Yama_Down.Count());
                                        KA.Add(k);
                                        CountAver.Add(Full_Yama_Down.Count());
                                        CountAverLast.Add(item.Y.IndexOf(Full_Yama_Down.Last()));
                                        CountAverFirst.Add(item.Y.IndexOf(Full_Yama_Down.First()));

                                    }




                                    if (AverK.Any()
                                        &&


                                         Math.Abs(AverK.Last() - Full_Yama_Down.Average()) < 1.0 / 2 * del0

                                         //  && 2 * XA[AverK.Count() - 1] - Full_Yama_Down.Count() < 0
                                         //  && XA[AverK.Count() - 1] < 50
                                         )
                                    {

                                        AverK[AverK.Count() - 1] =
                                                Full_Yama_Down.Average();
                                        XA[AverK.Count() - 1] = Full_Yama_Down.Count();
                                        KA[AverK.Count() - 1] = k;
                                        CountAver[AverK.Count() - 1] = Full_Yama_Down.Count();
                                    }




                                }




                                //}

                                if (AverK.Count() > 1)
                                {

                                    if (Math.Abs(AverK[AverK.Count() - 2] - AverK[AverK.Count() - 1]) < 1.0 * del0 / 2)
                                    {

                                        AverK.RemoveAt(AverK.Count() - 1);

                                        XA.RemoveAt(AverK.Count() - 1);

                                        KA.RemoveAt(AverK.Count() - 1);
                                    }
                                }

                            }



                            if (AverK.Count() > 1)
                            {

                                if (Math.Abs(AverK[1] - AverK[0]) < 1.0 * del0 / 2)
                                {

                                    AverK.RemoveAt(1);

                                    XA.RemoveAt(1);

                                    KA.RemoveAt(1);
                                }
                            }

                            if (CurveMax > 79 && CurveMax < 85)
                            {


                            }





                            int ks = 0;

                            start = 0;




                            var FlafSt = false;
                            var FlafFin = false;

                            var shortTrees = new List<int> { };

                            if (height == 4 && CurveMax > 50 && CurveMax < 61 && item.Y.Count() > 600 && item.Y.Count() < 780)
                            {

                            }

                            start = 0;
                            int k_f1 = 100;
                            int k_f2 = 100;

                            //var countYY = 0;
                            for (int z = 6; z < Head1 - 6; z++)
                            {
                                srIn = 0;
                                srOut = 0;

                                if (Head1 < 20) break;

                                for (int t = 0; t < AverK.Count(); t++)
                                {





                                    if ((k_f2 == 100 && k_f1 == 100 && (Math.Abs(item.Y[z] - AverK[t]) < del0 / 2

                                        ||
                                        Math.Abs(item.Y[z + 5] - AverK[t]) < del0
                                                                   && Math.Abs(item.Y[z - 5] - item.Y[z + 5]) < del0
                                           ||
                                              Math.Abs(item.Y[z] - AverK[t]) < del0


                                         )

                                       //  ||  (k_f1 == Nd && z < Head1 + 10)
                                       )

                                         && start == 0)




                                    {


                                        start = 1;

                                        k_f1 = t;
                                        Full_Yama_Down.Clear();
                                    }




                                }



                                if (start == 1 && (Math.Abs(item.Y[z + 5] - item.Y[z - 5]) < 2 * del0))
                                {
                                    countY = countY + 1;
                                    if (countY > 2) Full_Yama_Down.Add(item.Y[z]);

                                }




                                for (int t = 0; t < AverK.Count(); t++)
                                {


                                    if (Full_Yama_Down.Count() > 15
                                        && start == 1
                                        && ((
                                        Math.Abs(item.Y[z + 5] - AverK[t]) < del0 / 2

                                        && k_f1 != t && k_f2 != 100
                                         && k_f2 != t
                                        )
                                          //  && 2 * Math.Abs(item.Y[z] - item.Y[z - 10]) <  Math.Abs(item.Y[z] - item.Y[z + 10])
                                          || z > Head1 - 30
                                        && k_f1 != t && k_f1 != 100
                                         && k_f1 != t
                                         && start == 1
                                         ||
                                       k_f1 != 100
                                         && k_f2 != t
                                         && start == 1
                                         &&
                                         (Math.Abs(item.Y[z] - AverK[k_f1]) > 2 * del0
                                         || Math.Abs(item.Y[z] - AverK[k_f1]) > del0 && Math.Abs(item.Y[z - 5] - item.Y[z + 5]) > 2 * del0
                                         || Math.Abs(item.Y[z - 5] - AverK[k_f1]) > del0 && Math.Abs(item.Y[z - 5] - item.Y[z + 5]) > 2 * del0
                                         || Math.Abs(item.Y[z + 5] - AverK[k_f1]) > del0 && Math.Abs(item.Y[z - 5] - item.Y[z + 5]) > 2 * del0
                                              ||
                                       k_f1 != 100
                                         && k_f2 != t
                                         && start == 1
                                        && Full_Yama_Down.Count() > 25
                                                        )))



                                    {
                                        FinY = true;
                                        k_f2 = t;
                                    }
                                }

                                if (
                                    Full_Yama_Down.Count() > 20

                                       &&
                                       start == 1
                                       &&
                                       Math.Abs(item.Y[z]) < 8
                                            && k_f1 != 100

                                       )
                                {
                                    FinY = true;
                                    k_f2 = k_f1;
                                }

                                if (FinY && Full_Yama_Down.Count() > 10 && FinY && k_f2 != 100 && k_f1 != 100
                                                                          )

                                {

                                    countY = 0;

                                    start = 0;
                                    Full_Yama.Clear();


                                    Head1L = item.Y.IndexOf(Full_Yama_Down.First());
                                    HeadX1L = item.X[Head1L];
                                    Head2L = item.Y.IndexOf(Full_Yama_Down.Last());
                                    HeadX2L = item.X[Head2L];

                                    Full_Yama.AddRange(item.Y.Where(o =>

                                      item.X[item.Y.IndexOf(o)] <= HeadX2L
                                     &&
                                     item.X[item.Y.IndexOf(o)] >= HeadX1L
                                       &&
                                       Math.Abs(o - AverK[k_f1]) <= 1.5 * del0)

                                            );
                                    if (YL.Count() > 0 && Full_Yama.Count() > 0) if (HeadX2L - HeadX1L > 10 || X2L[YL.Count() - 1] - X2L[YL.Count() - 1] > 10 && HeadX2L - HeadX1L > 10)



                                        {
                                            Head1L = item.Y.IndexOf(Full_Yama.First());
                                            HeadX1L = item.X[Head1L];
                                            Head2L = item.Y.IndexOf(Full_Yama.Last());
                                            HeadX2L = item.X[Head2L];
                                            //    SY.Add((HeadX1Y + HeadX2Y) / 2);
                                            YL.Add(Full_Yama.Average());
                                            X1L.Add(HeadX1L);
                                            X2L.Add(HeadX2L);
                                            Y1L.Add(Head1L);
                                            Y2L.Add(Head2L);
                                        }
                                    if (YL.Count() == 0 && Full_Yama.Count() > 0) if (HeadX2L - HeadX1L > 10)
                                        {
                                            Head1L = item.Y.IndexOf(Full_Yama.First());
                                            HeadX1L = item.X[Head1L];
                                            Head2L = item.Y.IndexOf(Full_Yama.Last());
                                            HeadX2L = item.X[Head2L];
                                            // SY.Add((HeadX1Y + HeadX2Y) / 2);
                                            YL.Add(Full_Yama.Average());
                                            X1L.Add(HeadX1L);
                                            X2L.Add(HeadX2L);
                                            Y1L.Add(Head1L);
                                            Y2L.Add(Head2L);
                                        }
                                    // AverK[k_f1] = 10000;
                                    Full_Yama_Down.Clear();
                                    Full_Yama.Clear();
                                    k_f1 = 100;
                                    FinY = false;
                                    k_f2 = 100;

                                }






                            }













                        }








                        ////////////////////  final new Left














                        /// New right




                        /////////////  Start New Right


                        var Head1R = 0;
                        var HeadX1R = 0;
                        var Head2R = 0;
                        var HeadX2R = 0;

                        if (item.Y.Count() - Head2 > 30 && CurveMax > 10)
                        //  if (SelectedData_Yama.Count() > 200)
                        {

                            var del = 0.011 * CurveMax;
                            int Nd = (int)Math.Round(CurveMax / del);
                            var del0 = 0.033 * CurveMax;




                            var Nmin = 1;


                            var start = 0;

                            CountAverLast.Clear();
                            CountAverFirst.Clear();
                            AverK.Clear();
                            XA.Clear();
                            KA.Clear();
                            CountAver.Clear();
                            var FinY = false;

                            var srIn = 0.0;

                            var srOut = 0.0;

                            var countY = 0;
                            for (int k = Nd + 2; k >= 0; k--)

                            {



                                Full_Yama_Down.Clear();
                                Full_Yama_DownNext.Clear();
                                Full_Yama_DownPrev.Clear();

                                var ss = 0;
                                var ssPrev = 0;
                                var ssNext = 0;
                                start = 0;







                                Full_Yama_Down.Clear();
                                Full_Yama_DownNext.Clear();
                                Full_Yama_DownPrev.Clear();
                                Full_Yama_Down.AddRange(item.Y.Where(o =>
                                 Math.Abs(o - k * del * Math.Sign(o)) < del0 / 2

                             && item.X[item.Y.IndexOf(o)] > HeadX2));


                                Full_Yama_DownNext.AddRange(item.Y.Where(o =>
                                 Math.Abs(o - (k + 1) * del * Math.Sign(o)) < del0 / 2
                                                          && item.X[item.Y.IndexOf(o)] > HeadX2));


                                Full_Yama_DownPrev.AddRange(item.Y.Where(o =>
                                 Math.Abs(o - (k - 1) * del * Math.Sign(o)) < del0 / 2
                                                 && item.X[item.Y.IndexOf(o)] > HeadX2));
                                if (

                                        Full_Yama_Down.Count() > 10
                                        &&
                                       Full_Yama_DownPrev.Count() < Full_Yama_Down.Count()



                                        )
                                {


                                    if (!AverK.Any())
                                    {
                                        AverK.Add(Full_Yama_Down.Average());
                                        XA.Add(Full_Yama_Down.Count());
                                        KA.Add(k);
                                        CountAver.Add(Full_Yama_Down.Count());


                                        CountAverLast.Add(item.Y.IndexOf(Full_Yama_Down.Last()));
                                        CountAverFirst.Add(item.Y.IndexOf(Full_Yama_Down.First()));
                                    }
                                    //  if (AverK.Any() && Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average()) > del0)

                                    if (AverK.Any() && Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average()) >= 1 / 2 * del0 ///2 

                                    //  || AverK.Any() && Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average()) < 2 * del0
                                    //  XA[AverK.Count() - 1] - Full_Yama_Down.Count() < 0
                                    )
                                    //  XA.CountAverK[AverK.Count() - 1]- Full_Yama_Down.Count()
                                    // Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average())
                                    {

                                        AverK.Add(Full_Yama_Down.Average());
                                        XA.Add(Full_Yama_Down.Count());
                                        KA.Add(k);
                                        CountAver.Add(Full_Yama_Down.Count());
                                        CountAverLast.Add(item.Y.IndexOf(Full_Yama_Down.Last()));
                                        CountAverFirst.Add(item.Y.IndexOf(Full_Yama_Down.First()));

                                    }




                                    if (AverK.Any()
                                        && Math.Abs(AverK[AverK.Count() - 1] - Full_Yama_Down.Average()) < 1.0 / 2 * del0
                                   //  && 2 * XA[AverK.Count() - 1] - Full_Yama_Down.Count() < 0
                                   && XA[AverK.Count() - 1] < 50
                                         )
                                    {

                                        AverK[AverK.Count() - 1] =
                                                (Full_Yama_Down.Average() * Full_Yama_Down.Count() + AverK[AverK.Count() - 1])
                                                / (Full_Yama_Down.Count() + XA[AverK.Count() - 1]);
                                        XA[AverK.Count() - 1] = Full_Yama_Down.Count();
                                        KA[AverK.Count() - 1] = k;
                                        CountAver[AverK.Count() - 1] = Full_Yama_Down.Count();
                                    }



                                }




                                //}






                                //if (AverK.Count() > 1)
                                //{

                                //    if (Math.Abs(AverK[AverK.Count() - 2] - AverK[AverK.Count() - 1]) < 3)
                                //    {

                                //        AverK.RemoveAt(AverK.Count() - 1);

                                //        XA.RemoveAt(AverK.Count() - 1);

                                //        KA.RemoveAt(AverK.Count() - 1);
                                //    }
                                //}

                            }



                            if (AverK.Count() > 1)
                            {

                                if (Math.Abs(AverK[1] - AverK[0]) < 3)
                                {

                                    AverK.RemoveAt(1);

                                    XA.RemoveAt(1);

                                    KA.RemoveAt(1);
                                }
                            }





                            int ks = 0;

                            start = 0;




                            var FlafSt = false;
                            var FlafFin = false;

                            var shortTrees = new List<int> { };

                            if (height == 4 && CurveMax > 50 && CurveMax < 61 && item.Y.Count() > 600 && item.Y.Count() < 780)
                            {

                            }

                            start = 0;
                            int k_f1 = 100;
                            int k_f2 = 100;

                            //var countYY = 0;
                            for (int z = Head2 + 6; z < item.Y.Count() - 6; z++)
                            {
                                srIn = 0;
                                srOut = 0;

                                if (item.Y.Count() - Head2 < 10) break;

                                for (int t = 0; t < KA.Count(); t++)
                                {





                                    if ((k_f2 == 100 && k_f1 == 100 && (Math.Abs(item.Y[z] - AverK[t]) < del0
                                        && Math.Abs(item.Y[z + 5] - AverK[t]) < del0
                                        ||
                                        Math.Abs(item.Y[z] - AverK[t]) < del0
                                           && Math.Abs(item.Y[z - 5] - AverK[t]) < del0
                                             ||

                                        Math.Abs(item.Y[z] - AverK[t]) < del0 / 2
                                                                            ||

                                        Math.Abs(item.Y[z] - AverK[t]) < del0
                                           && Math.Abs(item.Y[z - 5] - item.Y[z + 5]) < del0

                                         )

                                       //  ||  (k_f1 == Nd && z < Head1 + 10)
                                       )

                                         && start == 0)




                                    {


                                        start = 1;

                                        k_f1 = t;
                                        Full_Yama_Down.Clear();
                                    }




                                }



                                if (start == 1 && (Math.Abs(item.Y[z + 5] - item.Y[z - 5]) < 2 * del0))
                                {
                                    countY = countY + 1;
                                    if (countY > 2) Full_Yama_Down.Add(item.Y[z]);

                                }




                                for (int t = 0; t < KA.Count(); t++)
                                {


                                    if (Full_Yama_Down.Count() > 15
                                        && start == 1
                                        && ((
                                        Math.Abs(item.Y[z + 5] - AverK[t]) < 0.9 * del0 / 2

                                        && k_f1 != t && k_f2 != 100
                                         && k_f2 != t
                                        )
                                          //  && 2 * Math.Abs(item.Y[z] - item.Y[z - 10]) <  Math.Abs(item.Y[z] - item.Y[z + 10])
                                          || z > item.Y.Count() - 30
                                        && k_f1 != t && k_f1 != 100
                                         && k_f1 != t
                                         && start == 1
                                         ||
                                       k_f1 != 100
                                         && k_f2 != t
                                         && start == 1
                                         && Math.Abs(item.Y[z] - AverK[k_f1]) > 2.5 * del0
                                               ||
                                       k_f1 != 100
                                         && k_f2 != t
                                         && start == 1
                                         && Math.Abs(item.Y[z] - AverK[k_f1]) > 2 * del0 && Full_Yama_Down.Count() > 25

                                          )

                                        )

                                    {
                                        FinY = true;
                                        k_f2 = t;
                                    }
                                }

                                if (
                                    Full_Yama_Down.Count() > 40

                                       &&
                                       start == 1
                                       &&
                                       Math.Abs(item.Y[z]) < 8
                                            && k_f1 != 100

                                       )
                                {
                                    FinY = true;
                                    k_f2 = k_f1;
                                }

                                if (FinY && Full_Yama_Down.Count() > 10 && FinY && k_f2 != 100 && k_f1 != 100
                                                                          )

                                {

                                    countY = 0;

                                    start = 0;
                                    Full_Yama.Clear();


                                    Head1R = item.Y.IndexOf(Full_Yama_Down.First());
                                    HeadX1R = item.X[Head1R];
                                    Head2R = item.Y.IndexOf(Full_Yama_Down.Last());
                                    HeadX2R = item.X[Head2R];

                                    Full_Yama.AddRange(item.Y.Where(o =>
                                      item.X[item.Y.IndexOf(o)] <= HeadX2R
                                     && item.X[item.Y.IndexOf(o)] >= HeadX1R
                                       && Math.Abs(o - AverK[k_f1]) <= 1.5 * del0)
                                            );
                                    if (YR.Count() > 0 && Full_Yama.Count() > 0) if (HeadX2R - HeadX1R > 20 || X2R[YR.Count() - 1] - X2R[YR.Count() - 1] > 20 && HeadX2R - HeadX1R > 20)

                                        {
                                            Head1R = item.Y.IndexOf(Full_Yama.First());
                                            HeadX1R = item.X[Head1R];
                                            Head2R = item.Y.IndexOf(Full_Yama.Last());
                                            HeadX2R = item.X[Head2R];
                                            //    SY.Add((HeadX1Y + HeadX2Y) / 2);
                                            YR.Add(Full_Yama.Average());
                                            X1R.Add(HeadX1R);
                                            X2R.Add(HeadX2R);
                                            Y1R.Add(Head1R);
                                            Y2R.Add(Head2R);
                                        }
                                    if (YR.Count() == 0 && Full_Yama.Count() > 0) if (HeadX2R - HeadX1R > 20)
                                        {
                                            Head1R = item.Y.IndexOf(Full_Yama.First());
                                            HeadX1R = item.X[Head1R];
                                            Head2R = item.Y.IndexOf(Full_Yama.Last());
                                            HeadX2R = item.X[Head2R];
                                            // SY.Add((HeadX1Y + HeadX2Y) / 2);
                                            YR.Add(Full_Yama.Average());
                                            X1R.Add(HeadX1R);
                                            X2R.Add(HeadX2R);
                                            Y1R.Add(Head1R);
                                            Y2R.Add(Head2R);
                                        }
                                    // AverK[k_f1] = 10000;
                                    Full_Yama_Down.Clear();
                                    Full_Yama.Clear();
                                    k_f1 = 100;
                                    FinY = false;
                                    k_f2 = 100;

                                }






                            }













                        }




                        ////////////////////  final new Right




                        if (Y1Y.Any() && Y1R.Any())
                        {

                            // if (Head2 - Y2Y.Max() > 25)
                            if (Y1R.Min() - Y2Y.Max() > 50)
                            {

                                Full_Yama.Clear();


                                Head1Y = Y2Y.Max() + 10;
                                HeadX1Y = item.X[Head1Y];
                                Head2Y = Y1R.Min() - 10;
                                HeadX2Y = item.X[Head2Y];
                                Full_Yama.AddRange(item.Y.Where(o =>
                                  item.X[item.Y.IndexOf(o)] <= HeadX2Y
                                 && item.X[item.Y.IndexOf(o)] >= HeadX1Y
                                   )
                                        );
                                Full_Yama_Down.Clear();



                                Full_Yama_Down.AddRange(item.Y.Where(o =>
                                  item.X[item.Y.IndexOf(o)] <= HeadX2Y - 10
                                 && Math.Abs(o - Full_Yama.Average()) <= 3
                                 && item.X[item.Y.IndexOf(o)] >= HeadX1Y + 10
                                   )
                                        );



                                Head1R = item.Y.IndexOf(Full_Yama_Down.First());
                                HeadX1R = item.X[Head1R];
                                Head2R = item.Y.IndexOf(Full_Yama_Down.Last());
                                HeadX2R = item.X[Head2R];
                                // SY.Add((HeadX1Y + HeadX2Y) / 2);
                                YR.Add(Full_Yama_Down.Average());
                                X1R.Add(HeadX1R);
                                X2R.Add(HeadX2R);
                                Y1R.Add(Head1R);
                                Y2R.Add(Head2R);
                            }





                        }






                        if (Y1Y.Any() && Y1L.Any())
                        {

                            // if (Head2 - Y2Y.Max() > 25)
                            if (Y2L.Max() - Y1Y.Min() > 50)
                            {

                                Full_Yama.Clear();


                                Head1Y = Y1Y.Min() - 10;
                                HeadX1Y = item.X[Head1Y];
                                Head2Y = Y2L.Max() + 10;
                                HeadX2Y = item.X[Head2Y];
                                Full_Yama.AddRange(item.Y.Where(o =>
                                  item.X[item.Y.IndexOf(o)] <= HeadX2Y
                                 && item.X[item.Y.IndexOf(o)] >= HeadX1Y
                                   )
                                        );
                                Full_Yama_Down.Clear();



                                Full_Yama_Down.AddRange(item.Y.Where(o =>
                                  item.X[item.Y.IndexOf(o)] <= HeadX2Y - 10
                                 && Math.Abs(o - Full_Yama.Average()) <= 3
                                 && item.X[item.Y.IndexOf(o)] >= HeadX1Y + 10
                                   )
                                        );


                                Head1L = item.Y.IndexOf(Full_Yama_Down.First());
                                HeadX1L = item.X[Head1L];
                                Head2L = item.Y.IndexOf(Full_Yama_Down.Last());
                                HeadX2L = item.X[Head2L];
                                // SY.Add((HeadX1Y + HeadX2Y) / 2);
                                YL.Add(Full_Yama_Down.Average());
                                X1L.Add(HeadX1L);
                                X2L.Add(HeadX2L);
                                Y1L.Add(Head1L);
                                Y2L.Add(Head2L);
                            }





                        }






                        //  Round Left HeadX

                        if (YL.Count > 0)
                        {

                            var x1 = 0;

                            var x0 = 0.0;
                            var x01 = 0.0;
                            var x02 = 0.0;
                            for (int j = 0; j < YL.Count; j++)
                            {


                                for (int i5 = j + 1; i5 < YL.Count; i5++)
                                {

                                    if (X1L[i5] < X1L[j])
                                    {
                                        x1 = X1L[j];
                                        X1L[j] = X1L[i5];
                                        X1L[i5] = x1;
                                        x1 = X2L[j];
                                        X2L[j] = X2L[i5];
                                        X2L[i5] = x1;
                                        x1 = Y1L[j];
                                        Y1L[j] = Y1L[i5];
                                        Y1L[i5] = x1;
                                        x1 = Y2L[j];
                                        Y2L[j] = Y2L[i5];
                                        Y2L[i5] = x1;
                                        x0 = YL[j];
                                        YL[j] = YL[i5];
                                        YL[i5] = x0;

                                    }

                                }
                            }



                            for (int j = 0; j < YL.Count; j++)
                            {

                                Points_manyR_str_LeftY_Head.Add(Y1L[j]);
                                Points_manyR_str_LeftY_Head.Add(Y2L[j]);
                                Points_manyR_str_LeftX.Add(X1L[j]);
                                Points_manyR_str_LeftX.Add(X2L[j]);
                                Points_manyR_str_LeftY.Add(YL[j]);
                                Points_manyR_str_LeftY.Add(YL[j]);
                            }
                        }


                        //  Round Right HeadX

                        if (YR.Count + YL.Count() > 10)
                        {

                        }
                        if (YR.Count > 0)
                        {
                            var x1 = 0;
                            var x0 = 0.0;

                            for (int j = 0; j < YR.Count; j++)
                            {


                                for (int i5 = j + 1; i5 < YR.Count; i5++)
                                {

                                    if (X1R[i5] < X1R[j])
                                    {
                                        x1 = X1R[j];
                                        X1R[j] = X1R[i5];
                                        X1R[i5] = x1;
                                        x1 = X2R[j];
                                        X2R[j] = X2R[i5];
                                        X2R[i5] = x1;
                                        x1 = Y1R[j];
                                        Y1R[j] = Y1R[i5];
                                        Y1R[i5] = x1;
                                        x1 = Y2R[j];
                                        Y2R[j] = Y2R[i5];
                                        Y2R[i5] = x1;
                                        x0 = YR[j];
                                        YR[j] = YR[i5];
                                        YR[i5] = x0;

                                    }

                                }
                            }

                            for (int j = 0; j < YR.Count; j++)
                            {
                                Points_manyR_str_RightY_Head.Add(Y1R[j]);
                                Points_manyR_str_RightY_Head.Add(Y2R[j]);
                                Points_manyR_str_RightX.Add(X1R[j]);
                                Points_manyR_str_RightX.Add(X2R[j]);
                                Points_manyR_str_RightY.Add(YR[j]);
                                Points_manyR_str_RightY.Add(YR[j]);
                            }
                        }







                        ////////////////////

                        ///////////////








                        var flagMR1 = 0;

                        var ff1 = flagMR1;

                        var ff11 = flagMR1;

                        if (Points_manyR_str_LeftY.Count > 1)
                        {
                            Head1 = (int)Points_manyR_str_LeftY_Head[0];
                            HeadX = (int)Points_manyR_str_LeftX[0];

                            flagMR1 = 2;
                        }

                        if (Points_manyR_str_RightY.Count > 1)
                        {
                            Head2 = (int)Points_manyR_str_RightY_Head[Points_manyR_str_RightY.Count - 1];
                            HeadX2 = (int)Points_manyR_str_RightX[Points_manyR_str_RightY.Count - 1];

                            //  Head2 = (int)Points_manyR_str_RightY_Head[ 1];
                            //  HeadX2 = (int)Points_manyR_str_RightX[ 1];

                            flagMR1 = 2;
                        }

                        var H0 = 2;
                        /////////////////////////////////////
                        ///





                        if (Head1 == 21)
                        {
                            Head1 = Head1;
                        }

                        if (Math.Abs(item.Y[Head1]) >= H0)
                        {
                            var M_luch = new List<double> { };
                            var Ss_listH2 = new List<double> { };
                            var S_min_listH2 = new List<double> { };
                            var S_min_k_listH2 = new List<double> { };
                            int dp = 0;
                            var mH2 = 1.0;
                            var step = 0.001;
                            var kof00 = 1.0;
                            if (Math.Abs(Head1) > 15) kof00 = Math.Min(Math.Abs(item.Y[Head1]) / (Head1), 1);

                            for (int k = dp; k < 2 * dp + 1; k++)
                            {
                                var s = 0.0;
                                var y0 = 1 * item.Y[Head1];
                                var lambda = 0.95;
                                double sd = 0;
                                var y00 = mH2 * Math.Abs(item.Y[Head1]);
                                var maxd = 0;

                                Ss_listH2.Clear();
                                for (int ii = 1; ii < 4000; ii++)
                                {

                                    s = 0.0;
                                    y0 = 1 * item.Y[Head1];
                                    lambda = 0.95;

                                    y00 = mH2 * Math.Abs(item.Y[Head1]);
                                    maxd = 0;
                                    var sr = (Head1) / 2;

                                    for (int m = sr; m < (Head1); m++)
                                    {

                                        var y1 = mH2 * y00 - (kof00 + (ii - 2000) * step) * m;

                                        var y2 = mH2 * Math.Abs(item.Y[Head1 - m]);
                                        var A = (kof00 + (ii - 2000) * step);
                                        var B = 1.0;
                                        var C = -mH2 * y00;
                                        var dZ = Math.Abs(A * m + B * y2 + C) / Math.Sqrt(A * A + 1.0);
                                        //if ((y1 - mH2 * y00) * (y1 - mH2 * y00) - m * m <20* mH2 * y00 * mH2 * y00)
                                        s = s + dZ;


                                    }

                                    Ss_listH2.Add(Math.Abs(s));

                                }
                                var testMin0H2 = Ss_listH2.Min();
                                var testMinInd0H2 = Ss_listH2.IndexOf(testMin0H2);
                                S_min_listH2.Add((int)testMin0H2);
                                S_min_k_listH2.Add(testMinInd0H2);
                            }

                            var testMin_minH2 = (int)S_min_listH2.Min();
                            var testMin_min_IndH2 = S_min_listH2.IndexOf(testMin_minH2);
                            var popr1H2 = testMin_min_IndH2 - dp;
                            var kofH2 = S_min_k_listH2[testMin_min_IndH2];
                            var vhodH1 = -mH2 * Math.Abs(item.Y[Head1] / (kof00 + (kofH2 - 2000) * step));//+ 20 * H0 / 100.0
                            var yp = Math.Abs(item.Y[Head1]);
                            // if (kofH2 > 800 && kofH2 < 1200) vhodH2 = 50;
                            if (yp < 20 && Math.Abs(vhodH1) < 20) vhodH1 = -45;

                            if (yp < 20 && Math.Abs(vhodH1) > 50) vhodH1 = (-45 + vhodH1) / 2;
                            HeadX = (int)(HeadX + vhodH1 < 0 ? 0 : HeadX + vhodH1);
                        }








                        if (Math.Abs(item.Y[Head2]) >= H0)
                        {
                            var M_luch = new List<double> { };
                            var Ss_listH2 = new List<double> { };
                            var S_min_listH2 = new List<double> { };
                            var S_min_k_listH2 = new List<double> { };
                            int dp = 0;
                            var mH2 = 1.0;
                            var step = 0.001;
                            var kof00 = 1.0;
                            //    if (Math.Abs(item.Y.Count() - Head2) > 10) kof00 = Math.Min(Math.Abs(item.Y[Head2]) / (item.Y.Count() - Head2), 1);

                            for (int k = dp; k < 2 * dp + 1; k++)
                            {
                                Ss_listH2.Clear();
                                for (int ii = 1; ii < 4000; ii++)
                                {

                                    var s = 0.0;
                                    var y0 = 1 * item.Y[Head2];
                                    var lambda = 0.95;
                                    double sd = 0;
                                    var y00 = Math.Abs(item.Y[Head2]);
                                    var maxd = 0;
                                    var sr = (item.Y.Count() - Head2) / 2;
                                    var sold = 0;
                                    for (int m = sr; m < (item.Y.Count() - Head2); m++)
                                    {

                                        var y1 = mH2 * y00 - (kof00 + (ii - 2000) * step) * m;

                                        var y2 = mH2 * Math.Abs(item.Y[Head2 + m]);
                                        var A = (kof00 + (ii - 2000) * step);
                                        var B = 1.0;
                                        var C = -mH2 * y00;
                                        var dZ = Math.Abs((A * m + B * y2 + C)) / Math.Sqrt(A * A + 1.0);

                                        //   if ((y1 - mH2 * y00) * (y1 - mH2 * y00) - m * m < 20 * mH2 * y00 * mH2 * y00) 
                                        s = s + dZ;


                                    }

                                    Ss_listH2.Add(Math.Abs(s));

                                }
                                var testMin0H2 = Ss_listH2.Min();
                                var testMinInd0H2 = Ss_listH2.IndexOf(testMin0H2);
                                S_min_listH2.Add((int)testMin0H2);
                                S_min_k_listH2.Add(testMinInd0H2);
                            }

                            var testMin_minH2 = (int)S_min_listH2.Min();
                            var testMin_min_IndH2 = S_min_listH2.IndexOf(testMin_minH2);
                            var popr1H2 = testMin_min_IndH2 - dp;
                            var kofH2 = S_min_k_listH2[testMin_min_IndH2];
                            var vhodH2 = mH2 * Math.Abs(item.Y[Head2] / (kof00 + (kofH2 - 2000) * step));// 
                            var yp = Math.Abs(item.Y[Head2]);
                            // if (kofH2 > 800 && kofH2 < 1200) vhodH2 = 50;
                            if (yp < 20 && vhodH2 < 20) vhodH2 = 50;
                            if (yp < 20 && Math.Abs(vhodH2) > 50) vhodH2 = (50 + vhodH2) / 2;

                            //  HeadX2 = (int)(HeadX2 + vhodH2 < 0 ? 0 : HeadX2 + vhodH2);




                            HeadX2 = (int)(HeadX2 + vhodH2 > result.Count() ? result.Count() - 1 : HeadX2 + vhodH2);


                            ///HeadX2 = (int)(HeadX2 + vhodH2 > item.Y.Count() ? item.Y.Count() - 1 : HeadX2 + vhodH2);

                        }























                        //////////////////////////////////////////   
                        var listY1 = new List<double>
                        {
                            //Math.Abs(result[HeadX]) <= 5 ? 0 : result[HeadX],
                            0,
                            avgSelD,
                            avgSelD,
                            //Math.Abs(result[HeadX2]) <= 5 ? 0 : result[HeadX2],
                            0,
                        };

                        if (item.X[item.Y.IndexOf(SelectedData.First())] < HeadX)
                        {
                            HeadX = item.X[item.Y.IndexOf(SelectedData.First())];
                        }

                        var delta1 = 0;
                        var delta2 = 0;
                        try
                        {
                            if ((Points_manyR_str_LeftY.Count < 1 && Points_manyR_str_RightY.Count < 1) && Math.Abs(SelectedData.Average()) < 10)
                            {


                                //delta1 = Math.Abs(HeadX - item.X[item.Y.IndexOf(SelectedData.First())]);
                                //delta2 = Math.Abs(HeadX2 - item.X[item.Y.IndexOf(SelectedData.Last())]);
                                //if (delta1 > 2.5 * delta2 && delta1 > 40 && delta2 < 100)
                                //    HeadX2 = item.X[item.Y.IndexOf(SelectedData.Last())] + (int)(1 * delta1);
                                //if (delta2 > 2.5 * delta1 & delta2 > 40 && delta2 < 100)
                                //    HeadX = item.X[item.Y.IndexOf(SelectedData.First())] - (int)(1 * delta2);
                                //HeadX2 = (int)(HeadX2 > result.Count() ? result.Count() - 1 : HeadX2);
                                //HeadX = (int)(HeadX < 0 ? 0 : HeadX);
                            }
                        }
                        catch (Exception e)

                        {


                            System.Console.WriteLine("Points_manyR_str_LeftY" + e.Message);
                        }

                        /////////////////////
                        ///


                        var CurveMaxF = item.Y.Max(o => Math.Abs(o));
                        var SelectedDataFinal = item.Y.Where(o => CurveMaxF * 0.5 < Math.Abs(o)).ToList();


                        var F1 = item.X[item.Y.IndexOf(SelectedDataFinal.First())]; //центр1 50
                        var V1 = item.X[item.Y.IndexOf(SelectedData.First())];      //круг1 85

                        var F2 = item.X[item.Y.IndexOf(SelectedDataFinal.Last())]; //центр2 50
                        var V2 = item.X[item.Y.IndexOf(SelectedData.Last())];      //круг2 85


                        //----------------------------------------
                        int HalfFirst = (int)(F1 - (V1 + HeadX) / 2) / 2;  //x1
                        int HalflAST = (int)(F2 - (V2 + HeadX2) / 2) / 2;  //x2
                        if (HalflAST + HeadX2 > result.Count() - 400)
                        { HalflAST = 0; }
                        if (HalflAST / 5 + HeadX2 > result.Count())
                        { HalflAST = 0; }
                        //   var yf = 0;
                        var yf = 0;
                        var xpf = (int)(Head1 / 2);





                        ////////////////////






                        var popr0 = 0;
                        var poprStr = 0;
                        //if (popr0 < 10) { poprStr = 3; }
                        //if (popr0 > 10 && popr0 < 20) { poprStr = 5; }

                        //if (popr0 > 20 && popr0 < 40) { poprStr = 8; }
                        //if (popr0 > 40) { poprStr = 8; }

                        HalflAST = poprStr;
                        HalfFirst = poprStr;
                        //  if ((Math.Abs(item.Y[Head2]) < 15) || (Math.Abs(item.Y[Head1]) < 15))
                        //  { poprStr = 0; }
                        if (Points_manyR_str_LeftY.Count < 1 && Points_manyR_str_RightY.Count < 1)
                        {
                            HeadX = HeadX + 0;
                            HeadX2 = HeadX2 - 0;
                        }
                        //if (Math.Abs(item.X[item.Y.IndexOf(SelectedData.First())] - item.X[item.Y.IndexOf(SelectedData.Last())]) < 20 && item.X[item.Y.IndexOf(SelectedData.Last())] - poprStr < result.Count() && item.X[item.Y.IndexOf(SelectedData.First())] + poprStr > 0)
                        //{
                        //    poprStr = -poprStr;

                        //}

                        var listX1 = new List<int>
                        {
                            HeadX ,
                            item.X[item.Y.IndexOf(SelectedData.First())] +  poprStr ,
                            item.X[item.Y.IndexOf(SelectedData.Last())] -  poprStr ,
                            HeadX2
                        };
                        //  for (int t = 0; t < (int)(listY1.Count() / 2 - 2); t++)

                        //Данные для карточки кривой
                        item.StrPoints.Add(HeadX - prev.Count());
                        item.StrPoints.Add(item.X[item.Y.IndexOf(SelectedData.First())] + poprStr - prev.Count());
                        item.StrPoints.Add(item.X[item.Y.IndexOf(SelectedData.Last())] - poprStr - prev.Count());
                        item.StrPoints.Add(HeadX2 - prev.Count());


                        if (Points_manyR_str_LeftY.Count > 1 && Points_manyR_str_RightY.Count < 1)
                        {
                            listY1.Clear();
                            listY1.Add(0);

                            for (var io = 0; io < Points_manyR_str_LeftY.Count; io++)
                            {
                                listY1.Add(Points_manyR_str_LeftY[io]);
                            };
                            listY1.Add(SelectedData.Average());
                            listY1.Add(SelectedData.Average());
                            listY1.Add(0);
                        };


                        if (Points_manyR_str_RightY.Count > 1 && Points_manyR_str_LeftY.Count < 1)
                        {
                            listY1.Clear();
                            listY1.Add(0);


                            listY1.Add(SelectedData.Average());
                            listY1.Add(SelectedData.Average());
                            for (var io = 0; io < Points_manyR_str_RightY.Count; io++)
                            {
                                listY1.Add(Points_manyR_str_RightY[io]);
                            };

                            listY1.Add(0);
                        };

                        if (Points_manyR_str_LeftY.Count > 1 && Points_manyR_str_RightY.Count > 1)
                        {
                            listY1.Clear();
                            listY1.Add(0);

                            for (var io = 0; io < Points_manyR_str_LeftY.Count; io++)
                            {
                                listY1.Add(Points_manyR_str_LeftY[io]);
                            };
                            listY1.Add(SelectedData.Average());
                            listY1.Add(SelectedData.Average());
                            for (var io = 0; io < Points_manyR_str_RightY.Count; io++)
                            {
                                listY1.Add(Points_manyR_str_RightY[io]);
                            };


                            listY1.Add(0);
                        };


                        if (Points_manyR_str_LeftY.Count > 1 && Points_manyR_str_RightY.Count < 1)
                        {
                            listX1.Clear();
                            item.StrPoints.Clear();

                            listX1.Add(HeadX);
                            item.StrPoints.Add(HeadX - prev.Count());

                            for (var io = 0; io < Points_manyR_str_LeftX.Count; io++)
                            {
                                listX1.Add(Points_manyR_str_LeftX[io]);
                                item.StrPoints.Add(Points_manyR_str_LeftX[io] - prev.Count());
                            };
                            listX1.Add(item.X[item.Y.IndexOf(SelectedData.First())]);
                            item.StrPoints.Add(item.X[item.Y.IndexOf(SelectedData.First())] - prev.Count());

                            listX1.Add(item.X[item.Y.IndexOf(SelectedData.Last())]);
                            item.StrPoints.Add(item.X[item.Y.IndexOf(SelectedData.Last())] - prev.Count());

                            listX1.Add(HeadX2);
                            item.StrPoints.Add(HeadX2 - prev.Count());
                        };

                        if (Points_manyR_str_LeftY.Count < 1 && Points_manyR_str_RightY.Count > 1)
                        {
                            listX1.Clear();
                            item.StrPoints.Clear();

                            listX1.Add(HeadX);
                            item.StrPoints.Add(HeadX - prev.Count());


                            listX1.Add(item.X[item.Y.IndexOf(SelectedData.First()) + poprStr]);
                            item.StrPoints.Add(item.X[item.Y.IndexOf(SelectedData.First())] + poprStr - prev.Count());

                            listX1.Add(item.X[item.Y.IndexOf(SelectedData.Last())] - poprStr);
                            item.StrPoints.Add(item.X[item.Y.IndexOf(SelectedData.Last())] - poprStr - prev.Count());


                            for (var io = 0; io < Points_manyR_str_RightX.Count; io++)
                            {
                                listX1.Add(Points_manyR_str_RightX[io]);
                                item.StrPoints.Add(Points_manyR_str_RightX[io] - prev.Count());
                            };


                            listX1.Add(HeadX2);
                            item.StrPoints.Add(HeadX2 - prev.Count());
                        };


                        var popr = 0;
                        var popr1 = 0;

                        if (Points_manyR_str_LeftY.Count > 1 && Points_manyR_str_RightY.Count > 1)
                        {
                            popr = 0;
                            popr1 = 0;
                            listX1.Clear();
                            item.StrPoints.Clear();

                            listX1.Add(HeadX);
                            item.StrPoints.Add(HeadX - prev.Count());

                            for (var io = 0; io < Points_manyR_str_LeftX.Count; io++)

                            {
                                listX1.Add(Points_manyR_str_LeftX[io]);
                                item.StrPoints.Add(Points_manyR_str_LeftX[io] - prev.Count());
                            };
                            listX1.Add(item.X[item.Y.IndexOf(SelectedData.First())]);
                            item.StrPoints.Add(item.X[item.Y.IndexOf(SelectedData.First())] - prev.Count());

                            listX1.Add(item.X[item.Y.IndexOf(SelectedData.Last())]);
                            item.StrPoints.Add(item.X[item.Y.IndexOf(SelectedData.Last())] - prev.Count());
                            for (var io = 0; io < Points_manyR_str_RightX.Count; io++)

                            {

                                listX1.Add(Points_manyR_str_RightX[io]);
                                item.StrPoints.Add(Points_manyR_str_RightX[io] - prev.Count());
                            };
                            listX1.Add(HeadX2 - popr1);
                            item.StrPoints.Add(HeadX2 - popr1 - prev.Count());
                        };

                        if (CurveMax > 54 && CurveMax < 60 && item.Y.Count() > 290 && item.Y.Count() < 350 && YY.Count() > 0 && YR.Count() == 0 && YL.Count() == 0)
                        {

                        }

                        if (Yama)   /////////  LIST    Y
                        {
                            listX1.Clear();

                            listY1.Clear();
                            listY1.Add(0);


                            if (Points_manyR_str_LeftY.Count > 1)
                            {
                                listY1.Clear();
                                listY1.Add(0);

                                for (var io = 0; io < Points_manyR_str_LeftY.Count; io++)
                                {
                                    listY1.Add(Points_manyR_str_LeftY[io]);
                                };
                            }
                            for (var io = 0; io < Points_manyR_str_YamaYY.Count; io++)
                            {
                                listY1.Add(Points_manyR_str_YamaYY[io]);
                            };
                            //listY1.Add(SelectedData_Before_Yama.Average());
                            //listY1.Add(SelectedData_Before_Yama.Average());
                            //listY1.Add(SelectedData_Yama_Down.Average());
                            //listY1.Add(SelectedData_Yama_Down.Average());
                            //listY1.Add(SelectedData_After_Yama.Average());
                            //listY1.Add(SelectedData_After_Yama.Average());
                            if (Points_manyR_str_RightY.Count > 1)
                            {
                                for (var io = 0; io < Points_manyR_str_RightY.Count; io++)
                                {
                                    listY1.Add(Points_manyR_str_RightY[io]);
                                };
                            }
                            listY1.Add(0);
                        };

                        if (Yama)     /////  LIST    X
                        {
                            listX1.Clear();
                            item.StrPoints.Clear();

                            listX1.Add(HeadX);
                            item.StrPoints.Add(HeadX - prev.Count());



                            if (Points_manyR_str_LeftY.Count > 1)
                            {
                                listX1.Clear();
                                item.StrPoints.Clear();

                                listX1.Add(HeadX);
                                item.StrPoints.Add(HeadX - prev.Count());
                                // Points_manyR_str_YamaX
                                for (var io = 0; io < Points_manyR_str_LeftX.Count; io++)
                                {
                                    listX1.Add(Points_manyR_str_LeftX[io]);
                                    item.StrPoints.Add(Points_manyR_str_LeftX[io] - prev.Count());
                                };
                            }

                            //listX1.Add(item.X[item.Y.IndexOf(SelectedData.First())]);

                            for (var io = 0; io < Points_manyR_str_YamaX.Count; io++)
                            {
                                listX1.Add(Points_manyR_str_YamaX[io]);
                                item.StrPoints.Add(Points_manyR_str_YamaX[io] - prev.Count());
                            };

                            //item.StrPoints.Add(item.X[item.Y.IndexOf(SelectedData.First())] - prev.Count());
                            //listX1.Add(HeadX_Yama_Up);
                            //item.StrPoints.Add(HeadX_Yama_Up - prev.Count());

                            //listX1.Add(HeadX_Yama_down);
                            //item.StrPoints.Add(HeadX_Yama_down - prev.Count());
                            //listX1.Add(HeadX2_Yama_down);
                            //item.StrPoints.Add(HeadX2_Yama_down - prev.Count());

                            //listX1.Add(HeadX2_Yama_Up);
                            //item.StrPoints.Add(HeadX2_Yama_Up - prev.Count());
                            //listX1.Add(item.X[item.Y.IndexOf(SelectedData.Last())] - 5);
                            //item.StrPoints.Add(item.X[item.Y.IndexOf(SelectedData.Last())] - 5 - prev.Count());
                            if (Points_manyR_str_RightY.Count > 1)
                            {
                                for (var io = 0; io < Points_manyR_str_RightX.Count; io++)
                                {
                                    listX1.Add(Points_manyR_str_RightX[io] + 0);
                                    item.StrPoints.Add(Points_manyR_str_RightX[io] + 0 - prev.Count());
                                };
                            }

                            listX1.Add(HeadX2);
                            item.StrPoints.Add(HeadX2 - prev.Count());
                        };

                        //Для Пру

                        //интерполяция кривой
                        var prevVal = listY1[1];
                        var xcount = listX1[0];
                        var c_first_count = 0;
                        if (listY1.Count() < listX1.Count() || listY1.Count() > listX1.Count())
                        {

                        }
                        var SelectedDataT = new List<double> { };
                        var SelectedDataTh = new List<double> { };
                        var SelectedDataTh1 = new List<double> { };
                        var SelectedDataTh2 = new List<double> { };
                        var h2 = 0.0;
                        var Head1T = 0;
                        var HeadX1T = 0;
                        var Head2T = 0;
                        var HeadX2T = 0;
                        //  item.StrPoints.Add(Points_manyR_str_RightX[io] + 0 - prev.Count());
                        item.StrPoints.Clear();


                        int t00 = 0;

                        if (listX1.Count() > 3)
                        {


                            var delta_ur = 8;
                            if (height == 4)
                            {
                                delta_ur = 6;
                            }

                            var s = 0.0;
                            var ss = 0;

                            var count = 0;
                            var t10 = 0;
                            var t20 = 0;
                            var flag = 0;
                            for (int t1 = 1; t1 < listX1.Count() - 1; t1++)
                            {
                                if (Math.Abs(listY1[t1 + 1] - listY1[t1]) < delta_ur && t10 == 0 && Math.Abs(listY1[t1 + 1]) > 15)
                                {
                                    s = s + (listY1[t1] + listY1[t1 + 1]) / 2.0 * Math.Abs(listX1[t1 + 1] - listX1[t1]);
                                    ss = ss + Math.Abs(listX1[t1 + 1] - listX1[t1]);

                                    t10 = t1;
                                    if (Math.Abs(listX1[t1 + 1] - listX1[t1]) < 2) flag = 1;
                                }
                                if (Math.Abs(listY1[t1 + 1] - listY1[t1]) < delta_ur && t10 > 0 && Math.Abs(listY1[t1 + 1]) > 15)
                                {
                                    s = s + (listY1[t1] + listY1[t1 + 1]) / 2.0 * Math.Abs(listX1[t1 + 1] - listX1[t1]);
                                    ss = ss + Math.Abs(listX1[t1 + 1] - listX1[t1]);


                                }


                                if (Math.Abs(listY1[t1] - listY1[1 + t1]) > delta_ur || (t1 == listY1.Count() - 2) && t10 > 0)
                                {
                                    t20 = t1;
                                    break;
                                }

                            }


                            if (t20 - t10 > 1 && t10 > 0 && t20 < listY1.Count() - 1)
                            {
                                for (int t1 = t10; t1 < t20 + 1; t1++)
                                {

                                    listY1[t1] = s / ss;



                                }


                            }


                        }


                        if (listX1.Count() > 3)
                        {


                            var delta_ur = 4;
                            if (height == 4)
                            {
                                delta_ur = 6;
                            }

                            var s = 0.0;
                            var ss = 0;

                            var count = 0;
                            var t10 = 0;
                            var t20 = 0;
                            var flag = 0;
                            for (int t1 = 1; t1 < listX1.Count() - 3; t1++)
                            {


                                if (Math.Abs(listY1[t1 + 1] - listY1[t1]) < delta_ur
                                    && t10 == 1 && Math.Abs(listY1[t1 + 1]) > 5

                                    && Math.Abs(listY1[t1 + 2] - listY1[t1 + 1]) < delta_ur
                                 && Math.Abs(listY1[t1 + 3] - listY1[t1 + 2]) < delta_ur && Math.Abs(listY1[t1 + 1]) > 10)
                                {
                                    ss = 0;
                                    s = 0;
                                    for (int t11 = 0; t11 < 4; t11++)
                                    {
                                        s = s + (listY1[t1 + t11] + listY1[t1 + t11 + 1]) / 2.0 * Math.Abs(listX1[t1 + t11 + 1] - listX1[t1 + t11]);
                                        ss = ss + Math.Abs(listX1[t1 + t11 + 1] - listX1[t1 = t11]);

                                        t20 = t10 + 3;
                                    }







                                    if (t20 - t10 > 1 && t10 > 0 && t20 < listY1.Count() - 1)
                                    {
                                        for (int t11 = 0; t11 < 4; t11++)
                                        {

                                            listY1[t10 + t11] = s / ss;



                                        }
                                        t10 = 0;
                                        t20 = 0;
                                        flag = 0;

                                    }


                                }
                            }





                        }





                        for (int t0 = 0; t0 < listX1.Count() - 1; t0++)
                        {
                            int t = t0;
                            int sv = 2 * t0 + 1;
                            var Maxt = 0.0;
                            var Mint = 0.0;
                            var HeadTh = 0;
                            var HeadXTh = 0;
                            var XT1 = listX1[t];
                            var XT2 = listX1[t + 1];
                            var XT1m = 0;
                            var XT2m = 0;
                            var XTmC = 0;

                            int XT0 = listX1[0];
                            int XTCount = listX1.Count() - 1;


                            var Max = listY1.Select(Math.Abs).Max();

                            Maxt = Math.Max(Math.Abs(listY1[t]), Math.Abs(listY1[t + 1]));
                            Mint = Math.Min(Math.Abs(listY1[t]), Math.Abs(listY1[t + 1]));


                            SelectedDataTh = new List<double> { };
                            SelectedDataTh1 = new List<double> { };
                            SelectedDataTh2 = new List<double> { };

                            SelectedDataTh.AddRange(item.Y.Where(o =>
                                       Math.Abs(Math.Abs(o) - (Mint + (Maxt - Mint) * 1 / 2)) < 4
                                       && Maxt - Mint > 2
                                        && item.X[item.Y.IndexOf(o)] < listX1[t + 1]
                                     && item.X[item.Y.IndexOf(o)] > listX1[t]));



                            if (listX1[t + 1] - listX1[t] < 20 && Math.Abs(listY1[t]) > 5 && t > 0 && t < listX1.Count() - 2)
                            {
                                listX1[t + 1] = listX1[t + 1] + 5;
                                listX1[t] = listX1[t] - 5;
                            }


                            if (!SelectedDataTh.Any())
                            {
                                continue;
                            }

                            if (SelectedDataTh.Any())
                            {
                                XT1 = item.Y.IndexOf(SelectedDataTh.First());
                                XT2 = item.Y.IndexOf(SelectedDataTh.Last());
                            }

                            HeadXTh = (item.X[XT2] + item.X[XT1]) / 2;
                            var C = (listX1[t] + listX1[t + 1]) / 2;
                            var delC = (C - HeadXTh);


                            if (SelectedDataTh.Count() > 1 && Maxt - Mint > 5)
                            {

                                if (Math.Abs(delC) < 30)
                                {


                                    XT1 = HeadXTh - (listX1[t + 1] - listX1[t]) / 2;
                                    XT2 = HeadXTh + (listX1[t + 1] - listX1[t]) / 2;


                                    listX1[t] = XT1;
                                    listX1[t + 1] = XT2;
                                    item.StrPoints.Add(listX1[t] - prev.Count());
                                    item.StrPoints.Add(listX1[t + 1] - prev.Count());
                                }


                            }
                        }






                        if (height == 10 || height == 4)
                        {


                            for (int t0 = 0; t0 < listX1.Count() - 1; t0++)
                            {
                                int t = t0;
                                int sv = 2 * t0 + 1;
                                var Maxt = 0.0;
                                var Mint = 0.0;
                                var HeadTh = 0;
                                var HeadXTh = 0;
                                var XT1 = listX1[t];
                                var XT2 = listX1[t + 1];
                                var XT1m = 0;
                                var XT2m = 0;
                                var XTmC = 0;

                                int XT0 = listX1[0];
                                int XTCount = listX1.Count() - 1;


                                var Max = listY1.Select(Math.Abs).Max();

                                Maxt = Math.Max(Math.Abs(listY1[t]), Math.Abs(listY1[t + 1]));
                                Mint = Math.Min(Math.Abs(listY1[t]), Math.Abs(listY1[t + 1]));


                                SelectedDataTh = new List<double> { };
                                SelectedDataTh1 = new List<double> { };
                                SelectedDataTh2 = new List<double> { };
                                if (t0 == 0)
                                {
                                    SelectedDataTh1.AddRange(item.Y.Where(o =>
                                               Math.Abs(Math.Abs(o) - (Mint + (Maxt - Mint) * 0.75)) < 3
                                               && Maxt - Mint > 10
                                                && item.X[item.Y.IndexOf(o)] < listX1[t + 1]
                                             && item.X[item.Y.IndexOf(o)] > listX1[t]));
                                    //&& item.X[item.Y.IndexOf(o)] > (listX1[t] + listX1[t + 1]) / 2.0));
                                }
                                if (t0 == listX1.Count() - 2)
                                {
                                    SelectedDataTh1.AddRange(item.Y.Where(o =>
                                               Math.Abs(Math.Abs(o) - (Mint + (Maxt - Mint) * 0.75)) < 3
                                               && Maxt - Mint > 10
                                                && item.X[item.Y.IndexOf(o)] < listX1[t + 1]
                                             && item.X[item.Y.IndexOf(o)] > listX1[t]));
                                }
                                //SelectedDataTh2.AddRange(item.Y.Where(o =>
                                //       Math.Abs(Math.Abs(o) - (Mint + (Maxt - Mint) * 0.75)) < 2
                                //       && Maxt - Mint > 2
                                //        && item.X[item.Y.IndexOf(o)] < listX1[t + 1]
                                //     && item.X[item.Y.IndexOf(o)] > listX1[t]));




                                if (!SelectedDataTh1.Any())
                                {
                                    continue;
                                }

                                if (SelectedDataTh1.Any())
                                {
                                    XT1 = item.Y.IndexOf(SelectedDataTh1.First());
                                    XT2 = item.Y.IndexOf(SelectedDataTh1.Last());
                                }

                                HeadXTh = (item.X[XT2] + item.X[XT1]) / 2;

                                var delta = 0;

                                if (t0 == 0) { delta = (listX1[t + 1] - HeadXTh) / 3; }
                                if (t0 == listX1.Count() - 2) { delta = (-listX1[t] + HeadXTh) / 3; }
                                if (SelectedDataTh1.Count() > 0 && Maxt - Mint > 30)
                                {


                                    if (Math.Abs(delta) < 30 && height == 10)
                                    {


                                        if (t0 != 0 && t0 != listX1.Count() - 2) continue;


                                        if (t0 == 0 && delta > 0)
                                        {
                                            listX1[t] = listX1[t] + delta;
                                            listX1[t + 1] = listX1[t + 1] - delta;
                                        }
                                        if (t0 == listX1.Count() - 2 && delta > 0)
                                        {
                                            listX1[t] = listX1[t] + delta;
                                            listX1[t + 1] = listX1[t + 1] - delta;
                                        }

                                        item.StrPoints.Add(listX1[t] - prev.Count());
                                        item.StrPoints.Add(listX1[t + 1] - prev.Count());
                                    }


                                    if (Math.Abs(delta) < 30 && height == 4)
                                    {
                                        if (Maxt - Mint > 40)
                                        {
                                            delta = (int)2 * delta;
                                        }

                                        if (t0 != 0 && t0 != listX1.Count() - 2) continue;


                                        if (t0 == 0 && delta > 0)
                                        {
                                            listX1[t] = listX1[t] + delta;
                                            listX1[t + 1] = listX1[t + 1] - delta;
                                        }
                                        if (t0 == listX1.Count() - 2 && delta > 0 && Math.Abs(listX1[t]) < Math.Abs(listX1[t + 1]))
                                        {
                                            listX1[t] = listX1[t] + delta;
                                            listX1[t + 1] = (int)(listX1[t + 1] - delta);
                                        }
                                        if (t0 == listX1.Count() - 2 && delta > 0 && Math.Abs(listX1[t]) > Math.Abs(listX1[t + 1]))
                                        {
                                            listX1[t] = (int)(listX1[t] + delta);
                                            listX1[t + 1] = listX1[t + 1] - delta;
                                        }


                                        item.StrPoints.Add(listX1[t] - prev.Count());
                                        item.StrPoints.Add(listX1[t + 1] - prev.Count());
                                    }




                                }
                            }





                        }






                        var newList = listX1.OrderBy((o => o)).ToList();
                        listX1 = newList;

                        for (int t = 0; t < Math.Min(listY1.Count(), listX1.Count()) - 1; t++)
                        {
                            double bottom_dx1 = listX1[t + 1] - listX1[t];

                            double y2 = listY1[t + 1];

                            double y1 = listY1[t];



                            for (int c = 0; c < listX1[t + 1] - listX1[t]; c++)
                            {

                                linearY = (y2 - y1) / bottom_dx1 * c + y1;


                                if (linearY * y1 >= 0)
                                {
                                    ModifiedCurve.Add(linearY);
                                }
                            }
                        }






                        item.lvl_center_value = Math.Abs(avgSelD);
                        item.lvl_center_index = Math.Abs(HeadX2 - HeadX);

                        //    item.StrPoints.Add(HeadX2 - prev.Count());

                        var deltaHead = 0;
                        //if (item.StrPoints.Count() - listX1.Count() > 3)
                        //{
                        //    deltaHead = item.StrPoints[item.StrPoints.Count() - listX1.Count() - 2] + prev.Count();
                        //}
                        //if (deltaHead < 0)

                        //{

                        //    listX1[listX1.Count() - 1] = listX1[listX1.Count() - 1] + deltaHead;
                        //}
                        //  deltaHead = item.StrPoints[item.StrPoints.Count() - listX1.Count() - 2]




                        var Head2new = 0;

                        HeadX = listX1[0];
                        HeadX2 = listX1[listX1.Count() - 1];
                        HeadX = (int)(HeadX < 0 ? 0 : HeadX);
                        HeadX2 = (int)(HeadX2 > resultCopy.Count() - 1 ? resultCopy.Count() - 1 : HeadX2);
                        //  HeadX - 1 > 0 ? HeadX - 1 : HeadX;
                        if (Curves.Count() < 1)
                        {
                            for (int index = 0; index < resultCopy.Count(); index++)
                            {
                                resultCopy[index] = 0.0;
                            }
                        }
                        var InternalIndex = 0;

                        for (int index = HeadX; index < HeadX2; index++)
                        {
                            resultCopy[index] = ModifiedCurve[InternalIndex];

                            //resultCopy[index] = avg[InternalIndex];
                            //resultCopy[index] = result[index] ;
                            InternalIndex++;
                        }
                        for (int index = HeadX - 1 > 0 ? HeadX - 1 : HeadX; index < HeadX; index++)
                        {
                            resultCopy[index] = 0.0;
                        }
                        for (int index = HeadX2; index < (HeadX2 <= resultCopy.Count() ? HeadX2 : HeadX2); index++)  //0=25
                        {
                            resultCopy[index] = 0.0;
                        }
                    }

                    // }


                    result = resultCopy;
                    //result = result.Zip(resultCopy, (x, y) => x + y).ToList();
                }


                if (ResultTestCurve.Any())
                {
                    var aaa = ResultTestCurve.Where(o => o.RealY.Count > 20).ToList();
                    var bbb = aaa.Where(o => o.RealY.Select(o => Math.Abs(o)).Max() >= 35).ToList();
                    var ttt = bbb.Where(o => o.RealY.Count < 100).ToList();
                    if (height == 4)
                    {
                        //  находеждение  закрестовииных кривых
                        foreach (var item in ttt)
                        {
                            // var localInd = 0;
                            // for (int q = item.X.First(); q < item.X.Last(); q++)
                            // {
                            //result[q] = item.Y[localInd] * 1.25;
                            var localInd = item.X.First();
                            // localInd++;
                            // var Upselected
                            var delta = +0;
                            var CurveMax = item.RealY.Max(o => Math.Abs(o));
                            var SelectedDataUp = item.RealY.Where(o => CurveMax * 0.7 > Math.Abs(o));
                            // var Head1 = SelectedDataUp.First();
                            // var Head2 = SelectedDataUp.Last();


                            var Head1 = item.RealY.IndexOf(SelectedDataUp.First());
                            var HeadX = item.X[item.RealY.IndexOf(SelectedDataUp.First())];

                            var Head2 = item.RealY.IndexOf(SelectedDataUp.Last());
                            var HeadX2 = item.X[Head2];

                            var xx1 = item.X.First();
                            var xx4 = item.X.Last();

                            var xx3 = HeadX2 - delta;
                            var y = item.RealY.Last();
                            var xx2 = HeadX + delta;
                            var xx5 = xx4;
                            var yy1 = 0;
                            var yy0 = 0;
                            var yy2 = (item.Y.Average() > 0 ? item.Y.Average() : item.Y.Average());
                            var yy3 = yy2;
                            var yy4 = 0;
                            var yy5 = 0;

                            var listX1 = new List<int>
                        {//     (int)xx0  ,
                           (int)xx1  ,
                          (int) xx2,
                          (int)xx3,
                         (int) xx4
                        // (int) xx5
                        };

                            var listY1 = new List<double>
                        {//         yy0,
                            0,
                           yy2,
                          yy3,
                      0

                        };






                            for (int t = 0; t < listY1.Count() - 1; t++)
                            {
                                for (int c = 0; c < listX1[t + 1] - listX1[t]; c++)
                                {
                                    double bottom_dx1 = listX1[t + 1] - listX1[t];

                                    double y2 = listY1[t + 1];

                                    double y1 = listY1[t];
                                    linearY = (y2 - y1) / bottom_dx1 * c + y1;
                                    if ((linearY * y2 < -1) || (linearY * y1 < -1))
                                        linearY = 0;
                                    result[localInd] = linearY;
                                    localInd++;
                                    // localInd++;
                                    //ModifiedCurve.Add(linearY);
                                }
                            }

                            //}


                        }


                    }
                }


                if (naprav == Direction.Reverse)
                {
                    result.Reverse();
                }

                ////басын аяғын қиып аламыз
                var cropped = result.GetRange(prev.Count == 0 ? 0 : prev.Count - 1, avg.Count - 1);
                //var cropped = originalDbPoints.GetRange(prev.Count == 0 ? 0 : prev.Count - 1, avg.Count - 1);

                if (getPairs)
                    return transitions;
                return cropped;
            }

            catch (Exception e)
            {
                System.Console.WriteLine("Index out of range 1" + e.Message);
            }


            //return ZeroDataStright;
            return avg;

        }


        public static List<double> GetTrapezoid(
           this List<double> avg, List<double> prev, List<double> next,
           int height, ref List<NatureCurves> CurveForLvl,
           bool getPairs = false, Direction naprav = Direction.Direct, List<double> strRealData = null)
        {
            // StrightAvgTrapezoid = StrightAvg.GetTrapezoid(prevStrightAvgPart, nextStrightAvgPart, 4, ref Curves);
            // LevelAvgTrapezoid = LevelAvg.GetTrapezoid(prev50, next50, 10, ref Curves);
            var Masht = 1.25;
            if (height == 10) Masht = 1;
            var Zero = avg.Select(o => o * 0.0).ToList();
            var ZeroDataPlus = avg.Select(o => o * 0.0).ToList();
            var ZeroDataMinus = avg.Select(o => o * 0.0).ToList();


            //var avgplus = avg.Select(o => Math.Abs(o) * Masht * (Math.Sign(o) + 1) / 2).ToList();
            //var prevplus = prev.Select(o => Math.Abs(o) * Masht * (Math.Sign(o) + 1) / 2).ToList();
            //var nextplus = next.Select(o => Math.Abs(o) * Masht * (Math.Sign(o) + 1) / 2).ToList();
            //var avgminus = avg.Select(o => Math.Abs(o) * Masht * (Math.Sign(o) - 1) / 2).ToList();
            //var prevminus = prev.Select(o => Math.Abs(o) * Masht * (Math.Sign(o) - 1) / 2).ToList();
            //var nextminus = next.Select(o => Math.Abs(o) * Masht * (Math.Sign(o) - 1) / 2).ToList();

            var avgplus = avg.Select(o => o > 5 ? Masht * o : 0).ToList();
            var prevplus = prev.Select(o => o > 5 ? Masht * o : 0).ToList();
            var nextplus = next.Select(o => o > 5 ? Masht * o : 0).ToList();

            var avgminus = avg.Select(o => o < -5 ? Masht * o : 0).ToList();
            var prevminus = prev.Select(o => o < -5 ? Masht * o : 0).ToList();
            var nextminus = next.Select(o => o < -5 ? Masht * o : 0).ToList();
            //  if (strRealData==null)
            // {
            // return avg;
            //   }
            //   var strRealData_strRealData = strRealData.Select(o => o * 1).ToList();





            var Curves = new List<NatureCurves> { };


            if (height == 4)
            {


                ZeroDataPlus = avgplus.GetTrapezoidPM(prevplus, nextplus, height, ref Curves, strRealData: avgplus);

                Curves = new List<NatureCurves> { };
                ZeroDataMinus = avgminus.GetTrapezoidPM(prevminus, nextminus, height, ref Curves, strRealData: avgminus);

                Zero = ZeroDataPlus.Zip(ZeroDataMinus, (x, y) => (x + y) / Masht).ToList();



            }

            if (height == 10)
            {

                Curves = new List<NatureCurves> { };
                ZeroDataPlus = avgplus.GetTrapezoidPM(prevplus, nextplus, 10, ref Curves, strRealData: avgplus);

                Curves = new List<NatureCurves> { };
                ZeroDataMinus = avgminus.GetTrapezoidPM(prevminus, nextminus, 10, ref Curves, strRealData: avgminus);


                Zero = ZeroDataPlus.Zip(ZeroDataMinus, (x, y) => (x + y) / Masht).ToList();


            }
























            return Zero;

        }

        public static double GetGapKoef()
        {
            return 0.68;
        }

        public static double Radian(this double angle)
        {
            return (angle / 180.0) * Math.PI;
        }

        public static int MeterToPicket(this int meter)
        {
            return (meter % 100 == 0 && meter != 0) ? meter / 100 : (meter % 100 != 0 ? meter / 100 + 1 : 1);
        }
        public static double ToRadians(this double angleIn10thofaDegree)
        {
            // Angle in 10th of a degree
            return Math.Tan((angleIn10thofaDegree * Math.PI) / 1800);
        }

        public static double RadianToAngle(this float radian)
        {
            return radian * 180.0 / Math.PI;
        }


    }
    public enum SocketState
    {
        Abortively = -1,
        Success = 0
    }

    public enum VagonType
    {
        Sapsan = 0,
        Lastochka = 1,
        Strizh = 2,
        Allegro = 3,
        PassengerAndFreight = 4,
    }
    class ResultTestCurve
    {
        public List<double> RealY = new List<double> { };
        public List<double> Y = new List<double> { };
        public List<int> X = new List<int> { };

    }
    class ResultTestCurveRiht
    {
        public List<double> Riht_Centre = new List<double> { };
        public List<double> Y = new List<double> { };
        public List<int> X1 = new List<int> { };
        public List<int> X2 = new List<int> { };
        public List<int> Y1 = new List<int> { };
        public List<int> Y2 = new List<int> { };
        public List<int> CountR = new List<int> { };
        // public List<int> Head1 = new List<int> { };
        // public List<int> Head2 = new List<int> { };
        // public List<int> HeadX = new List<int> { };
        //public List<int> HeadX2 = new List<int> { };
    }
    class ResultTestCurveLeft
    {
        public List<double> Left_Centre = new List<double> { };
        public List<double> Y = new List<double> { };
        public List<int> X1 = new List<int> { };
        public List<int> X2 = new List<int> { };
        public List<int> Y1 = new List<int> { };
        public List<int> Y2 = new List<int> { };
        public List<int> CountL = new List<int> { };
        // public List<int> Head1 = new List<int> { };
        // public List<int> Head2 = new List<int> { };
        //   public List<int> HeadX = new List<int> { };
        //  public List<int> HeadX2 = new List<int> { };
    }





    public class NatureCurves
    {
        public List<double> Y = new List<double> { };
        public List<int> X = new List<int> { };
        public bool Added = false;

        public double lvl_center_value = -1;
        public int lvl_center_index = -1;


        public List<int> LevelPoints = new List<int> { };
        public List<int> StrPoints = new List<int> { };

    }

    public class ThresholdsAcceleration
    {
        //АНП, близкое к допустимому, м/с2
        public double AnpBliskoeKDopusku = 0;
        //АНП для ограничения скорости, м/с2
        public double AnpOgrSkor = 0;
        //АГ для ограничения скорости, м/с2
        public double AgOgrSkor = 0;
        //ψ
        public double Psi = 0.6;
    }
}






