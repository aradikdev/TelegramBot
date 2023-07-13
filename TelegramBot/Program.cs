using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DevSpaceConsole
{
    public class Program
    {
        private static string _token { get; set; } = "6213536541:AAHPDtBfd9Aa17H4mQt5PL0QpvJl3dBkeBo";
        private static TelegramBotClient _client;
        private static Timer _timer;
        public static void Main()
        {

            _client = new TelegramBotClient(_token);

            using CancellationTokenSource cts = new();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
            };
            _client.StartReceiving(updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token);

            Console.WriteLine("Start");

            _timer = new Timer(GetMessagesFromDb, null, 3000, 3000);

            Console.ReadLine();
            // Send cancellation request to stop bot
            cts.Cancel();
        }

        private static void GetMessagesFromDb(object state)
        {
            // Discard the result
            SendMessage();
        }
        private async static Task SendMessage()
        {
            //your connection string 
            string connString = "Data Source=(local);Initial Catalog=DevSpaceBot;Integrated Security=True;Trust Server Certificate=true";
            //create instanace of database connection
            SqlConnection conn = new SqlConnection(connString);
            try
            {
                //open connection
                conn.Open();
                //create a new SQL Query using StringBuilder
                StringBuilder strBuilder = new StringBuilder();

                strBuilder.Append("SELECT * FROM Messages WHERE IsSended = 0");
                string sqlQuery = strBuilder.ToString();

                SqlCommand cmd = new SqlCommand(sqlQuery, conn);
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    long id = Convert.ToInt32(rdr["Id"]);
                    long chatId = Convert.ToInt64(rdr["ChatId"]);
                    string message = Convert.ToString(rdr["MessageText"]);
                    Console.WriteLine(chatId);
                    Console.WriteLine(message);
                    await SendMessageToTelegram(chatId, message);
                    await UpdateMessage(id);
                }

                strBuilder.Clear(); // clear all the string
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
        private async static Task UpdateMessage(long id)
        {
            //your connection string 
            string connString = "Data Source=(local);Initial Catalog=DevSpaceBot;Integrated Security=True;Trust Server Certificate=true";
            //create instanace of database connection
            SqlConnection conn = new SqlConnection(connString);
            try
            {
                //open connection
                conn.Open();
                //create a new SQL Query using StringBuilder
               
                StringBuilder strBuilder2 = new StringBuilder();
                strBuilder2.Append("update Messages set IsSended = 1 where Id = ");
                strBuilder2.Append(id);
                string sqlQuery2 = strBuilder2.ToString();

                SqlCommand cmd2 = new SqlCommand(sqlQuery2, conn);
                cmd2.ExecuteNonQuery();
                strBuilder2.Clear(); // clear all the string
               
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
                return;
            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;
            

            Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

            SaveToDb(chatId, messageText, message.From.Username);

            //SendMessageToTelegram(chatId, messageText);
        }
        public static async Task SaveToDb(long chatId, string messageText, string userName)
        {
            int? userId = await GetUserByChatId(chatId);
            if (userId == null)
            {
                await SaveUser(chatId, userName);
                userId = await GetUserByChatId(chatId);
            }
            
            await SaveMessage(chatId, messageText, userId);
        }
        private async static Task SaveMessage(long chatId, string messageText, int? userId)
        {
            //your connection string 
            string connString = "Data Source=(local);Initial Catalog=DevSpaceBot;Integrated Security=True;Trust Server Certificate=true";
            //create instanace of database connection
            SqlConnection conn = new SqlConnection(connString);
            try
            {
                //open connection
                conn.Open();
                //create a new SQL Query using StringBuilder

                StringBuilder strBuilder2 = new StringBuilder();
                strBuilder2.Append("Insert into Messages (MessageText, MessageDate, ChatId, IsOur, IsSended, UserId)  values ('");
                strBuilder2.Append(messageText);
                strBuilder2.Append("','");
                strBuilder2.Append(DateTime.Now);
                strBuilder2.Append("','");
                strBuilder2.Append(chatId);
                strBuilder2.Append("','");
                strBuilder2.Append(0);
                strBuilder2.Append("','");
                strBuilder2.Append(1);
                strBuilder2.Append("','");
                strBuilder2.Append(userId);
                strBuilder2.Append("')");
                string sqlQuery2 = strBuilder2.ToString();

                SqlCommand cmd2 = new SqlCommand(sqlQuery2, conn);
                cmd2.ExecuteNonQuery();
                strBuilder2.Clear(); // clear all the string
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
        private async static Task SaveUser(long chatId, string userName)
        {
            //your connection string 
            string connString = "Data Source=(local);Initial Catalog=DevSpaceBot;Integrated Security=True;Trust Server Certificate=true";
            //create instanace of database connection
            SqlConnection conn = new SqlConnection(connString);
            try
            {
                //open connection
                conn.Open();
                //create a new SQL Query using StringBuilder

                StringBuilder strBuilder2 = new StringBuilder();
                strBuilder2.Append("Insert into Users (Name, ChatId, IsActive, LastMessage)  values ('");
                strBuilder2.Append(userName);
                strBuilder2.Append("','");
                strBuilder2.Append(chatId);
                strBuilder2.Append("','");
                strBuilder2.Append(1);
                strBuilder2.Append("','");
                strBuilder2.Append(DateTime.Now);
                strBuilder2.Append("')");
                string sqlQuery2 = strBuilder2.ToString();

                SqlCommand cmd2 = new SqlCommand(sqlQuery2, conn);
                cmd2.ExecuteNonQuery();
                strBuilder2.Clear(); // clear all the string
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
        private async static Task<int?> GetUserByChatId(long chatId)
        {
            //your connection string 
            string connString = "Data Source=(local);Initial Catalog=DevSpaceBot;Integrated Security=True;Trust Server Certificate=true";
            //create instanace of database connection
            SqlConnection conn = new SqlConnection(connString);
            try
            {
                //open connection
                conn.Open();
                //create a new SQL Query using StringBuilder
                StringBuilder strBuilder = new StringBuilder();

                strBuilder.Append("SELECT * FROM Users WHERE ChatId = ");
                strBuilder.Append(chatId);
                string sqlQuery = strBuilder.ToString();

                SqlCommand cmd = new SqlCommand(sqlQuery, conn);
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    int id = Convert.ToInt32(rdr["Id"]);
                    return id;
                }
                strBuilder.Clear(); // clear all the string
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            return null;
        }

        public static async Task SendMessageToTelegram(long chatId, string messageText)
        {
            // Echo received message text
            await _client.SendTextMessageAsync(
                chatId: chatId,
                text: messageText);
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}



