using System.Configuration;
using Microsoft.Extensions.Configuration;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;

namespace ALARm.DataAccess
{
    public static class Helper
    {

        public static string ConnectionString()
        {
            try
            {

                var configBuilder = new ConfigurationBuilder();
                //System.Console.WriteLine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                //var path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "////appsettings.json");
                string userName = Environment.UserName;
                var path = $@"C:\Users\{userName}\AppData\Local\Programs\alarm-p-p.-web\resources\bin\appsettings.json";
                configBuilder.AddJsonFile(path, false);
                var root = configBuilder.Build();
                var appSetting = root.GetSection("ConnectionStrings:DefaultConnection");
                return appSetting.Value;
            }
            catch(Exception e)  
            {
                Console.WriteLine("ошибка подлючения AppData" + e);
                return ConfigurationManager.ConnectionStrings["cn"].ConnectionString ;
            }
        }

       
    }
}
