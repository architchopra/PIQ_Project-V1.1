using System. Net. Http. Headers;
using System. Text;

namespace PIQ_Project
    {
    public class TeamsPOST
        {
        public async Task SendMessageAndCsvFile ( string webhookUrl, string messageText, string csvFileName = null )
            {
            if ( csvFileName != null )
                {
                if ( File. Exists ( csvFileName ) )
                    {
                    await SendMessage ( webhookUrl, messageText );
                    await SendCsvFile ( webhookUrl, csvFileName );
                    }
                else
                    {
                    await SendMessage ( webhookUrl, messageText + " -- Warning! File Does Not Exist! - " + Path. GetFileName ( csvFileName ) );
                    }
                }
            else
                {
                await SendMessage ( webhookUrl, messageText );
                }
            }
        private async Task SendMessage ( string webhookUrl, string text )
            {
            // Create a payload for the message

            try
                {
                // Create a payload for the message
                string message = "{\"text\": \"" + text + "\"}";

                // Create an HTTP client
                using ( HttpClient client = new HttpClient ( ) )
                    {
                    // Prepare the HTTP request
                    var content = new StringContent(message, Encoding.UTF8, "application/json");

                    // Send the HTTP POST request to the Power Automate flow
                    var response = await client.PostAsync(webhookUrl, content);

                    // Check if the request was successful
                    if ( response. IsSuccessStatusCode )
                        {
                        Console. WriteLine ( "Data sent successfully to Power Automate!" );
                        }
                    else
                        {
                        Console. WriteLine ( $"Failed to send data to Power Automate. Status code: {response. StatusCode}" );
                        }
                    }
                }
            catch ( Exception ex )
                {
                Console. WriteLine ( $"An error occurred: {ex. Message}" );
                }
            }

        private async Task SendCsvFile ( string webhookUrl, string fileName )
            {
            try
                {
                // Read CSV file data
                byte[] csvData = File.ReadAllBytes(fileName);

                // Create an HTTP client
                using ( HttpClient client = new HttpClient ( ) )
                    {
                    // Create a stream content to send the data in chunks
                    using ( var content = new StreamContent ( new MemoryStream ( csvData ) ) )
                        {
                        // Set the content type
                        content. Headers. ContentType = new MediaTypeHeaderValue ( "text/csv" );

                        // Send the POST request to the webhook URL with the stream content
                        var response = await client.PostAsync(webhookUrl, content);

                        // Check if the request was successful
                        if ( response. IsSuccessStatusCode )
                            {
                            Console. WriteLine ( "CSV file sent successfully!" );
                            }
                        else
                            {
                            Console. WriteLine ( $"Failed to send CSV file. Status code: {response. StatusCode}" );
                            }
                        }
                    }
                }
            catch ( Exception ex )
                {
                Console. WriteLine ( $"An error occurred: {ex. Message}" );
                }
            }
        }
    }
