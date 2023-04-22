using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Threading.Tasks;
using System.Web;
using System.Threading;
using System.Diagnostics;
using System.Threading.Channels;
using System.Runtime.Remoting.Channels;

namespace Enoca.Task.RabbitMQ.Controllers
{
    public class ValuesController : ApiController
    {
        [HttpPost]
        [Route("api/Values/SendMessage")]
        public IHttpActionResult SendMessage()
        {
            try
            {
                // RabbitMQ'ya bağlanmak için bir ConnectionFactory örneği oluşturulması.
                var factory = new ConnectionFactory();
                factory.Uri = new Uri("amqps://yourCloudRabbitMQUrl");

                //Bağlantının aktifleştirilmesi ve kanal açma
                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {

                        //Queue Tanımlama
                        channel.QueueDeclare(queue: "example-queue-2", exclusive: false);

                        //Mesaj Oluşturma
                        var message = $"Hello, RabbitMQ! Yeni bir mesaj | {DateTime.Now}";
                        byte[] byteMessage = Encoding.UTF8.GetBytes(message);


                        //Mesaj Gönderme
                        channel.BasicPublish(
                            exchange: "",
                            routingKey: "example-queue-2",//Mesajın hangi kuyruğa gideceğini belirtir.
                            body: byteMessage);                        
                        return Ok($"Mesaj iletildi! Mesaj:{message}");                        
                    }
                }               
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/Values/GetMessage")]
        public IHttpActionResult GetMessage()
        {
            try
            {
                //Bağlantı Oluşturma
                var factory = new ConnectionFactory();
                factory.Uri = new Uri("amqps://yourCloudRabbitMQUrl");
               
                var queueMessage = "";
                //Bağlantıyı Aktifleştirme ve Kanal Açma
                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        //Queue Tanımlama Not: Publisher kısmı ile aynı olması gerekiyor.
                        channel.QueueDeclare(queue: "example-queue-2", exclusive: false);

                        // EventingBasicConsumer kullanarak mesajları dinliyoruz.
                        var consumer = new EventingBasicConsumer(channel);                       
                        consumer.Received += (sender, e) =>
                        {
                            var body = e.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);                            
                            channel.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);
                            queueMessage = message;                           
                            Debug.WriteLine(message);
                           
                        };
                        // Kanalda mesajları dinleme.
                        channel.BasicConsume(queue: "example-queue-2", autoAck: false, consumer);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }   
                return Ok(queueMessage);
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata oluştu: {ex.Message}");
            }
        }
    }
}

//***********Direct Exchange: Mesajları doğrudan bir kuyruğa yönlendirir. Mesajın rota anahtarı ile kuyruk adı tam olarak eşleşmelidir.

//********** Fanout Exchange: Mesajları tüm kuyruklara gönderir. Bu exchange türünde rota anahtarı kullanılmaz.


// Mesaj Gönderme :
//channel.ExchangeDeclare("fanout-exchange", ExchangeType.Fanout);
//channel.BasicPublish(exchange: "fanout-exchange", routingKey: "", basicProperties: null, body: byteMessage);

//// Mesaj Alma :
//channel.ExchangeDeclare("fanout-exchange", ExchangeType.Fanout);
//var queueName = channel.QueueDeclare().QueueName;
//channel.QueueBind(queue: queueName, exchange: "fanout-exchange", routingKey: "");


//********** Topic Exchange: Mesajları rota anahtarlarına göre kuyruklara yönlendirir. Kuyruk adı ve rota anahtarı belirli bir kalıba göre eşleşmelidir.

// Mesaj Gönderme :
//channel.ExchangeDeclare("topic-exchange", ExchangeType.Topic);
//var routingKey = "animals.mammals.dogs"; // Örnek bir rota anahtarı
//channel.BasicPublish(exchange: "topic-exchange", routingKey: routingKey, basicProperties: null, body: byteMessage);

//// Mesaj Alma için :
//channel.ExchangeDeclare("topic-exchange", ExchangeType.Topic);
//var queueName = channel.QueueDeclare().QueueName;
//var bindingKey = "animals.mammals.*"; // Örnek bir bağlama anahtarı
//channel.QueueBind(queue: queueName, exchange: "topic-exchange", routingKey: bindingKey);



//********** Headers Exchange: Mesajları kuyruklara yönlendirmek için mesaj özelliklerini kullanır. Mesaj özellikleri, mesajın başlık alanlarında bulunur.


// Mesaj Gönderme :
//channel.ExchangeDeclare("headers-exchange", ExchangeType.Headers);
//var properties = channel.CreateBasicProperties();
//properties.Headers = new Dictionary<string, object> { { "category", "mammals" }, { "type", "dogs" } };
//channel.BasicPublish(exchange: "headers-exchange", routingKey: "", basicProperties: properties, body: byteMessage);

//// Mesaj Alma :
//channel.ExchangeDeclare("headers-exchange", ExchangeType.Headers);
//var queueName = channel.QueueDeclare().QueueName;
//var headers = new Dictionary<string, object> { { "category", "mammals" }, { "type", "dogs" } };
//channel.QueueBind(queue: queueName, exchange: "headers-exchange", routingKey: "", arguments: new Dictionary<string, object> { { "x-match", "all" }, { "category", "mammals" }, { "type", "dogs" } });
