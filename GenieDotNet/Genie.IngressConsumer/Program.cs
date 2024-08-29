using Genie.IngressConsumer.Services;

while (true)
{
    // Console.WriteLine(@"Enter (R)abbitMQ (multi-threaded) or (Ra)bbitMQ (single-threaded) ");
    // var input = Console.ReadLine();
    string input = "ra";

    Task task = input?.ToLower() switch
    {
        "r" => Task.Run(async () => { await RabbitMQService.Start(); }),
        "ra" => Task.Run(async () => { await RabbitMQServiceSingleThreaded.Start(); }),
        _ => Task.Run(() => { Console.WriteLine("Invalid input"); })
    };

    await Task.WhenAny([task]);
    Console.WriteLine("Consumer has exited");
}
