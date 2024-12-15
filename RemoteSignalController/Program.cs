// See https://aka.ms/new-console-template for more information

 HttpClient _client = new HttpClient();


        Console.WriteLine("Shutdown Signal Controller");
        Console.WriteLine("==========================");
        Console.WriteLine("Choose an action:");
        Console.WriteLine("1. Turn Shutdown ON");
        Console.WriteLine("2. Turn Shutdown OFF");
        Console.WriteLine("0. Exit");
        
        while (true)
        {
            Console.Write("\nEnter your choice: ");
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await SetShutdownSignal(true);
                    break;
                case "2":
                    await SetShutdownSignal(false);
                    break;
                case "0":
                    Console.WriteLine("Exiting...");
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please enter 1, 2, or 0.");
                    break;
            }
        }
     async Task SetShutdownSignal(bool shutdownValue)
    {
        string apiUrl = "http://192.168.1.6:2030/api/shutdown";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new StringContent($"{{ \"Shutdown\": {shutdownValue.ToString().ToLower()} }}", 
                    System.Text.Encoding.UTF8, "application/json")
            };

            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Shutdown signal set to '{shutdownValue}'.");
            }
            else
            {
                Console.WriteLine($"Failed to update shutdown signal. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }